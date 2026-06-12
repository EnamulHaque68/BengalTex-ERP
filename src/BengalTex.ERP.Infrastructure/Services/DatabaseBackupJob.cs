using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Configuration for the nightly SQL Server backup job (section "DatabaseBackup").
/// </summary>
public sealed class DatabaseBackupOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Directory the backup file is written to, AS SEEN BY THE SQL SERVER PROCESS
    /// (BACKUP DATABASE ... TO DISK resolves on the SQL host, not the API host).
    /// Empty → bare filename → SQL Server's own default backup directory.
    /// Windows same-host example: "D:\\BengalTexBackups". Docker: "/var/opt/mssql/backups".
    /// </summary>
    public string BackupDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Same directory AS SEEN BY THE API PROCESS — used for retention cleanup.
    /// Defaults to <see cref="BackupDirectory"/> (correct when API and SQL share a host).
    /// In Docker, mount one volume into both containers and set this to the API-side path.
    /// </summary>
    public string CleanupDirectory { get; set; } = string.Empty;

    /// <summary>Backups older than this many days are deleted after a successful run. ≤ 0 disables cleanup.</summary>
    public int RetentionDays { get; set; } = 14;

    /// <summary>WITH COMPRESSION — NOT supported on SQL Server Express; leave false there.</summary>
    public bool Compress { get; set; }

    /// <summary>Run RESTORE VERIFYONLY after the backup to validate the file. Recommended.</summary>
    public bool VerifyAfterBackup { get; set; } = true;
}

/// <summary>
/// Nightly Hangfire job — full backup of the application database via
/// <c>BACKUP DATABASE ... WITH INIT, CHECKSUM</c>, optional <c>RESTORE VERIFYONLY</c>,
/// then retention cleanup of old .bak files. Also enqueueable on demand
/// (POST /api/maintenance/backup-now) before risky operations like migrations.
///
/// Restore (new server / disaster):
///   RESTORE DATABASE [BengalTexERP] FROM DISK = N'…\BengalTexERP-yyyyMMdd-HHmmss.bak'
///       WITH REPLACE, RECOVERY;
/// (Stop the API first; re-point the connection string; start the API.)
/// </summary>
public class DatabaseBackupJob
{
    public const string RecurringJobId = "database-backup";

    private readonly IConfiguration _config;
    private readonly DatabaseBackupOptions _opts;
    private readonly ILogger<DatabaseBackupJob> _logger;

    public DatabaseBackupJob(
        IConfiguration config,
        IOptions<DatabaseBackupOptions> opts,
        ILogger<DatabaseBackupJob> logger)
    {
        _config = config;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        if (!_opts.Enabled)
        {
            _logger.LogInformation("DatabaseBackup: disabled via configuration; skipping.");
            return;
        }

        var connectionString = _config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        var dbName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (string.IsNullOrWhiteSpace(dbName))
            throw new InvalidOperationException("Could not determine database name from DefaultConnection.");

        var fileName = $"{dbName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.bak";
        var diskPath = JoinSqlPath(_opts.BackupDirectory, fileName);

        await using var conn = new SqlConnection(connectionString);
        conn.InfoMessage += (_, e) => _logger.LogInformation("DatabaseBackup (SQL): {Message}", e.Message);
        await conn.OpenAsync(ct);

        var withClause = _opts.Compress ? "WITH INIT, CHECKSUM, COMPRESSION, STATS = 25" : "WITH INIT, CHECKSUM, STATS = 25";
        // dbName comes from our own connection string (not user input); bracket-escaped anyway.
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"BACKUP DATABASE [{dbName.Replace("]", "]]")}] TO DISK = @path {withClause}";
            cmd.Parameters.AddWithValue("@path", diskPath);
            cmd.CommandTimeout = 3600;   // backups of multi-GB DBs take minutes, not the default 30 s
            await cmd.ExecuteNonQueryAsync(ct);
        }
        _logger.LogInformation("DatabaseBackup: backup of [{Db}] written to '{Path}'.", dbName, diskPath);

        if (_opts.VerifyAfterBackup)
        {
            await using var verify = conn.CreateCommand();
            verify.CommandText = "RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM";
            verify.Parameters.AddWithValue("@path", diskPath);
            verify.CommandTimeout = 3600;
            await verify.ExecuteNonQueryAsync(ct);
            _logger.LogInformation("DatabaseBackup: RESTORE VERIFYONLY passed for '{Path}'.", diskPath);
        }

        CleanupOldBackups(dbName);
    }

    private void CleanupOldBackups(string dbName)
    {
        if (_opts.RetentionDays <= 0) return;

        var dir = string.IsNullOrWhiteSpace(_opts.CleanupDirectory) ? _opts.BackupDirectory : _opts.CleanupDirectory;
        if (string.IsNullOrWhiteSpace(dir))
        {
            _logger.LogInformation(
                "DatabaseBackup: BackupDirectory not set (using SQL Server's default dir) — retention cleanup skipped. " +
                "Set DatabaseBackup:BackupDirectory (and CleanupDirectory if the API runs on a different host) to enable cleanup.");
            return;
        }

        try
        {
            if (!Directory.Exists(dir))
            {
                _logger.LogWarning(
                    "DatabaseBackup: cleanup directory '{Dir}' is not visible from the API host — cleanup skipped. " +
                    "If SQL Server runs elsewhere (e.g. its own container), share the backup volume and set DatabaseBackup:CleanupDirectory.", dir);
                return;
            }

            var cutoff = DateTime.UtcNow.AddDays(-_opts.RetentionDays);
            var deleted = 0;
            foreach (var file in Directory.EnumerateFiles(dir, $"{dbName}-*.bak"))
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                {
                    File.Delete(file);
                    deleted++;
                }
            }
            if (deleted > 0)
                _logger.LogInformation("DatabaseBackup: deleted {Count} backup file(s) older than {Days} days from '{Dir}'.",
                    deleted, _opts.RetentionDays, dir);
        }
        catch (Exception ex)
        {
            // Cleanup failure must not fail the job — tonight's backup already succeeded.
            _logger.LogError(ex, "DatabaseBackup: retention cleanup failed for '{Dir}'.", dir);
        }
    }

    /// <summary>
    /// Joins dir + file using the separator style of the directory itself — the path is
    /// resolved by the SQL SERVER host (possibly Linux) while this code may run on Windows,
    /// so Path.Combine (host-native separators) would corrupt cross-OS paths.
    /// </summary>
    private static string JoinSqlPath(string dir, string fileName)
    {
        if (string.IsNullOrWhiteSpace(dir)) return fileName;   // bare name → SQL default backup dir
        var separator = dir.Contains('/') ? "/" : "\\";
        return dir.TrimEnd('/', '\\') + separator + fileName;
    }
}

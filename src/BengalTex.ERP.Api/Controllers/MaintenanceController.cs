using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Infrastructure.Services;
using BengalTex.ERP.Shared.Common;
using BengalTex.ERP.Shared.Permissions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Operational maintenance actions for admins. Each action enqueues the corresponding
/// Hangfire job (fire-and-forget) — progress/result is visible in the Serilog output
/// and (in Development) the Hangfire dashboard.
/// </summary>
[ApiController]
[Route("api/maintenance")]
[Authorize]
public class MaintenanceController : ControllerBase
{
    private readonly IBackgroundJobClient _jobs;

    public MaintenanceController(IBackgroundJobClient jobs) => _jobs = jobs;

    /// <summary>
    /// Run a full database backup now (same job as the nightly 01:30 schedule).
    /// Use before risky operations — applying migrations, bulk imports, version upgrades.
    /// </summary>
    [HttpPost("backup-now")]
    [HasPermission(Permissions.Settings.Edit)]
    public IActionResult BackupNow()
    {
        var jobId = _jobs.Enqueue<DatabaseBackupJob>(x => x.RunAsync(CancellationToken.None));
        return Ok(ApiResponse<string>.Ok(jobId,
            "Backup job queued. Check the API log for the result (file path + verification)."));
    }

    /// <summary>Run the audit-log retention trim now (same job as the nightly 02:30 schedule).</summary>
    [HttpPost("trim-audit-log-now")]
    [HasPermission(Permissions.Settings.Edit)]
    public IActionResult TrimAuditLogNow()
    {
        var jobId = _jobs.Enqueue<AuditLogRetentionJob>(x => x.RunAsync(CancellationToken.None));
        return Ok(ApiResponse<string>.Ok(jobId,
            "Audit-log retention job queued. Check the API log for the deleted-row count."));
    }

    /// <summary>
    /// Run the month-end statement batch now (same job as the 06:00-on-the-1st schedule —
    /// sends LAST month's statements). No-op unless MonthlyStatements:Enabled=true.
    /// </summary>
    [HttpPost("send-monthly-statements-now")]
    [HasPermission(Permissions.Settings.Edit)]
    public IActionResult SendMonthlyStatementsNow()
    {
        var jobId = _jobs.Enqueue<BengalTex.ERP.Application.Reports.Jobs.MonthlyStatementBatchJob>(
            x => x.RunAsync(CancellationToken.None));
        return Ok(ApiResponse<string>.Ok(jobId,
            "Monthly statement batch queued. Check the API log for sent/skipped/failed counts."));
    }

    /// <summary>
    /// Run the operational-alerts sweep now (same job as the 07:00 daily schedule) —
    /// low stock, overdue invoices, expiring certificates. Deduplicated per DedupDays.
    /// </summary>
    [HttpPost("run-operational-alerts")]
    [HasPermission(Permissions.Settings.Edit)]
    public IActionResult RunOperationalAlerts()
    {
        var jobId = _jobs.Enqueue<OperationalAlertsJob>(x => x.RunAsync(CancellationToken.None));
        return Ok(ApiResponse<string>.Ok(jobId,
            "Operational-alerts sweep queued. Check the Notifications viewer for new alerts."));
    }
}

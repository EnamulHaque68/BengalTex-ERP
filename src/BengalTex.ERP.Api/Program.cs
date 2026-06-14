using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using BengalTex.ERP.Api.Authentication;
using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Api.Hubs;
using BengalTex.ERP.Api.Middleware;
using BengalTex.ERP.Api.Services;
using BengalTex.ERP.Application;
using BengalTex.ERP.Application.Auth;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Settings;
using BengalTex.ERP.Infrastructure;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// QuestPDF community license — required before any PDF is generated. Free for companies
// under USD 1M annual revenue; switch to LicenseType.Professional with a paid licence
// above that threshold. See https://www.questpdf.com/license/
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// SERILOG (Read from appsettings.json)
// ============================================
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

// ============================================
// CONFIGURATION BINDING
// ============================================
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("Application"));
builder.Services.Configure<BengalTex.ERP.Infrastructure.Services.AuditLogRetentionOptions>(
    builder.Configuration.GetSection("AuditLogRetention"));
builder.Services.Configure<BengalTex.ERP.Infrastructure.Services.DatabaseBackupOptions>(
    builder.Configuration.GetSection("DatabaseBackup"));
builder.Services.Configure<BengalTex.ERP.Application.Reports.Jobs.MonthlyStatementOptions>(
    builder.Configuration.GetSection("MonthlyStatements"));
builder.Services.Configure<BengalTex.ERP.Infrastructure.Services.OperationalAlertsOptions>(
    builder.Configuration.GetSection("OperationalAlerts"));
builder.Services.Configure<BengalTex.ERP.Application.Reports.Jobs.DunningOptions>(
    builder.Configuration.GetSection("Dunning"));

// ============================================
// LAYER REGISTRATIONS
// ============================================
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ============================================
// API SERVICES
// ============================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IJwtService, JwtService>();   // Application.Auth.IJwtService → Api.Authentication.JwtService
builder.Services.AddScoped<ISessionBroadcaster, SessionBroadcaster>(); // SignalR-backed
builder.Services.AddMemoryCache();

// ============================================
// AUTHENTICATION (JWT)
// ============================================
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

// Fail fast if the signing key is missing or weak. Secrets are NOT committed to
// appsettings.json — supply them via the Jwt__Secret environment variable in production
// (or appsettings.Development.json, which is git-ignored, for local dev).
if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || Encoding.UTF8.GetByteCount(jwtSettings.Secret) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Secret is missing or too short (need at least 32 bytes). Set the 'Jwt__Secret' " +
        "environment variable (production) or appsettings.Development.json (local dev).");
}

// AddIdentity registers cookie auth and sets it as DefaultAuthenticate/ChallengeScheme.
// We override that here so unauthenticated API calls return 401 JSON via JwtBearer
// instead of being redirected (302) to /Account/Login by the cookie scheme.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        // SignalR connects via WebSocket — browsers cannot set custom headers there,
        // so the access token is passed as ?access_token=... on the negotiate request.
        // Pull it out for any /hubs/* path so [Authorize] on the hub still works.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// SignalR for real-time session events
builder.Services.AddSignalR();

// ============================================
// HSTS (HTTP Strict Transport Security)
// ============================================
// Only emitted over HTTPS in non-Development environments. 1-year max-age, includeSubDomains.
// Browsers will refuse plain-HTTP for the entire host for 1 year after the first HTTPS visit.
builder.Services.AddHsts(options =>
{
    options.Preload = false;             // Toggle once domain is on the HSTS preload list.
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// ============================================
// REQUEST BODY SIZE CAP
// ============================================
// Defaults are ~28 MB on Kestrel / ~30 MB on IIS. Bump explicitly to 50 MB so document
// attachments (BL copy, audit certificates, large CSV imports) fit, but cap so a hostile
// client can't OOM us with a 4 GB stream.
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(o =>
    o.Limits.MaxRequestBodySize = 50L * 1024 * 1024);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50L * 1024 * 1024;
    o.ValueLengthLimit = int.MaxValue;
});

// ============================================
// RATE LIMITING (brute-force / credential-stuffing protection)
// ============================================
// Per-IP fixed window on the public auth endpoints (login / forgot- / reset-password).
// Complements the per-account Identity lockout (5 fails → 15 min) with an IP-level cap
// that also blunts username/email enumeration across many accounts.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ============================================
// AUTHORIZATION (Permission-based)
// ============================================
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// ============================================
// CONTROLLERS + JSON
// ============================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ============================================
// CORS (For Angular frontend)
// ============================================
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ============================================
// SWAGGER (with JWT support)
// ============================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bengal TEX ERP API",
        Version = "v1",
        Description = "Garments Accessories Manufacturing ERP API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header. Example: 'Bearer eyJhbGc...'"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================
// HANGFIRE (Background Jobs)
// ============================================
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));

builder.Services.AddHangfireServer();

var app = builder.Build();

// ============================================
// MIDDLEWARE PIPELINE (Order matters!)
// ============================================

// 1. Serilog request logging
app.UseSerilogRequestLogging();

// 2. Global exception handler (catches everything below)
app.UseMiddleware<GlobalExceptionMiddleware>();

// 2b. Security headers (stamped on every response — must be early so OnStarting fires
//     before the status line is sent for every code path below).
app.UseMiddleware<SecurityHeadersMiddleware>();

// 3. Swagger (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bengal TEX ERP API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    // 3b. HSTS (production only — never emit it in dev so localhost can use plain HTTP)
    app.UseHsts();
}

// 4. HTTPS redirect
app.UseHttpsRedirection();

// 5. CORS (Before authentication)
app.UseCors("AngularApp");

// 6. Authentication (Who is the user?)
app.UseAuthentication();

// 7. Authorization (What can they do?)
app.UseAuthorization();

// 7b. Rate limiter (endpoint policies resolved after routing/auth)
app.UseRateLimiter();

// 8. Hangfire dashboard (Development only for now)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

// 9. Routes
app.MapControllers();
app.MapHub<SessionHub>("/hubs/session");

// 10. Health check endpoint
app.MapGet("/", () => "Bengal TEX ERP API is running!");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTimeOffset.UtcNow }));

// ============================================
// DATABASE INITIALIZATION (migrate + seed)
// ============================================
// Dev: seed only — migrations are applied manually via `dotnet ef database update`.
// Container/Prod: set Database__InitializeOnStartup=true → apply EF migrations + seed on boot
// (the seeder is idempotent — SuperAdmin, roles, permissions, base currency, numbering series).
var initializeDb = app.Configuration.GetValue<bool>("Database:InitializeOnStartup");
if (app.Environment.IsDevelopment() || initializeDb)
{
    using var scope = app.Services.CreateScope();
    if (initializeDb)
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }
    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    await seeder.SeedAsync();
}

// ============================================
// HANGFIRE RECURRING JOBS
// ============================================
// Outbox processor — drains transactional outbox every 30 seconds
RecurringJob.AddOrUpdate<OutboxProcessor>(
    OutboxProcessor.RecurringJobId,
    x => x.ProcessAsync(CancellationToken.None),
    "*/30 * * * * *"); // 6-field cron: every 30 seconds

// Audit log retention — purges AuditLogEntries older than configured retention (default 365 days)
// nightly at 02:30 local time. Set AuditLogRetention:RetentionDays to 0 to disable.
RecurringJob.AddOrUpdate<AuditLogRetentionJob>(
    AuditLogRetentionJob.RecurringJobId,
    x => x.RunAsync(CancellationToken.None),
    "30 2 * * *"); // 5-field cron: every day at 02:30

// Database backup — full BACKUP DATABASE nightly at 01:30 (before the audit-log trim at
// 02:30). Configure via the DatabaseBackup section; also enqueueable on demand from
// POST /api/maintenance/backup-now. Set DatabaseBackup:Enabled=false to disable.
RecurringJob.AddOrUpdate<DatabaseBackupJob>(
    DatabaseBackupJob.RecurringJobId,
    x => x.RunAsync(CancellationToken.None),
    "30 1 * * *"); // 5-field cron: every day at 01:30

// Month-end statements — emails last month's statement PDF to every active party with
// activity + an email on file. OPT-IN: does nothing until MonthlyStatements:Enabled=true.
RecurringJob.AddOrUpdate<BengalTex.ERP.Application.Reports.Jobs.MonthlyStatementBatchJob>(
    BengalTex.ERP.Application.Reports.Jobs.MonthlyStatementBatchJob.RecurringJobId,
    x => x.RunAsync(CancellationToken.None),
    "0 6 1 * *"); // 5-field cron: 06:00 on the 1st of every month

// Operational alerts — daily at 07:00: low stock, overdue invoices, expiring certificates.
// Deduplicated so a persistent condition isn't re-alerted every morning.
RecurringJob.AddOrUpdate<OperationalAlertsJob>(
    OperationalAlertsJob.RecurringJobId,
    x => x.RunAsync(CancellationToken.None),
    "0 7 * * *"); // 5-field cron: every day at 07:00

// Dunning reminders — daily at 08:00: emails customers about overdue invoices, escalating
// tone by age. OPT-IN: does nothing until Dunning:Enabled=true (+ a real Email provider).
RecurringJob.AddOrUpdate<BengalTex.ERP.Application.Reports.Jobs.DunningReminderJob>(
    BengalTex.ERP.Application.Reports.Jobs.DunningReminderJob.RecurringJobId,
    x => x.RunAsync(CancellationToken.None),
    "0 8 * * *"); // 5-field cron: every day at 08:00

// ============================================
// RUN
// ============================================
try
{
    Log.Information("Starting Bengal TEX ERP API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
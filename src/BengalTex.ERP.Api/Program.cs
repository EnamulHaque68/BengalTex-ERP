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
using BengalTex.ERP.Infrastructure.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

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
// DATABASE SEEDER (idempotent — safe to run every startup in Dev)
// ============================================
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
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
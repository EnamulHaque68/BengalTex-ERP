using BengalTex.ERP.Application.Auth;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Infrastructure.Identity;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Interceptors;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using BengalTex.ERP.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BengalTex.ERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<AuditInterceptor>();

        // Device fingerprint settings + service
        services.Configure<DeviceFingerprintSettings>(config.GetSection("DeviceFingerprint"));
        services.AddScoped<IDeviceFingerprintService, DeviceFingerprintService>();

        // File storage (local disk; swap for S3/Blob in production)
        services.Configure<FileStorageSettings>(config.GetSection("FileStorage"));
        services.AddScoped<IFileStorage, LocalFileStorage>();

        // Approval workflow rules (threshold + approver role per gated document type)
        services.Configure<ApprovalSettings>(config.GetSection("Approvals"));

        // SMS gateway (DevLogger stub; swap for SslWireless / Twilio in production)
        services.Configure<SmsSettings>(config.GetSection("Sms"));
        services.AddScoped<ISmsSender, DevSmsSender>();

        // Email gateway — real SMTP when Email:Provider = "Smtp", else DevLogger (logs).
        services.Configure<EmailSettings>(config.GetSection("Email"));
        if (string.Equals(config.GetSection("Email")["Provider"], "Smtp", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else
            services.AddScoped<IEmailSender, DevEmailSender>();

        // Auth services
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ISessionEnforcementService, SessionEnforcementService>();
        services.AddScoped<ISuspiciousActivityDetector, SuspiciousActivityDetector>();
        services.AddScoped<IGeoFenceService, GeoFenceService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();

        // Seeder
        services.AddScoped<IDataSeeder, DataSeeder>();

        // Outbox processor (Hangfire recurring job — class activated by IServiceProvider, no interface needed)
        services.AddScoped<OutboxProcessor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                sql =>
                {
                    sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    sql.UseNetTopologySuite();
                    sql.EnableRetryOnFailure(3);
                });

            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<INumberingService, NumberingService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IStockLotService, StockLotService>();
        services.AddScoped<IJournalPostingService, JournalPostingService>();
        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddSingleton<IQrCodeService, QrCodeService>();

        services.AddIdentity<ApplicationUser, ApplicationRole>(o =>
            {
                o.Password.RequiredLength = 8;
                o.Password.RequireDigit = true;
                o.Password.RequireUppercase = true;
                o.Password.RequireNonAlphanumeric = false;
                o.User.RequireUniqueEmail = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}
# BengalTex ERP – Critical Bug Register & Risk Tracker

**Document Version:** 1.0.0  
**Last Updated:** November 9, 2026  
**Purpose:** Active registry of identified bugs, edge cases, configuration hazards, and architectural vulnerabilities.

---

## 1. Bug & Hazard Severity Classification

- 🔴 **CRITICAL (P1):** System startup failure, financial imbalance, data loss, or security compromise.
- 🟠 **HIGH (P2):** Core business operation blocked or incorrect transaction calculation.
- 🟡 **MEDIUM (P3):** Non-blocking limitation, fallback in use, or dev stub in production path.
- 🟢 **LOW (P4):** Cosmetic, documentation typo, or minor performance optimization.

---

## 2. Active Bug & Risk Register

| Bug ID | Severity | Area / Module | Description & Root Cause | Business / Operational Impact | Mitigation & Resolution Status |
| :---: | :---: | :--- | :--- | :--- | :--- |
| **BUG-001** | 🔴 **CRITICAL** | Startup / Database | **Hangfire / LocalDB First-Boot Race Condition:** When the application starts up on a fresh machine where the database `BengalTexERP` has not yet been created, `builder.Services.AddHangfireServer()` fails during `builder.Build()` because Hangfire attempts to install its schema before `MigrateAsync()` runs. | API crashes on first boot with `SqlException: Cannot open database "BengalTexERP"`. | 🟢 **RESOLVED:** Reordered `Program.cs` startup pipeline: automated database migration and idempotent seeding now execute immediately after `builder.Build()` and prior to Hangfire dashboard or background dispatcher activation. Added `"Database": { "InitializeOnStartup": true }` to `appsettings.json` and `appsettings.Development.json`. Verified with clean boot and 131/131 passing API integration tests. |
| **BUG-002** | 🟡 **MEDIUM** | Notifications / SMS | **DevSmsSender Used as Production Default:** `ISmsSender` is registered to `DevSmsSender` which outputs SMS bodies to Serilog instead of transmitting to a live SMS gateway aggregator. | In production, users will not receive real SMS text messages for OTPs, security alerts, or delivery notices. | **Action Required:** Implement live telecom provider adapter (e.g. SSL Wireless / Twilio) and register via `Sms:Provider` in `appsettings.Production.json`. |
| **BUG-003** | 🟡 **MEDIUM** | Notifications / Email | **DevEmailSender Configured as Default:** `EmailSettings.Provider` defaults to `"DevLogger"` in `appsettings.json`. Although `SmtpEmailSender` is fully implemented, live emails are not dispatched unless configuration is updated. | Automated customer invoices, purchase orders, and monthly statements are written to log files rather than delivered to inboxes. | **Action Required:** Document and verify production SMTP configuration in `appsettings.Production.json`. |
| **BUG-004** | 🟡 **MEDIUM** | Attendance / Security | **Missing Commercial FingerprintJS API Key:** `environment.development.ts` has `apiKey: ''`. The system falls back to a browser-derived canvas and user-agent hash. | While functional for basic device verification, advanced fraud detection (identifying spoofed virtual machines or rooted devices) requires FingerprintJS Pro. | **Action Required:** Document key configuration for high-security installations. |
| **BUG-005** | 🟡 **MEDIUM** | Testing / Client | **Absence of Client Automated Test Specs:** The Angular frontend contains only 2 test files (`app.spec.ts`, `health.spec.ts`) across 153 components and 84 services. | Lack of automated client regression tests increases risk of undetected UI breakage during frontend updates. | **Action Required:** Phase 3 of Roadmap prioritizes building comprehensive client-side unit test harnesses. |
| **BUG-006** | 🟢 **LOW** | Compiler Warnings | **Minor XML Comment Warnings:** Warnings CS1574 and CS1570 in `CreatePaymentCommand.cs`, `CloseSalesOrderCommand.cs`, and `FinancialIntelligenceController.cs` regarding XML doc tags. | Does not affect runtime execution; prints benign compiler warnings during build. | **Action Required:** Clean up XML doc comments during next maintenance pass. |
| **BUG-007** | 🟢 **LOW** | Client Routing | **Superseded `home.module.ts` Still Present on Disk:** `home.module.ts` exists on disk but is superseded by `dashboard.module.ts`. | Minor code bloat. Router correctly points to `DashboardModule`. | **Action Required:** Archive or deprecate `home.module.ts` cleanly. |
| **BUG-008** | 🟢 **LOW** | Development Sandbox | **Named Pipes / IPC Block in Windows Sandbox:** MSBuild multi-node compilation (`-nodeReuse:true`) and VSTest IPC get blocked inside restricted Windows AppContainer sandboxes. | Commands take long or time out if ran without `-nodeReuse:false -m:1` or `BypassSandbox: true`. | **Action Documented:** Sandboxed development runs use `-nodeReuse:false -m:1`. Normal terminal execution outside sandbox works without restrictions. |

---

## 3. Bug Lifecycle & Verification Protocol

1. Every bug must be assigned a unique `BUG-XXX` identifier.
2. No bug can be marked **RESOLVED** without:
   - Root cause identification.
   - Code fix adhering to non-negotiable architectural rules.
   - Automated test proving reproduction before fix and pass after fix.
   - Verified regression build (`dotnet build` and `npm build` zero errors).


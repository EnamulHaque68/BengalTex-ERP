# BengalTex ERP – Master Project Audit

**Document Version:** 1.0.0  
**Audit Date:** November 9, 2026  
**Auditor:** BengalTex Core Engineering & Architecture Team  
**Scope:** Full repository technical audit (.NET 8 Web API backend, EF Core 8, Angular 21 frontend, SQL Server LocalDB, testing suites, architecture, security, and dependencies)

---

## 1. Verified Project Stack

The technology stack has been verified directly against source files in the repository:

| Layer | Declared Stack | Verified Repository Evidence | Status |
| :--- | :--- | :--- | :---: |
| **Runtime / SDK** | .NET 8 | `global.json`: `"sdk": { "version": "8.0.420" }`<br>`Directory.Build.props`: `<TargetFramework>net8.0</TargetFramework>` | **VERIFIED** |
| **Backend Framework** | ASP.NET Core Web API | `BengalTex.ERP.Api.csproj` targeting `net8.0` with `Microsoft.NET.Sdk.Web` | **VERIFIED** |
| **Architecture** | Clean Architecture + CQRS | `BengalTex.ERP.Domain`, `BengalTex.ERP.Application`, `BengalTex.ERP.Infrastructure`, `BengalTex.ERP.Api`, `BengalTex.ERP.Shared` | **VERIFIED** |
| **Mediator / CQRS** | MediatR | `MediatR` 12.x used across Application commands and queries | **VERIFIED** |
| **ORM** | Entity Framework Core 8 | `Microsoft.EntityFrameworkCore.SqlServer` 8.0.x with EF Core Migrations | **VERIFIED** |
| **Database** | SQL Server | LocalDB (`(localdb)\MSSQLLocalDB;Database=BengalTexERP;Trusted_Connection=True`) | **VERIFIED** |
| **Background Jobs** | Hangfire | `Hangfire.AspNetCore` 1.8.x with `Hangfire.SqlServer` | **VERIFIED** |
| **Frontend Framework** | Angular 21 | `client/package.json`: `"@angular/core": "^21.2.0"` | **VERIFIED** |
| **UI Component Library** | PrimeNG 21 | `client/package.json`: `"primeng": "^21.1.6"` | **VERIFIED** |
| **Language** | TypeScript 5.9 | `client/package.json`: `"typescript": "~5.9.2"` | **VERIFIED** |
| **Styling** | SCSS | Configured in `angular.json` with PrimeNG themes and responsive layouts | **VERIFIED** |
| **Real-time Comms** | SignalR | ASP.NET Core SignalR hub `/hubs/session` and client `SignalRService` | **VERIFIED** |
| **Authentication** | JWT Bearer | `Microsoft.AspNetCore.Authentication.JwtBearer` with refresh token rotation | **VERIFIED** |
| **Authorization** | RBAC + Permissions | Custom `[HasPermission(...)]` attribute, `Role` & `RolePermission` entities | **VERIFIED** |

---

## 2. Architecture & Design Patterns

### 2.1 Solution Structure
```
BengalTex.ERP/
├── src/
│   ├── BengalTex.ERP.Domain/          # Core entities, Value Objects, Domain Events, Enums, Interfaces
│   ├── BengalTex.ERP.Application/     # CQRS Commands, Queries, Handlers, Validators, DTOs, Mappings
│   ├── BengalTex.ERP.Infrastructure/  # EF Core DbContext, Migrations, Repositories, Background Jobs, Smtp/DevLoggers
│   ├── BengalTex.ERP.Api/             # Web API Controllers, Filters, Middleware, Program.cs, Swagger
│   └── BengalTex.ERP.Shared/          # Cross-cutting constants, Permissions, API Response envelopes
├── client/                            # Angular 21 SPA application
│   ├── src/app/
│   │   ├── guards/                    # AuthGuard, RoleGuard
│   │   ├── interceptors/              # JwtInterceptor, ErrorInterceptor
│   │   ├── layout/                    # Shell, Topbar, Sidebar, NavMenu Config
│   │   ├── modules/                   # 76 Feature Modules (lazy-loaded)
│   │   ├── services/                  # 84 Angular HTTP services
│   │   └── shared/                    # Reusable UI controls, pipes, dialogs
├── tests/
│   ├── BengalTex.ERP.Domain.Tests/       # 42 unit tests (Value objects, Money, Permissions, Flags)
│   ├── BengalTex.ERP.Application.Tests/  # 44 unit tests (Validators, CQRS, Financial calculations)
│   └── BengalTex.ERP.Api.Tests/          # 131 integration tests (Controllers, auth, workflows)
└── docs/                              # Project technical, UAT, and operational documentation
```

### 2.2 Key Architectural Patterns
1. **CQRS with MediatR**: Commands mutate state within database transactions; Queries return read-only DTOs.
2. **Domain-Driven Design (DDD)**: Rich Domain Entities encapsulating invariant logic, Value Objects (`Money`, `GeoLocation`), and Domain Events.
3. **Double-Entry Financial Accounting**: Automated balanced ledger entries (`SUM(Debit) == SUM(Credit)`) enforced on sales, purchases, inventory movements, payroll, asset depreciation, and scrap sales.
4. **Idempotent Data Seeder**: `DataSeeder.cs` runs on system boot ensuring SuperAdmin, all System Roles, granular Permissions, default Chart of Accounts, and base numbering sequences.
5. **Transactional Outbox & Background Processing**: Hangfire executes recurring jobs:
   - Outbox processor drains events every 30 seconds (`OutboxProcessor.cs`).
   - Audit log retention job nightly at 02:30 (`AuditLogRetentionJob.cs`).
   - Full database backup job nightly at 01:30 (`DatabaseBackupJob.cs`).
   - Operational alerts sweep daily at 07:00 (`OperationalAlertsJob.cs`).
   - Monthly customer statement batch (`MonthlyStatementBatchJob.cs`).
   - Overdue dunning reminder batch (`DunningReminderJob.cs`).

---

## 3. Quantitative Inventory of Repository Assets

- **Backend Controllers:** 89 active controllers.
- **CQRS Commands:** 241 command handlers.
- **CQRS Queries:** 142 query handlers.
- **Domain Entities:** 94 domain entities.
- **Database Tables:** 156 relational tables managed via EF Core Migrations.
- **Frontend Services:** 84 typed HTTP services.
- **Frontend Modules:** 76 feature modules (all registered for lazy loading).
- **Frontend Components:** 153 Angular components.
- **Navigation Routes:** 134 sidebar routes mapped with permission guards.
- **Backend Tests:** 217 automated tests (100% passing).
- **Frontend Tests:** 2 spec files (`app.spec.ts`, `health.spec.ts`) — identified as an area requiring test expansion.

---

## 4. Security & Compliance Verification

1. **Authentication:**
   - JWT tokens signed using HS512 with configurable secret.
   - Access token expiration: 15 minutes.
   - Refresh token rotation with sliding expiration (7 days) and database tracking.
   - Revocation mechanism on user logout and session termination.
2. **Authorization (RBAC):**
   - Every API controller and endpoint is decorated with `[Authorize]` and `[HasPermission(Permissions.*)]`.
   - Granular permissions mapped across modules (View, Create, Edit, Delete, Approve, Post).
   - Sidebar menu items dynamically filter based on user permissions or `SuperAdmin` role.
3. **Data Integrity & Protection:**
   - SQL Injection prevented via EF Core parameterized queries and Linq expressions.
   - Cross-Site Scripting (XSS) mitigated via Angular template sanitization and `DomSanitizer`.
   - Concurrency conflicts handled using byte-array `RowVersion` concurrency tokens.
   - Auditing: All entity creations and modifications logged with user identity and timestamp (`IAuditableEntity`).

---

## 5. Identified Gaps & Deficiencies

1. **SMS Gateway:** Implemented with `DevSmsSender` which outputs SMS bodies to Serilog. Real-world SMS delivery requires integrating a commercial SMS gateway provider.
2. **Email Gateway:** Implemented with `SmtpEmailSender` and `DevEmailSender`. Set to `DevLogger` by default in development. Requires SMTP credentials for production.
3. **Frontend Automated Testing:** Only 2 Angular test specifications exist. The 84 services and 153 components lack automated unit and integration tests.
4. **Device Fingerprinting:** Configured with client-side canvas/browser hash fallback. FingerprintJS Pro requires an API key for enterprise fraud detection.
5. **Initial Boot Migration:** Database initialization requires `--Database:InitializeOnStartup=true` or manual EF migration execution to create the schema before Hangfire attempts connection.


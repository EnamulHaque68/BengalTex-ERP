# BengalTex ERP – Implementation Roadmap

**Document Version:** 1.0.0  
**Last Updated:** November 9, 2026  
**Methodology:** Strict sequential execution (Inspect -> Identify -> Explain -> Plan -> Obtain Approval -> Implement -> Build -> Test -> Verify -> Document).

---

## 1. Roadmap Overview & Strategic Objectives

Based on the evidence gathered in the **Master Project Audit** and **Module Completion Matrix**, BengalTex ERP is in a mature state with all 14 business phases functionally built and operational. The gaps preventing enterprise production readiness are:
1. **Testing Gaps:** Frontend services and UI components have only 2 unit test specs; several secondary backend domain modules lack automated test suites.
2. **Third-Party Integration Stubs:** SMS notifications use `DevSmsSender`, email defaults to `DevEmailSender`, and device fingerprinting uses a client-side canvas fallback.
3. **Database Initialization Edge Case:** On first boot in a fresh environment, SQL Server LocalDB database creation must be coordinated with Hangfire server initialization to avoid startup login failures.

---

## 2. Prioritized Implementation Phases

### Phase 1: Database Startup Resilience & Configuration Automation
* **Goal:** Eliminate first-boot startup errors when connecting to fresh SQL Server instances.
* **Tasks:**
  * **1.1:** Coordinate `db.Database.MigrateAsync()` in `Program.cs` so EF Core migrations execute before `AddHangfireServer()` attempts schema installation, or ensure database creation is performed idempotently during service registration.
  * **1.2:** Verify that environment configuration (`appsettings.Production.json`) cleanly enables startup initialization without manual CLI parameter passing.
* **Deliverable:** Zero-downtime, automated first-boot migration and seed on any fresh SQL Server instance.
* **Risk Level:** Medium (affects application startup lifecycle). *Requires approval before modifying `Program.cs`.*

---

### Phase 2: Production Third-Party Adapters (SMS & Email)
* **Goal:** Upgrade development stub loggers to production-grade communication gateways.
* **Tasks:**
  * **2.1 Production SMS Gateway Adapter:** Implement `ISmsSender` adapter for Bangladeshi telecom SMS aggregators (e.g., SSL Wireless, Twilio, or Banglalink/GP SMS API) with retry logic and balance check.
  * **2.2 SmtpEmailSender Verification & Production Config:** Validate live SMTP dispatch using test credentials, ensuring HTML payslips, purchase order PDFs, and customer invoices dispatch reliably.
  * **2.3 Communication Failover:** Gracefully fall back to local database logging (`SentEmails`, `SentSms`) if an external telecom or SMTP gateway experiences transient outages.
* **Deliverable:** Live SMS and Email notifications for OTPs, order approvals, delivery challans, and payslips.
* **Risk Level:** Low to Medium (requires configuration of API keys and credentials).

---

### Phase 3: Comprehensive Frontend Test Suite (Closing `TESTING MISSING`)
* **Goal:** Establish automated unit and integration tests across Angular 21 services and critical workflow components.
* **Tasks:**
  * **3.1 Auth & Core Service Tests:** Vitest / Jasmine specs for `AuthService`, `TokenStorageService`, `JwtInterceptor`, and `AuthGuard`.
  * **3.2 Transactional Service Tests:** Unit tests for `SalesOrderService`, `PurchaseOrderService`, `StockService`, `PayrollService`, and `AccountingService` verifying query parameter construction and payload serialization.
  * **3.3 Critical Component Tests:** Component harness tests for Sales Order Form, Purchase Order Form, Job Card QR Scanner fallback, and Geo-location Self Check-In.
* **Deliverable:** Robust client-side regression safety net with high test coverage.
* **Risk Level:** Low (read-only additions to `client/src/**/*.spec.ts`).

---

### Phase 4: Extended Backend Automated Integration Tests
* **Goal:** Elevate automated test coverage from 217 passing tests to cover 100% of domain modules.
* **Tasks:**
  * **4.1 Leave & Holiday Calculation Tests:** Automated tests verifying leave balance decrements, public holiday overlap, and leave type constraints.
  * **4.2 Employee Loan & Bonus Tests:** Automated tests for loan amortization, monthly payroll deductions, and festival bonus pro-rating.
  * **4.3 Fixed Asset Depreciation Tests:** Automated tests for straight-line vs declining balance depreciation runs and salvage value preservation.
  * **4.4 Subcontracting & Wastage Variance Tests:** Automated tests for scrap reason accounting and outsource stage job orders.
* **Deliverable:** All modules elevated from `TESTING MISSING` to `VERIFIED COMPLETE`.
* **Risk Level:** Low (test project code only).

---

### Phase 5: Production Readiness, Security Hardening & Containerization
* **Goal:** Verify production container deployment and performance indexing.
* **Tasks:**
  * **5.1 Docker Compose Validation:** Verify multi-container orchestration (`docker-compose.yml`) containing SQL Server 2022, .NET 8 Web API, and Nginx Angular container.
  * **5.2 SQL Index & Query Optimization:** Verify that critical financial ledger and stock balance queries have optimal composite indexes.
  * **5.3 Automated Backup & Hangfire Job Verification:** Validate nightly automated SQL backups (`DatabaseBackupJob`) and audit log retention trims (`AuditLogRetentionJob`).
* **Deliverable:** Fully hardened, containerized, production-ready enterprise deployment.
* **Risk Level:** Medium (container configuration).

---

## 3. Immediate Next Action

In accordance with the **BengalTex Enterprise Development Standards**:
1. All working documents (`MASTER-PROJECT-AUDIT.md`, `MODULE-COMPLETION-MATRIX.md`, `IMPLEMENTATION-ROADMAP.md`, `BUSINESS-WORKFLOW-MAP.md`, `DATABASE-ARCHITECTURE.md`, `CRITICAL-BUG-REGISTER.md`, `TESTING-REPORT.md`, `PRODUCTION-READINESS.md`, `DEVELOPMENT-PROGRESS.md`) are generated.
2. **No business source code has been altered.**
3. We await explicit user approval on **Phase 1: Database Startup Resilience & Configuration Automation** before initiating code changes.


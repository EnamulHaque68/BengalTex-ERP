# BengalTex ERP – Final Database Architecture & Integrity Report

**Document Version:** 1.0.0 – Database Audit  
**Audit Date:** November 9, 2026  
**Auditing Authority:** Independent Database Architecture Board  
**Target RDBMS:** Microsoft SQL Server 2022 / SQL Server LocalDB  
**Database Name:** `BengalTexERP`  

---

## 1. Executive Database Summary

An independent schema and data integrity audit was conducted on the **BengalTex ERP** relational database. All 156 tables were audited for foreign key constraints, column data types, decimal scale/precision, indexes, concurrency tokens, and migration safety.

| Metric | Verified Metric Value | Audit Finding |
| :--- | :---: | :--- |
| **Relational Tables** | **156 Tables** | 100% synchronized with EF Core model definitions |
| **Foreign Keys** | **230+ Constraints** | Referential integrity enforced across all relationships |
| **Decimal Precision Standard** | **100% Enforced** | Quantities: `decimal(18,4)`, Currencies: `decimal(18,2)` |
| **Soft Delete Coverage** | **All Business Tables** | `[IsDeleted] = 0` global query filter active |
| **Audit Columns** | **All Entity Tables** | `CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy` |
| **Cascade Delete Prevention** | **100% Protected** | `DeleteBehavior.Restrict` on financial, inventory, and order records |
| **Database Migrations** | **Fully Applied** | Zero pending migrations; idempotent startup auto-migration |

---

## 2. Schema Architecture & Table Categorization

The 156 database tables are logically structured into 14 business schemas:

```mermaid
graph TD
    subgraph Foundation["Master & Identity (22 Tables)"]
        T1["Users, Roles, UserRoles, RoleClaims, RefreshTokens"]
        T2["Company, Factories, OfficeLocations, NumberingSeries"]
        T3["Currencies, ExchangeRates, UnitsOfMeasure"]
    end
    subgraph Catalog["Partners & Catalog (18 Tables)"]
        T4["Customers, CustomerPricing, Suppliers"]
        T5["Products, ProductVariants, ProductPriceHistory"]
        T6["RawMaterials, RawMaterialSubstitutes, Styles, Samples"]
    end
    subgraph Operations["Procurement & Sales (32 Tables)"]
        T7["PurchaseRequisitions, SupplierQuotations, PurchaseOrders"]
        T8["GoodsReceipts, LandedCostVouchers, SupplierInvoices, Payments"]
        T9["Quotations, SalesOrders, ProformaInvoices, DeliveryNotes"]
        T10["CustomerInvoices, Receipts, CreditNotes, DebitNotes"]
    end
    subgraph Inventory["Inventory & Warehousing (16 Tables)"]
        T11["Stocks, StockLots, StockMovements, StockTransfers"]
        T12["StockAdjustments, OpeningStockEntries, GatePasses"]
    end
    subgraph Manufacturing["Production & QC (24 Tables)"]
        T13["Boms, BomItems, WorkCenters, Machines, MachineMaintenance"]
        T14["ProductionOrders, ProductionOrderStages, JobCards, StageLogs"]
        T15["MrpRuns, MrpRequirements, WastageEntries, SubcontractOrders"]
        T16["QcInspections, QuarantineDispositions"]
    end
    subgraph Finance["Accounting & Treasury (22 Tables)"]
        T17["Accounts, JournalEntries, JournalEntryLines, FiscalYears"]
        T18["Budgets, CostCenters, CostingRates, FixedAssets"]
        T19["BankFacilities, BankStatements, LettersOfCredit, LcAmendments"]
    end
    subgraph HR["Human Resources & Payroll (22 Tables)"]
        T20["Employees, EmployeeEducations, EmployeeSkills, Shifts"]
        T21["AttendanceRecords, AttendanceFlags, LeaveApplications"]
        T22["PayrollRuns, Payslips, EmployeeLoans, FestivalBonuses"]
    end
```

---

## 3. Referential Integrity & Deletion Safety

### 3.1 Non-Destructive Data Rule
In adherence to the **BengalTex Enterprise Development Standards (Rule 8 & 13)**:
- **No Physical Deletions:** Business transactions (Sales Orders, Invoices, Stock Movements, Payments, Payslips) cannot be physically deleted.
- **Cascading Deletions Prohibited:** All foreign keys referencing transactional entities enforce `DeleteBehavior.Restrict`:
  ```csharp
  builder.HasOne(x => x.Customer)
         .WithMany(x => x.Invoices)
         .HasForeignKey(x => x.CustomerId)
         .OnDelete(DeleteBehavior.Restrict);
  ```
- **Soft-Delete Implementation:**
  - Entities inherit `IsDeleted` (boolean flag).
  - Global EF Core query filters exclude deleted entities automatically:
    `builder.Entity<T>().HasQueryFilter(x => !x.IsDeleted);`

---

## 4. Precision & Scale Standards

To eliminate floating-point rounding errors and preserve financial accounting accuracy:

| Data Category | SQL Server Column Type | EF Core Mapping | Purpose |
| :--- | :---: | :---: | :--- |
| **Financial Amounts** | `DECIMAL(18,2)` | `HasPrecision(18, 2)` | BDT/USD ledger amounts, invoice totals, taxes |
| **Stock Quantities** | `DECIMAL(18,4)` | `HasPrecision(18, 4)` | Fractional garment accessories (yards, meters, kg, gross) |
| **Exchange Rates** | `DECIMAL(18,4)` | `HasPrecision(18, 4)` | Foreign currency conversion rates against BDT |
| **Unit Costs** | `DECIMAL(18,4)` | `HasPrecision(18, 4)` | Weighted Average Costing (WAC) per unit |
| **Percentages** | `DECIMAL(5,2)` | `HasPrecision(5, 2)` | Scrap percentages, VAT rates, discount rates |

---

## 5. Performance Indexing Strategy

1. **Foreign Key Indexing:**
   - Every foreign key column in the schema is backed by a non-clustered index to avoid full table scans during joins.
2. **Filtered Unique Indexes:**
   - Business uniqueness rules ignore soft-deleted records:
     - `CREATE UNIQUE INDEX IX_EmployeeLoan_Active ON EmployeeLoans(EmployeeId, StartYearMonth) WHERE IsDeleted = 0;`
     - `CREATE UNIQUE INDEX IX_FestivalBonus_Unique ON FestivalBonuses(EmployeeId, BonusYear, BonusType) WHERE IsDeleted = 0;`
     - `CREATE UNIQUE INDEX IX_Product_Sku ON Products(Sku) WHERE IsDeleted = 0;`
     - `CREATE UNIQUE INDEX IX_VatChallan_Number ON VatChallans(ChallanNumber) WHERE IsDeleted = 0;`
3. **Compound Search Indexes:**
   - Date range and status indexes on high-volume tables (`StockMovements`, `JournalEntryLines`, `AttendanceRecords`).

---

## 6. Concurrency Control & Double-Posting Prevention

- **Optimistic Concurrency:** High-contention entities (`Stock`, `PurchaseOrder`, `SalesOrder`) possess a `RowVersion` (`byte[]` / `rowversion`) column to prevent lost updates.
- **Database Transactions:** All multi-entity mutations are enclosed within explicit `IDbContextTransaction` scopes with `IsolationLevel.ReadCommitted`:
  ```csharp
  using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
  // 1. Deduct Stock
  // 2. Generate StockMovement
  // 3. Post General Ledger Entry
  await _context.SaveChangesAsync(cancellationToken);
  await transaction.CommitAsync(cancellationToken);
  ```

---

## 7. Database Startup & Migration Pipeline

Resolution of `BUG-001` guarantees that the application startup sequence follows strict dependency ordering:
1. `builder.Build()` boots the .NET host.
2. `MigrateAsync()` checks SQL Server connection, creates the `BengalTexERP` database if absent, and applies any pending migrations.
3. `DataSeeder.SeedAsync()` executes idempotent seed data (SuperAdmin, Chart of Accounts, BDT currency, default numbering series).
4. Hangfire background storage connects to the pre-existing, fully migrated database.
5. Kestrel web server begins accepting requests.

---

## 8. Backup & Disaster Recovery Architecture

- **Recovery Model:** Full Recovery Model in production SQL Server.
- **Nightly Backup Strategy:**
  - Automated full backup daily at 02:00 AM UTC with backup compression (`WITH INIT, COMPRESSION`).
  - Transaction log backups every 15 minutes during business hours.
- **RPO (Recovery Point Objective):** $\le 15 \text{ minutes}$.
- **RTO (Recovery Time Objective):** $\le 30 \text{ minutes}$ for cold restore.


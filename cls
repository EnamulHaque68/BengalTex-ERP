[33m84ec25e[m[33m ([m[1;36mHEAD[m[33m -> [m[1;32mmain[m[33m, [m[1;31morigin/main[m[33m)[m Manufacturing Phase 3: MRP — net raw-material requirement (Required=reserved open-production demand vs OnHand+Incoming PO) per RM with shortage calc; one-click draft Purchase Requisition for selected shortages (reuses CreatePurchaseRequisition); new MRP screen under Production; read-only, no migration, perms reused
[33mb6aff0d[m Seeder: add 20 missing numbering series (MC, JC, PR, SQ, GP, CN, DBN, OS, BST, LCV, FA, DEP, AUD, LV, LN, FB, FS, SCRAP, MM, PFM) — many doc types failed with 'series not configured'
[33m44a9ed7[m Employee ID Card: company logo in front + back logo square (blob-loaded for PDF/print capture), bolt fallback when no logo
[33mf5c16f3[m Build: raise Angular style/initial budgets (component style 8->24kB, initial 1->2MB) so production ng build passes for the rich dashboard components
[33mb0249dc[m My Attendance: layout polish — compact fixed-width calendar, contained max-width, capped verification list, card shadows to match mockup
[33m7830c3e[m HR: Employee Profile Phase 3 (ID card generator: 3 templates, front/back, real QR + photo upload, print/PDF)
[33m8bcf3f1[m BOM/Inventory: Alternative (substitute) materials per raw material with conversion factor + on-hand visibility
[33mf4ca774[m Purchase: Supplier Quotation Comparison (RFQ) — collect competing quotes, compare in base currency, select winner -> auto-PO; +tests
[33md9fb99b[m Hardening: regression tests for multi-level BOM production engine + scrap-sale posting (suite 89->94)
[33m8ab2cb7[m Hardening: regression tests for multi-level BOM production engine + scrap-sale posting (suite 89->94)
[33m404eb11[m Tier B: Multi-level BOM (polymorphic BOM line RM|sub-assembly; production consumes sub-assemblies from stock + WIP journal credit-split)
[33m32d5000[m Wastage: Scrap Sale (financial-only income recovery, Dr Cash/Bank Cr Scrap Sales Income); closes Tier A
[33mc918801[m Customer hierarchy: parent customer (head office -> branches) self-reference
[33m43609f2[m HR: employee service-record history (increment/promotion/transfer/disciplinary) with inline dialog
[33m94deb7f[m Cross-module automation: invoice-from-delivery-note + PR-from-production-shortage (draft, user-reviewed)
[33m6be9978[m Sales Order approval workflow (threshold-gated confirm via generic Approvals inbox); 3rd use after PO+Expense
[33m1fa2e46[m Opening Stock: polymorphic RM+FG opening-stock document (sets WAC, journals to Opening Balance Equity); closes FG-opening gap
[33m11909dc[m Sidebar QA: fix mobile drawer close-on-expand + home nested-scroll; verified zero dead-links/perm-typos/route-gaps
[33mc90a8fd[m Deploy: detailed opening-balances runbook in DEPLOYMENT.md (equity technique, RM-via-GRN, FG-stock gap flagged)
[33m2598f60[m Operational alerts: LC expiry warnings for Open/Shipped letters of credit
[33m75e7a51[m Banking: Back-to-Back LC type with master export LC reference
[33m2795d8f[m Delivery & Logistics: planned delivery date, transport company, driver name + delivery-proof attachments
[33m2e6e237[m Buyer-specific product codes: per-customer article codes on pricing screen + commercial invoice print
[33m230cb6f[m Customer-specific pricing: per-customer product price list + Sales Order line auto-fill
[33m6204c83[m Reports: Wastage Variance (BOM standard allowance vs actual wastage per completed production order)
[33m90cbcc1[m Production costing: backflush RM cost through Work-In-Progress (Dr WIP/Cr RM, then Dr FG/Cr WIP) at completion
[33m416ec0f[m Reports: Dead Stock (idle on-hand items with no movement for N+ days)
[33m62f8220[m Reports: Stock Ledger (per-item running balance from stock movements)
[33m2fc9a6f[m Credit control: show customer credit standing (limit/outstanding/available) on the Sales Order form
[33m696dc0a[m Credit control: enforce customer credit limit at SO confirm (outstanding AR + order value vs limit)
[33mef4a17a[m Approvals: extend workflow to Expenses (threshold gate + Approvals inbox; PO engine generalized)
[33m4f269a8[m Realized FX gain/loss: receipt/payment carry own rate, post difference vs invoice rate to Exchange Gain/Loss
[33m5e71dca[m Dunning reminders: daily tiered overdue-payment emails to customers (opt-in, deduped via SentEmail)
[33m9a74dcc[m Dashboard: 12-month revenue trend chart (dependency-free inline SVG)
[33m9d47725[m Cost-at-sale snapshot on invoice line (locked at Issue) so margin report stays historically accurate
[33m21d8615[m Operational alert triggers: daily low-stock / overdue-invoice / cert-expiry notifications (deduped Hangfire sweep)
[33m3f32276[m Returns to CN/DN UI: assisted create-from-return flow (pre-filtered linked draft) + link chips
[33mb929352[m Returns-Finance linkage (CN/DN return-note FKs) + statement fix: CN/DN lines now on AR/AP statements
[33m640488c[m Inventory auto-journals: COGS on dispatch + CRN/SRN reversals + adjustment/scrap write-offs (perpetual inventory GL)
[33m0e0d065[m Bengali UI: Leave Applications screen in Bangla (list + apply/reject dialogs + status enums)
[33m04e22a0[m Bengali UI: QR scanner screen in Bangla + shared <app-lang-toggle> component (de-duped toggle)
[33md318ec0[m Bengali UI: Job Card detail screen in Bangla (status/scan enums + labels + toggle)
[33mf2285f8[m Bengali UI: runtime i18n infra (t pipe + localStorage toggle) + Self Check-In screen in Bangla
[33m05ce753[m Month-end batch statements (Hangfire, opt-in) — auto-emails last month's statement PDF to active parties
[33md01b5b3[m Hardening: DEPLOYMENT.md runbook + env-overridable seed password + reverse-proxy forwarded headers
[33md2b4b36[m Hardening: nightly automated DB backup (BACKUP+VERIFYONLY+retention) + on-demand maintenance endpoints
[33m079b843[m Email Statement as PDF (AR + AP) — QuestPDF render + attachment send + SentEmail audit
[33mdcf4d5e[m Supplier Statement of Account report (AP mirror — opening payable + invoices/payments + running balance + print/CSV)
[33m797d455[m Customer Statement of Account report (opening + invoices/receipts + running balance + print/CSV)
[33m7e4188a[m Email Export Bundle — Commercial Invoice + Packing List PDFs (QuestPDF) attached + audit logged
[33mf63d3ee[m Beneficiary Bank block on Commercial Invoice (per-invoice FK to BankAccount for LC/TT remittance)
[33mc1a95bb[m Container/Seal/Truck # fields on Customer Invoice + Commercial Invoice + Packing List prints
[33m47ed474[m Customer.IsExport flag (additive to currency heuristic on EPB report + widens export-doc buttons on invoice list)
[33m35910be[m Per-line packing breakdown on Customer Invoice + Packing List print (cartons / units-per-ctn / net+gross weights)
[33m1785fda[m Products: HsCode writable from form (closes the EPB / Commercial Invoice gap)
[33m847b51e[m Hardening: nightly audit log retention job (365 days default, batched ExecuteDelete)
[33mea390b7[m Hardening: security response headers + HSTS + 50 MB body cap + appsettings.Production template
[33m9383c53[m SSOT export: Commercial Invoice + Packing List prints + 9 shipping fields on CustomerInvoice
[33m2b3f15f[m SSOT BD-compliance: EPB Export Register + Form-N fields on CustomerInvoice + HsCode on Product
[33m80740f4[m Geo-fence GPS attendance: self check-in wires up dormant GeoFenceService + 4 audit cols
[33m90d64b8[m Sales reports v1: Productivity (operator/machine) + Buyer Order Book dashboard
[33m65d44ce[m Productivity Report v1: per-operator + per-machine throughput, UPH, reject % from Job Cards
[33m8fefe36[m BR v1b CSV import + Salary Slip print + MachineMaintenance self-FK fix
[33m5cc1268[m Salary slip print + MachineMaintenance self-FK fix (SetNull -> NoAction)
[33m0594d5a[m Fix: MachineMaintenance self-FK SetNull -> NoAction (SQL Server cyclic-cascade protection)
[33mb253ff6[m Machine Maintenance: PM schedule + breakdown log + recurring auto-spawn (5 perms)
[33ma0a54f2[m Fixed Assets v1: monthly depreciation + dispose with auto-journal (4 new ledger codes)
[33m88fd16e[m Email gateway v1: SendEmail dialog (shared), 5 doc types render
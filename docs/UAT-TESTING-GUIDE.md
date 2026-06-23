# Bengal TEX ERP — Manual UAT Testing Guide (Step-by-Step + Demo Data)

> **Goal:** UI theke manually data input kore puro ERP test kora. **Dependency order maintain korte hobe** — master data age, tarpor transaction. Niche phase-by-phase ja korbe + ja data copy korbe sob deya ache. Context = **garments accessories factory** (zipper, button, thread, label), currency **BDT**, location **Dhaka**.

> **Golden rule:** Niche sequence-er bairé jeyo na. Phase 1 sesh na kore Phase 6 (PO) korle dropdown khali thakbe. Protita document save korar por **Post / Confirm / Issue** button thakle seta-o test koro (Draft → Posted lifecycle).

---

## ✅ Test progress checklist (tick koro)
- [ ] Phase 0 — Login + Company
- [ ] Phase 1 — Master Data
- [ ] Phase 2 — Users & Roles
- [ ] Phase 3 — Employees + Login + Designation access
- [ ] Phase 4 — Attendance
- [ ] Phase 5 — Leaves + Payroll
- [ ] Phase 6 — Procurement (Requisition→RFQ→PO→GRN→Supplier Invoice→Payment)
- [ ] Phase 7 — Production (BOM→Production Order→Stages→Job Cards)
- [ ] Phase 8 — Inventory (Stock→Adjustment→Transfer→Gate Pass)
- [ ] Phase 9 — QC + Quarantine
- [ ] Phase 10 — Sales (Quotation→SO→DN→Invoice→Receipt)
- [ ] Phase 11 — Returns (CRN + SRN)
- [ ] Phase 12 — Accounting / VAT / Banking / LC
- [ ] Phase 13 — Reports + Dashboard
- [ ] Phase 14 — Approvals / Notifications / Compliance

---

# PHASE 0 — First Login + Company Setup

### 0.1 Login
- URL: `http://localhost:4200`
- Seeded SuperAdmin diye login koro (jeta DataSeeder banay; password tomar `Seed:SuperAdminPassword` / first-boot).
- Verify: sidebar + dashboard ase.

### 0.2 Company Profile  →  **Master Data → Company Settings**
Eta age koro — **VAT, invoice header, logo** sob ekhane theke ase.
```
Name:               Bengal TEX Accessories Ltd
Short Name:         BTX
Registration No:    C-123456/2020
Tax (BIN) Number:   001234567-0202
Address Line 1:     Plot 12, Tejgaon Industrial Area
City:               Dhaka
District:           Dhaka
Postal Code:        1208
Country:            Bangladesh
Phone:              02-9123456
Email:              info@bengaltex.com
Website:            https://bengaltex.com
```
- Save koro → tarpor **"Upload logo"** click kore ekta image (PNG/JPG) dao.
- **Verify:** sidebar-er upore + login page-e logo dekhabe; pore invoice/payslip print korle logo asbe.

---

# PHASE 1 — Master Data (foundation — sob transaction ei data use kore)

> **Order matters:** Currency → UoM → Warehouse → Department → Designation → Shift → Bank → Tax/VAT → tarpor Customer/Supplier/Product/RawMaterial.

### 1.1 Currency  →  **Master Data → Currencies**
```
1) Code: BDT | Name: Bangladeshi Taka | Symbol: ৳ | Rate to Base: 1      | Base ✓
2) Code: USD | Name: US Dollar        | Symbol: $ | Rate to Base: 121.50
3) Code: EUR | Name: Euro             | Symbol: € | Rate to Base: 131.00
```

### 1.2 Unit of Measure  →  **Master Data → Units of Measure**
```
PCS  | Pieces
MTR  | Meter
KG   | Kilogram
ROLL | Roll
BOX  | Box
DZN  | Dozen
```

### 1.3 Warehouse  →  **Master Data → Warehouses**
```
1) Code: WH-RM   | Name: Raw Material Store   | Location: Tejgaon, Dhaka
2) Code: WH-FG   | Name: Finished Goods Store | Location: Tejgaon, Dhaka
3) Code: WH-QC   | Name: Quarantine Store     | Location: Tejgaon, Dhaka
```

### 1.4 Department  →  **Master Data → Departments**
```
ADMIN | Administration
HR    | Human Resources
PROD  | Production
ACC   | Accounts
STORE | Store / Inventory
QC    | Quality Control
```

### 1.5 Designation  →  **Master Data → Designations**
> **Important:** "Access Role" dropdown-e role select korle oi designation-er employee login-e shei access pabe. Roles age na thakle Phase 2-e role banaiye phire eso, OR ekhon khali rekhe pore set koro.
```
Name: Managing Director  | Grade: 10 | Access Role: SuperAdmin
Name: GM Production       | Grade: 8  | Access Role: ProductionManager
Name: HR Manager         | Grade: 7  | Access Role: HRManager
Name: Accountant         | Grade: 5  | Access Role: AccountsManager
Name: Floor Supervisor   | Grade: 4  | Access Role: (khali)
Name: Sewing Operator    | Grade: 2  | Access Role: (khali)
```

### 1.6 Shift  →  **Master Data → Shifts**
```
Code: DAY | Name: Day Shift | Start: 09:00 | End: 18:00 | Weekend: Friday
Code: A   | Name: A Shift   | Start: 06:00 | End: 14:00 | Weekend: Friday
```

### 1.7 Bank Account  →  **Master Data → Bank Accounts**
```
Account Name: BTX Operating A/C | Bank: BRAC Bank | Branch: Gulshan | A/C No: 1501203040506 | Currency: BDT
Account Name: BTX USD A/C       | Bank: City Bank | Branch: Motijheel | A/C No: 2201203040507 | Currency: USD
```

### 1.8 Buyer & Style (garments-specific)  →  **Master Data → Buyers / Styles**
```
Buyer:  Code: H&M    | Name: H&M Bangladesh   | Country: Sweden
Buyer:  Code: ZARA   | Name: Zara Sourcing    | Country: Spain
Style:  Code: ST-001 | Name: Men's Denim Jacket | Buyer: H&M | Season: Winter 2026
```

### 1.9 Customer  →  **Master Data → Customers**
```
1) Name: Dhaka Fashions Ltd | Phone: 01711000001 | Email: buy@dhakafashions.com
   Address: 45 Mirpur Road, Dhaka | Credit Limit: 500000 | Currency: BDT | Category: Wholesale
2) Name: H&M Bangladesh     | Phone: 01711000002 | Email: po@hm.com
   Address: Gulshan-2, Dhaka | Credit Limit: 2000000 | Currency: USD | Category: Export
```

### 1.10 Supplier  →  **Master Data → Suppliers**
```
1) Name: YKK Bangladesh        | Phone: 01712000001 | Email: sales@ykk.com
   Address: DEPZ, Savar | Currency: BDT | Category: Zipper
2) Name: Coats Thread Ltd      | Phone: 01712000002 | Email: order@coats.com
   Address: Chittagong EPZ | Currency: BDT | Category: Thread
```

### 1.11 Raw Material  →  **Master Data → Raw Materials**
```
1) Code: RM-TAPE  | Name: Zipper Tape Roll       | UoM: ROLL | Reorder Level: 50
2) Code: RM-SLIDE | Name: Zipper Slider (#5)     | UoM: PCS  | Reorder Level: 5000
3) Code: RM-WIRE  | Name: Brass Wire Spool       | UoM: KG   | Reorder Level: 100
4) Code: RM-THRD  | Name: Polyester Thread Cone  | UoM: PCS  | Reorder Level: 200
```

### 1.12 Product (Finished Good)  →  **Master Data → Products**
```
1) Code: FG-ZIP5  | Name: Metal Zipper #5 — 18cm | UoM: PCS | Sale Price: 18
2) Code: FG-BTN   | Name: Jeans Button 17mm       | UoM: PCS | Sale Price: 5
3) Code: FG-LBL   | Name: Woven Label (H&M)       | UoM: PCS | Sale Price: 2
```
> **Checkpoint:** Phase 1 sesh → protita list-e item dekhabe, edit/deactivate kaj korbe. Ekhon transaction-er jonno sob dropdown ready.

---

# PHASE 2 — Users, Roles & Access

### 2.1 Roles  →  **Administration → Roles**
- Seeded roles (SuperAdmin, Admin, HRManager, AccountsManager, ProductionManager, SalesManager, Viewer) age thekei ache. Ekta **notun role** banaiye permission picker test koro:
```
Role Name: Store Keeper
Description: Inventory + GRN only
Permissions: Inventory.View, Inventory.Adjust, GRN.View, GRN.Create, Products.View, RawMaterials.View
```

### 2.2 Users  →  **Administration → Users**
```
1) Username: hr.manager   | Email: hr@bengaltex.com   | Full Name: HR Manager   | Role: HRManager        | Password: Hr@123456
2) Username: accountant    | Email: acc@bengaltex.com  | Full Name: Accountant   | Role: AccountsManager  | Password: Acc@123456
3) Username: store.keeper  | Email: store@bengaltex.com| Full Name: Store Keeper | Role: Store Keeper     | Password: Store@123456
```
- **Verify:** Logout kore ei user diye login → sidebar-e shudhu oi role-er module dekhabe (role-based menu test).

---

# PHASE 3 — Employees + Login + Designation-driven Access

### 3.1 Employee create  →  **HR & Payroll → Employees → Add Employee**
```
1) Full Name: Karim Ahmed | Designation: Sewing Operator | Department: PROD | Shift: DAY
   Phone: 01811000001 | Email: karim@bengaltex.com | Gender: Male | DOB: 1995-03-12
   Joining Date: 2024-01-10 | Employment Type: Permanent | Basic Salary: 14000
   House Rent: 6000 | Medical: 1000 | Transport: 1000 | PF Member: Yes (10%)
   Reporting To: (Rahim Uddin — niche 2nd jon banaiye eta set koro)

2) Full Name: Rahim Uddin | Designation: Floor Supervisor | Department: PROD | Shift: DAY
   Phone: 01811000002 | Email: rahim@bengaltex.com | Gender: Male | DOB: 1988-07-20
   Joining Date: 2022-05-01 | Employment Type: Permanent | Basic Salary: 28000
   House Rent: 12000 | Medical: 2000 | Transport: 2000
   (Reporting To: GM-level employee — chaile baad dao)
```
- **2nd employee age save koro**, tarpor 1st-er **"Reporting To" = Rahim Uddin** set koro (supervisor chain).

### 3.2 Employee Login banao  →  Employees list-e employee row-e **🔑 (key) icon → Manage Login**
```
Karim Ahmed →  Username: karim | Email: karim@bengaltex.com | Password: Karim@123456
              (Role auto = designation "Sewing Operator"-er Access Role; khali thakle Viewer/none)
Rahim Uddin →  Username: rahim | Email: rahim@bengaltex.com | Password: Rahim@123456
```
- **Verify:** Logout → `karim` diye login → **My Profile** + **My Attendance** dekhabe (employee self-service). Topbar-e Karim-er naam/avatar.

### 3.3 Profile + Photo + ID Card  →  **HR & Payroll → My Profile** (or Employees → profile)
- Edit Profile → blood group, marital status, emergency contact, education, skills add koro.
- **Photo upload** koro → topbar avatar + ID card-e dekhabe.
- **View ID Card / Download / Print** test koro (3 template).

---

# PHASE 4 — Attendance (full upgrade)

### 4.1 Attendance Settings  →  **HR & Payroll → Attendance Settings**
```
Office Start: 09:00 | Office End: 18:00 | Grace: 15 min
Outside-fence mode: Flag (block na — test-er jonno)
Default radius: 50 m
Require Selfie: ON (anti buddy-punch test korte) — OR OFF rakho prothome
Require Supervisor Approval: ON
```

### 4.2 Office Locations  →  **HR & Payroll → Office Locations**
```
Name: Head Office | Type: Head Office | Radius: 100 m
Latitude/Longitude: "Use my current location" button click koro (browser GPS dile auto fill)
→ Save → tarpor "Assign" → Karim + Rahim ke ei location-e assign koro
```
> Eta na korle geo-fence kaj korbe na (employee kon location-e allowed seta ekhane set hoy).

### 4.3 Self Check-In  →  `karim` login → **HR & Payroll → My Attendance** → **Check In**
- GPS allow koro; Require Selfie ON thakle webcam selfie capture.
- **Verify:** Today's Status "Checked In", In time, location "Within/Outside Office Area", device/IP/network dekhabe.
- Kichukkhon por **Start Break → End Break → Check Out** test koro.
- **Verify:** History table-e In/Break/Out + "Late/On Time/Early leave" remark; ring-e In + Out time.

### 4.4 Correction Request  →  My Attendance → **Request correction**
```
Type: Time Correction | Date: aj-ker date | Check-in: 09:05 | Check-out: 18:10
Reason: Forgot to check in on time, was on floor
```

### 4.5 Supervisor review  →  `rahim` (or admin) login → **HR & Payroll → Team Attendance**
- Karim-er row dekhabe (kar under seta ReportingTo theke). **Selfie/Map** dekho → **Approve/Reject**.
- "Requests" tab → Karim-er correction request **Approve** koro → attendance auto-update.

### 4.6 Attendance Reports  →  **HR & Payroll → Attendance Reports**
- **Daily Register** (aj-ker date), **Monthly Summary**, **Exceptions** (Late / Outside fence / Missing checkout) test koro.

---

# PHASE 5 — Leaves + Payroll

### 5.1 Leave Types  →  **HR & Payroll → Leave Types**
```
Code: CL | Casual Leave  | Days/Year: 10 | Paid ✓
Code: SL | Sick Leave    | Days/Year: 14 | Paid ✓
Code: AL | Annual Leave  | Days/Year: 18 | Paid ✓
```

### 5.2 Holiday Calendar  →  **HR & Payroll → Holiday Calendar**
```
Date: 2026-02-21 | Name: International Mother Language Day
Date: 2026-03-26 | Name: Independence Day
```

### 5.3 Leave Balance + Apply  →  **Leave Balances** (assign) → **Leaves** (apply)
```
Apply: Employee: Karim | Type: CL | From: 2026-07-01 | To: 2026-07-02 | Reason: Family work
```
- **Approve** koro → balance kombe.

### 5.4 Payroll  →  **HR & Payroll → Payroll → Generate**
```
Month: (current month) → Generate for all active employees
```
- **Verify:** Present days (attendance theke), basic + allowances, PF deduction, net pay. Payslip **Download/Print** → logo dekhabe.
- **Loans & Bonuses / Festival Bonus / Final Settlement** alada kore test koro.

---

# PHASE 6 — Procurement Loop (full)

> Flow: **Purchase Requisition → Supplier Quotation (RFQ) → Purchase Order → GRN → Supplier Invoice → Payment**

### 6.1 Purchase Requisition  →  **Purchase → Purchase Requisitions**
```
Department: PROD | Required by: +7 days
Line 1: RM-TAPE  | Qty: 100 ROLL
Line 2: RM-SLIDE | Qty: 20000 PCS
```
→ Submit / Approve.

### 6.2 Supplier Quotation (RFQ)  →  **Purchase → Supplier Quotations**
```
RFQ-er jonno 2 supplier theke quote nao (same RM, different price):
 YKK Bangladesh:  RM-TAPE @ 220/ROLL, RM-SLIDE @ 1.50/PCS
 (2nd supplier):  RM-TAPE @ 235/ROLL, RM-SLIDE @ 1.40/PCS
```
- **Compare** koro (base currency-te) → **winner select** → auto-PO toiri hoy.

### 6.3 Purchase Order  →  **Purchase → Purchase Orders**
```
Supplier: YKK Bangladesh | Currency: BDT | Exchange Rate: 1
Line 1: RM-TAPE  | Qty: 100   | Unit Price: 220
Line 2: RM-SLIDE | Qty: 20000 | Unit Price: 1.50
```
- **Submit for approval** (threshold 50,000-er beshi hole pending; ProductionManager/Admin approve korbe).
- ⚠️ **Approval test:** total = 100×220 + 20000×1.5 = 52,000 > 50,000 → **PendingApproval**. Admin → **Approvals → My Inbox** → Approve.

### 6.4 GRN (Goods Receipt)  →  **Purchase → Goods Receipts**
```
Against PO (above) | Warehouse: WH-RM
Receive: RM-TAPE 100, RM-SLIDE 20000 (full)
```
- **Post** koro → **stock barbe** (Inventory-te check) + **RawMaterial WAC update** (PO price theke).

### 6.5 Supplier Invoice  →  **Purchase → Supplier Invoices**
```
Against GRN/PO | Invoice No: YKK-INV-001 | VAT Rate: 15%
```
- VAT auto calc; TotalAmount = subtotal + VAT. **Post** koro.

### 6.6 Payment  →  **Purchase → Payments**
```
Pay Supplier: YKK Bangladesh | Against Invoice: YKK-INV-001
Amount: (partial — half dao first) | Method: Bank Transfer | Bank: BTX Operating A/C
```
- **Verify:** Invoice "Partially Paid" → baki amount-er 2nd payment → "Paid".

---

# PHASE 7 — Production

### 7.1 BOM  →  **Production → BOM**
```
Product: FG-ZIP5 (Metal Zipper #5 — 18cm) | Output Qty: 1 PCS
Components:
  RM-TAPE  : 0.4 ROLL  (per zipper — chhoto value, test-er jonno)
  RM-SLIDE : 1 PCS
  RM-WIRE  : 0.05 KG
```
- (Optional) **Alternative material**: RM-WIRE-er ekta substitute add koro (conversion factor soho).

### 7.2 Production Order  →  **Production → Production Orders**
```
Product: FG-ZIP5 | Qty to produce: 1000 PCS | Warehouse(out): WH-RM | Warehouse(in): WH-FG
```
- **Start** → RM consume (stock kombe), **Complete** → FG stock barbe + Product WAC update.
- **Multi-stage:** stages (Cutting → Assembly → Finishing) Start/Complete/Skip test koro; operator (employee) assign.

### 7.3 Job Cards  →  **Production → Job Cards**
- Job card create + **scanner** (QR) diye stage update test koro (job-cards-module).

### 7.4 Subcontracting  →  **Production → Subcontracting**
```
Send RM to subcontractor → receive finished → reconcile
```

---

# PHASE 8 — Inventory

### 8.1 Stock View  →  **Inventory → Stock**
- GRN + Production-er por current stock dekho (RM + FG, per warehouse, ৳ valuation).

### 8.2 Stock Adjustment  →  **Inventory → Stock Adjustments**
```
Warehouse: WH-RM | Item: RM-THRD | Adjust: +500 PCS | Reason: Opening stock count
Item: RM-WIRE | Adjust: -2 KG | Reason: Damaged
```
- **Post** → stock movement audit-e dekhabe.

### 8.3 Stock Transfer  →  **Inventory → Stock Transfers**
```
From: WH-FG | To: WH-RM | Item: FG-ZIP5 | Qty: 100
```
- **Post** (two-pass atomic) → dui warehouse-e qty adjust.

### 8.4 Lot/Batch  →  **Inventory → Lot/Batch**
- GRN-e lot number dile FIFO consume test koro.

### 8.5 Gate Pass  →  **Inventory → Gate Pass**
```
Type: Returnable | Item: (tool/sample) | Out to: subcontractor | Expected return date
```

---

# PHASE 9 — QC + Quarantine

### 9.1 QC Inspection  →  **Quality → QC Inspection**
```
Source: GRN (RM-TAPE batch) | Inspect: 100 ROLL
Passed: 95 | Rejected: 5 (defect: torn tape)
```
- **Post** → rejected 5 **Quarantine Store (WH-QC)**-te jabe, passed usable thakbe.

### 9.2 Quarantine Disposition  →  **Quality → Quarantine Disposition**
```
Disposition: Release (5 ROLL back to usable)  OR  Scrap (write-off)
```

---

# PHASE 10 — Sales Loop (full)

> Flow: **Quotation → Sales Order → Delivery Note → Customer Invoice → Receipt**

### 10.1 Quotation  →  **Sales → Quotations**
```
Customer: Dhaka Fashions Ltd | Currency: BDT
Line: FG-ZIP5 | Qty: 5000 | Unit Price: 18
```
- Print → logo dekhabe. **Convert to Sales Order**.

### 10.2 Sales Order  →  **Sales → Sales Orders**
```
Customer: Dhaka Fashions Ltd | Currency: BDT | Rate: 1
Line: FG-ZIP5 | Qty: 5000 | Price: 18 | (Buyer: H&M, Style: ST-001 optional)
```
- **Confirm** koro.

### 10.3 Delivery Note  →  **Sales → Delivery Notes**
```
Against SO | Warehouse(out): WH-FG | Deliver: FG-ZIP5 5000
```
- **Post** → FG stock kombe, SO dispatched qty barbe.
- ⚠️ Stock kom thakle age Production (Phase 7) diye FG stock baro.

### 10.4 Customer Invoice  →  **Sales → Customer Invoices**
```
Against SO/DN | VAT Rate: 15%
```
- VAT auto; **Post**. **Export buyer (H&M, USD)** hole **Commercial Invoice + Packing List** print koro (logo + export fields).

### 10.5 Receipt  →  **Sales → Receipts**
```
From: Dhaka Fashions Ltd | Against Invoice | Amount: (partial first) | Method: Bank Transfer
```
- **Verify:** Invoice Partially Paid → full payment → Paid.

---

# PHASE 11 — Returns

### 11.1 Customer Return (CRN)  →  **Sales → Customer Return Notes**
```
Against DN | Return: FG-ZIP5 200 PCS | Reason: Defective
```
- **Post** → FG stock back (ReturnIn) + SO dispatched qty kombe.

### 11.2 Supplier Return (SRN)  →  **Purchase → Supplier Return Notes**
```
Against GRN | Return: RM-TAPE 5 ROLL | Reason: Quality issue
```
- **Post** → RM stock kombe (source-stock check) + PO received qty kombe.

---

# PHASE 12 — Accounting / VAT / Banking / LC

### 12.1 Chart of Accounts  →  **Accounts → Chart of Accounts**
- Seeded accounts dekho; ekta notun account add koro (e.g. "Office Supplies — Expense").

### 12.2 Journal Entry  →  **Accounts → Journal Entries**
```
Debit:  Office Supplies (Expense) 5000
Credit: BTX Operating A/C (Bank)   5000
Narration: Stationery purchase
```
- **Post** (balanced hote hobe: Dr = Cr).

### 12.3 VAT / Mushok  →  **Accounts → VAT** (or Reports → VAT Summary)
- Customer + Supplier invoice-er por **Mushok 6.3 Challan** + **VAT Summary** (Output − Input = Net liability) dekho.

### 12.4 Banking → Bank Reconciliation  →  **Banking → Bank Reconciliation**
```
Upload/enter bank statement lines → match with payments/receipts → reconcile
```

### 12.5 Letter of Credit (LC)  →  **Banking → Letter of Credit**
```
Type: Import LC | Bank: City Bank | Beneficiary: YKK | Amount: 10000 USD | Expiry: +90 days
```

---

# PHASE 13 — Reports + Dashboard

### 13.1 Reports  →  **Reports →** (protita kholo, date range dao)
- Stock Summary (৳ valuation), AR Ageing, AP Ageing, Sales Summary, COGS/Margin, VAT Summary,
  Stock Ledger, Item Ledger, Dead Stock, Customer Statement, Supplier Statement, Buyer Order Book, Wastage/Variance.
- **Verify:** uporer transaction-gula ekhane reflect korche (data meaningful).

### 13.2 Dashboard  →  **Dashboard**
- KPI tiles (cash+bank, this-month revenue, stock value, active orders) + role-wise widgets + "Needs Attention" list.

---

# PHASE 14 — Approvals / Notifications / Compliance

### 14.1 Approvals  →  **Administration → Approvals**
- Phase 6.3-er PO approval-ta ekhane "My Inbox / All" + decision + history dekho.

### 14.2 Notifications  →  **Administration → Notifications**
- Low stock / overdue invoice / expiring cert alerts (Operational Alerts job) dekho.

### 14.3 Compliance  →  **Compliance**
- **Audit Log** viewer (protita document save-er change-history), **Certificates** (expiry), **CAP** (corrective action) test koro.

---

## 🔁 End-to-end smoke test (sob ekসাথে — final)
1. RM kino: **PO → GRN** (stock in)
2. **BOM → Production Order** (RM → FG)
3. FG becho: **SO → DN → Invoice → Receipt**
4. **Reports → Stock Summary + Sales Summary + Margin** dekho — pura chain reflect korche?
5. **Dashboard** KPI gula update hoyeche?
→ Hole **core ERP loop ekdom thik kaj korche** ✅

---

## ⚠️ Test korar somoy mone rakhar bishoy
- **Migration apply kora ase to?** (`dotnet ef database update ...`) — na hole attendance/designation-access feature kaj korbe na.
- **Dropdown khali?** → mane oi master data ekhono banaও nai (Phase 1-e phire jao).
- **"Insufficient stock"?** → age GRN/Production diye stock baro, tarpor DN/transfer.
- **Post button ase?** → Draft save kore tarpor Post — na korle stock/accounting move korbe na.
- **Currency mismatch?** → PO/SO-te customer/supplier-er currency + rate thik ase kina dekho.
- **Permission denied?** → oi role-e permission nei; SuperAdmin diye test koro, OR Roles-e permission dao.

> **Tip:** Protita phase sesh-e checklist-e tick dao. Kono jaigai unexpected error/behaviour pele — **screenshot + kon phase + ki data** likhে rakho, pore ek সাথে fix korbo.

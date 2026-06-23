# Bengal TEX ERP — EXACT Field-Wise Demo Data (copy each value)

> Ei file-e protita form-er **exact field** + **copy korar moto value** deya. Field name UI-er sathe match korbe. **(dropdown)** = master theke select koro, **(toggle)** = on/off, **(date)** = date picker, baki text/number.
> Order maintain koro — niche jeta age, seta age. (Updated to match real UI forms 100%.)

---

## ⓪ COMPANY  →  Master Data → Company Settings
```
Name              : Bengal TEX Accessories Ltd
Short Name        : BTX
Registration No   : C-123456/2020
Tax (BIN) Number  : 001234567-0202
Address Line 1    : Plot 12, Tejgaon Industrial Area
Address Line 2    : Tejgaon
City              : Dhaka
District          : Dhaka
Postal Code       : 1208
Country           : Bangladesh
Phone             : 02-9123456
Email             : info@bengaltex.com
Website           : https://bengaltex.com
```
→ Save → tarpor **Upload logo** (PNG/JPG).

---

# PHASE 1 — MASTER DATA (exact)

> **Order:** Currency → Unit of Measure → Product Category → Warehouse → Department → Designation → Shift → Bank Account → Customer → Supplier → Product → Raw Material.

## 1.1 CURRENCY  →  Master Data → Currencies
**Fields:** code, name, symbol, exchangeRateToBase, isBaseCurrency(toggle), isActive(toggle)
```
# Currency 1
Code                 : BDT
Name                 : Bangladeshi Taka
Symbol               : Tk
Exchange Rate To Base: 1
Is Base Currency     : ON
Active               : ON

# Currency 2
Code                 : USD
Name                 : US Dollar
Symbol               : $
Exchange Rate To Base: 121.50
Is Base Currency     : OFF
Active               : ON

# Currency 3
Code                 : EUR
Name                 : Euro
Symbol               : E
Exchange Rate To Base: 131.00
Is Base Currency     : OFF
Active               : ON
```

## 1.2 UNIT OF MEASURE  →  Master Data → Units of Measure
**Fields:** code, name, symbol, unitType(dropdown: Count/Weight/Length/Volume/Area), baseUnitId(dropdown, optional), conversionFactor, isActive
```
# UoM 1
Code            : PCS
Name            : Pieces
Symbol          : pcs
Unit Type       : Count
Base Unit       : (none)
Conversion      : 1
Active          : ON

# UoM 2
Code            : MTR
Name            : Meter
Symbol          : m
Unit Type       : Length
Base Unit       : (none)
Conversion      : 1

# UoM 3
Code            : KG
Name            : Kilogram
Symbol          : kg
Unit Type       : Weight
Base Unit       : (none)
Conversion      : 1

# UoM 4
Code            : ROLL
Name            : Roll
Symbol          : roll
Unit Type       : Count
Base Unit       : (none)
Conversion      : 1

# UoM 5
Code            : BOX
Name            : Box
Symbol          : box
Unit Type       : Count
Base Unit       : (none)
Conversion      : 1
```

## 1.3 PRODUCT CATEGORY  →  Master Data → Product Categories
> **Eta age koro** — Product form-e Category required.
**Fields:** code, name, description, isActive
```
Code        : CAT-ZIP
Name        : Zippers
Description : Metal & nylon zippers
Active      : ON

Code        : CAT-BTN
Name        : Buttons
Description : Jeans buttons, snaps
Active      : ON

Code        : CAT-LBL
Name        : Labels & Tags
Description : Woven & printed labels
Active      : ON
```

## 1.4 WAREHOUSE  →  Master Data → Warehouses
**Fields:** code, name, warehouseType(dropdown: General/RawMaterial/FinishedGoods/WorkInProgress/Reject), address, factoryId(dropdown, optional), isActive
```
# WH 1
Code            : WH-RM
Name            : Raw Material Store
Warehouse Type  : RawMaterial
Address         : Plot 12, Tejgaon, Dhaka
Factory         : (none / pick if exists)
Active          : ON

# WH 2
Code            : WH-FG
Name            : Finished Goods Store
Warehouse Type  : FinishedGoods
Address         : Plot 12, Tejgaon, Dhaka

# WH 3
Code            : WH-QC
Name            : Quarantine Store
Warehouse Type  : Reject
Address         : Plot 12, Tejgaon, Dhaka
```

## 1.5 DEPARTMENT  →  Master Data → Departments
**Fields:** code, name, parentDepartmentId(dropdown, opt), headEmployeeId(dropdown, opt — pore set), description, isActive
```
Code        : ADMIN
Name        : Administration
Parent      : (none)
Head        : (none)
Description : Head office admin
Active      : ON

Code        : HR
Name        : Human Resources

Code        : PROD
Name        : Production

Code        : ACC
Name        : Accounts

Code        : STORE
Name        : Store / Inventory

Code        : QC
Name        : Quality Control
```

## 1.6 DESIGNATION  →  Master Data → Designations
**Fields:** code, name, gradeLevel(1-10), accessRoleName(dropdown: existing Roles), description, isActive
```
Code        : MD
Name        : Managing Director
Grade Level : 10
Access Role : SuperAdmin
Active      : ON

Code        : GM-PROD
Name        : GM Production
Grade Level : 8
Access Role : ProductionManager

Code        : HRM
Name        : HR Manager
Grade Level : 7
Access Role : HRManager

Code        : ACC
Name        : Accountant
Grade Level : 5
Access Role : AccountsManager

Code        : SUP
Name        : Floor Supervisor
Grade Level : 4
Access Role : (none)

Code        : OPR
Name        : Sewing Operator
Grade Level : 2
Access Role : (none)
```

## 1.7 SHIFT  →  Master Data → Shifts
**Fields:** code, name, startTime, endTime, weekendDayOfWeek(dropdown), secondWeekendDayOfWeek(dropdown, opt), description, isActive
```
Code              : DAY
Name              : Day Shift
Start Time        : 09:00
End Time          : 18:00
Weekend Day       : Friday
Second Weekend    : (none)
Description       : General day shift
Active            : ON

Code              : A
Name              : A Shift
Start Time        : 06:00
End Time          : 14:00
Weekend Day       : Friday
Second Weekend    : (none)
```

## 1.8 BANK ACCOUNT  →  Master Data → Bank Accounts
**Fields:** accountName, bankName, branchName, accountNumber, accountType(dropdown: Current/Savings/FixedDeposit), routingNumber, swiftCode, currency(dropdown), ledgerAccountId(dropdown, opt), notes, isActive
```
Account Name   : BTX Operating A/C
Bank Name      : BRAC Bank
Branch Name    : Gulshan
Account Number : 1501203040506
Account Type   : Current
Routing Number : 060270556
SWIFT Code     : BRAKBDDH
Currency       : BDT
Ledger Account : (none)
Notes          : Main operating account
Active         : ON

Account Name   : BTX USD A/C
Bank Name      : The City Bank
Branch Name    : Motijheel
Account Number : 2201203040507
Account Type   : Current
Routing Number : 225264777
SWIFT Code     : CIBLBDDH
Currency       : USD
```

## 1.9 CUSTOMER  →  Master Data → Customers
**Fields:** code, name, contactPerson, phone, email, website, addressLine1, addressLine2, city, district, postalCode, country, binNumber, vatNumber, tinNumber, category(dropdown: A/B/C), creditLimit, creditPeriodDays(0-365), isExport(toggle), parentCustomerId(dropdown opt), notes, isActive
```
# Customer 1 (local)
Code            : (khali rakho — auto)
Name            : Dhaka Fashions Ltd
Contact Person  : Mr. Anwar Hossain
Phone           : 01711000001
Email           : buy@dhakafashions.com
Website         : https://dhakafashions.com
Address Line 1  : 45 Mirpur Road
Address Line 2  : Section 2
City            : Dhaka
District        : Dhaka
Postal Code     : 1216
Country         : Bangladesh
BIN Number      : 009988776-0101
VAT Number      : 19111009988
TIN Number      : 412233445566
Category        : B
Credit Limit    : 500000
Credit Period   : 30
Is Export       : OFF
Parent Customer : (none)
Notes           : Regular wholesale buyer
Active          : ON

# Customer 2 (export)
Code            : (auto)
Name            : H&M Bangladesh
Contact Person  : Ms. Lena Karlsson
Phone           : 01711000002
Email           : po@hm.com
Website         : https://hm.com
Address Line 1  : Gulshan Avenue
Address Line 2  : Gulshan-2
City            : Dhaka
District        : Dhaka
Postal Code     : 1212
Country         : Bangladesh
BIN Number      : 007766554-0202
VAT Number      : 19111007766
TIN Number      : 559988774422
Category        : A
Credit Limit    : 2000000
Credit Period   : 60
Is Export       : ON
Notes           : Export buyer (USD)
```

## 1.10 SUPPLIER  →  Master Data → Suppliers
**Fields:** code, name, contactPerson, phone, email, website, addressLine1, addressLine2, city, district, postalCode, country, binNumber, vatNumber, tinNumber, paymentTermsDays(0-365), bankName, bankAccountNumber, bankBranch, bankAccountHolderName, rating(0-5), notes, isActive
```
# Supplier 1
Code                  : (auto)
Name                  : YKK Bangladesh
Contact Person        : Mr. Tanaka
Phone                 : 01712000001
Email                 : sales@ykk.com
Website               : https://ykk.com
Address Line 1        : Plot 5, DEPZ
Address Line 2        : Savar
City                  : Savar
District              : Dhaka
Postal Code           : 1340
Country               : Bangladesh
BIN Number            : 005544332-0303
VAT Number            : 19111005544
TIN Number            : 778899001122
Payment Terms (days)  : 30
Bank Name             : Standard Chartered
Bank Account Number   : 0102030405060
Bank Branch           : Gulshan
Bank Account Holder   : YKK Bangladesh Ltd
Rating                : 5
Notes                 : Zipper supplier
Active                : ON

# Supplier 2
Code                  : (auto)
Name                  : Coats Bangladesh Ltd
Contact Person        : Mr. Rashed Khan
Phone                 : 01712000002
Email                 : order@coats.com
Address Line 1        : CEPZ
City                  : Chittagong
District              : Chittagong
Postal Code           : 4223
Country               : Bangladesh
Payment Terms (days)  : 45
Bank Name             : HSBC
Bank Account Number   : 0908070605040
Bank Branch           : Agrabad
Bank Account Holder   : Coats Bangladesh Ltd
Rating                : 4
Notes                 : Thread supplier
```

## 1.11 PRODUCT (Finished Good)  →  Master Data → Products
**Fields:** code, name, specification, productCategoryId(dropdown), unitOfMeasureId(dropdown), size, color, material, hsCode, salesPrice, reorderLevel, isStockItem(toggle), imageUrl, notes, isActive
```
# Product 1
Code            : (auto)
Name            : Metal Zipper #5 - 18cm
Specification   : Brass metal zipper, auto-lock slider, 18cm closed-end
Product Category: Zippers
Unit of Measure : PCS
Size            : 18cm
Color           : Antique Brass
Material        : Brass
HS Code         : 9607.11.00
Sales Price     : 18
Reorder Level   : 2000
Is Stock Item   : ON
Image URL       : (khali)
Notes           : Best-seller zipper
Active          : ON

# Product 2
Code            : (auto)
Name            : Jeans Button 17mm
Specification   : Tack button, antique finish
Product Category: Buttons
Unit of Measure : PCS
Size            : 17mm
Color           : Antique Silver
Material        : Brass
HS Code         : 9606.22.00
Sales Price     : 5
Reorder Level   : 5000

# Product 3
Code            : (auto)
Name            : Woven Label (H&M)
Specification   : Damask woven main label
Product Category: Labels & Tags
Unit of Measure : PCS
Size            : 5x2 cm
Color           : Black/White
Material        : Polyester
HS Code         : 5807.10.00
Sales Price     : 2
Reorder Level   : 10000
```

## 1.12 RAW MATERIAL  →  Master Data → Raw Materials
**Fields:** code, name, specification, category(text, required), unitOfMeasureId(dropdown), minimumStockLevel, openingStock, standardCost, preferredSupplierId(dropdown opt), notes, isActive
```
# RM 1
Code              : (auto)
Name              : Zipper Tape Roll
Specification     : Polyester woven tape, 30mm
Category          : Tape
Unit of Measure   : ROLL
Minimum Stock     : 50
Opening Stock     : 0
Standard Cost     : 220
Preferred Supplier: YKK Bangladesh
Notes             : For zipper production
Active            : ON

# RM 2
Code              : (auto)
Name              : Zipper Slider #5
Specification     : Auto-lock brass slider
Category          : Slider
Unit of Measure   : PCS
Minimum Stock     : 5000
Opening Stock     : 0
Standard Cost     : 1.50
Preferred Supplier: YKK Bangladesh

# RM 3
Code              : (auto)
Name              : Brass Wire Spool
Specification     : 1.2mm brass wire
Category          : Wire
Unit of Measure   : KG
Minimum Stock     : 100
Opening Stock     : 0
Standard Cost     : 850
Preferred Supplier: YKK Bangladesh

# RM 4
Code              : (auto)
Name              : Polyester Thread Cone
Specification     : 5000m cone, tex 40
Category          : Thread
Unit of Measure   : PCS
Minimum Stock     : 200
Opening Stock     : 0
Standard Cost     : 95
Preferred Supplier: Coats Bangladesh Ltd
```

---

# PHASE 2 — USERS & ROLES (exact)

## 2.1 ROLE  →  Administration → Roles
**Fields:** name, description, + permission checkboxes
```
Name        : Store Keeper
Description : Inventory + GRN access only
Permissions : (tick) Inventory.View, Inventory.Adjust, Products.View, RawMaterials.View, GRN.View, GRN.Create
```

## 2.2 USER  →  Administration → Users
**Fields:** userName, email, fullName, password, confirmPassword, factoryId(dropdown opt), roles(multi-select)
```
# User 1
Username         : hr.manager
Email            : hr@bengaltex.com
Full Name        : HR Manager
Password         : Hr@123456
Confirm Password : Hr@123456
Factory          : (none)
Roles            : HRManager

# User 2
Username         : accountant
Email            : acc@bengaltex.com
Full Name        : Accountant
Password         : Acc@123456
Confirm Password : Acc@123456
Roles            : AccountsManager

# User 3
Username         : store.keeper
Email            : store@bengaltex.com
Full Name        : Store Keeper
Password         : Store@123456
Confirm Password : Store@123456
Roles            : Store Keeper
```

---

# PHASE 3 — EMPLOYEE (exact)

## 3.1 EMPLOYEE  →  HR & Payroll → Employees → Add Employee
**Fields:** code, fullName, designation(text), department(text), phone, email, nationalId, address, joiningDate(date), dateOfBirth(date), gender(dropdown: Male/Female/Other), employmentType(dropdown: Permanent/Contract/DailyWage), basicSalary, houseRentAllowance, medicalAllowance, transportAllowance, foodAllowance, isPfMember(toggle), pfRate, isTaxable(toggle), departmentId(dropdown), designationId(dropdown), shiftId(dropdown), bankAccountId(dropdown), reportingToEmployeeId(dropdown), [edit: status, isActive], notes
```
# Employee 2 — AGE BANAO (karon Employee 1 ke ER under report korabe)
Code               : (khali — auto)
Full Name          : Rahim Uddin
Designation (text) : Floor Supervisor
Department (text)  : Production
Phone              : 01811000002
Email              : rahim@bengaltex.com
National ID        : 1990123456789
Address            : Mohakhali, Dhaka
Joining Date       : 2022-05-01
Date of Birth      : 1988-07-20
Gender             : Male
Employment Type    : Permanent
Basic Salary       : 28000
House Rent         : 12000
Medical Allowance  : 2000
Transport Allowance: 2000
Food Allowance     : 1500
PF Member          : ON
PF Rate            : 10
Taxable            : OFF
Department         : Production   (dropdown)
Designation        : Floor Supervisor   (dropdown)
Shift              : Day Shift   (dropdown)
Bank Account       : BTX Operating A/C   (dropdown)
Reporting To       : (none)
Notes              : Production floor supervisor

# Employee 1 — Rahim banano por
Code               : (auto)
Full Name          : Karim Ahmed
Designation (text) : Sewing Operator
Department (text)  : Production
Phone              : 01811000001
Email              : karim@bengaltex.com
National ID        : 1995031298765
Address            : Tejgaon, Dhaka
Joining Date       : 2024-01-10
Date of Birth      : 1995-03-12
Gender             : Male
Employment Type    : Permanent
Basic Salary       : 14000
House Rent         : 6000
Medical Allowance  : 1000
Transport Allowance: 1000
Food Allowance     : 1000
PF Member          : ON
PF Rate            : 10
Taxable            : OFF
Department         : Production   (dropdown)
Designation        : Sewing Operator   (dropdown)
Shift              : Day Shift   (dropdown)
Bank Account       : BTX Operating A/C   (dropdown)
Reporting To       : Rahim Uddin   (dropdown)  ← supervisor chain
Notes              : Sewing line operator
```

## 3.2 MANAGE LOGIN  →  Employees list → 🔑 icon → Manage Login
```
Karim Ahmed →  Username: karim | Email: karim@bengaltex.com | Password: Karim@123456
Rahim Uddin →  Username: rahim | Email: rahim@bengaltex.com | Password: Rahim@123456
```

---

> **NOTE:** Ei file = Phase 0-3 (Company + Master Data + Users + Employee) 100% exact field-match.
> **Phase 4+ (Attendance settings, PO, GRN, BOM, Production, SO, DN, Invoice, Receipt etc.) transaction form-gula header + line-grid — oigular exact field-wise data ami next-e dibo (bolo, ektu boro pass)।** Main testing guide `UAT-TESTING-GUIDE.md`-e oigular flow + data ache.

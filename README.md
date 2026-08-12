# 🏢 Bengal TEX ERP

<p align="center"> <img src="./assets/finaltex.png" alt="Bengal TEX ERP - Garments Accessories ERP System" width="100%"> </p>

<p align="center"> <strong>⚙️ Enterprise Resource Planning platform for garments accessories manufacturing and trading operations.</strong> </p>

<p align="center"> <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 8"> <img src="https://img.shields.io/badge/Angular-21-DD0031?logo=angular&logoColor=white" alt="Angular 21"> <img src="https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoftsqlserver&logoColor=white" alt="SQL Server 2022"> <img src="https://img.shields.io/badge/Entity%20Framework%20Core-8-512BD4?logo=dotnet&logoColor=white" alt="EF Core"> <img src="https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white" alt="Docker"> </p>

---

## 📖 Overview

Bengal TEX ERP is a full-stack enterprise ERP application designed around the operational needs of a garments accessories manufacturing and trading business.

The system brings core business operations into a unified platform, covering HR & Payroll, Procurement, Inventory, Production, Sales, Accounting, Banking, Quality Control, Compliance, Reporting, Approvals, and Administration.

The solution is implemented as a layered ASP.NET Core 8 Web API + Angular 21 + SQL Server application, with domain-focused modules, authentication and authorization, background processing, real-time notifications, audit logging, reporting, document generation, and Docker-based deployment.

🎯 **Project focus:** connect people, products, materials, transactions, production, finance, and approvals through consistent business workflows and traceable data.

---

## 🗂️ Business Scope

### 👥 HR & Payroll
- ✅ Employee management and employee profiles
- ✅ Departments, designations, shifts, holidays and leave types
- ✅ Attendance and attendance correction workflow
- ✅ Multiple office locations and attendance settings
- ✅ Employee login management
- ✅ Payroll, payslips, loans, bonuses and final settlement
- ✅ Role- and designation-driven access

### 🛒 Procurement
- ✅ Purchase requisitions
- ✅ Supplier quotations / RFQ
- ✅ Purchase orders
- ✅ Goods receipt notes
- ✅ Supplier invoices
- ✅ Supplier returns
- ✅ Landed cost
- ✅ Purchase payments

### 🏭 Production
- ✅ Bill of Materials (BOM)
- ✅ Material Requirements Planning (MRP)
- ✅ Production orders
- ✅ Job cards
- ✅ Work centers
- ✅ Machine and maintenance management
- ✅ Subcontracting
- ✅ Wastage and scrap sales
- ✅ Production costing

### 📦 Inventory
- ✅ Products and product categories
- ✅ Product variants
- ✅ Raw materials
- ✅ Warehouses
- ✅ Stock on hand
- ✅ Stock movements
- ✅ Stock lots / batches
- ✅ Stock adjustments
- ✅ Stock transfers
- ✅ Opening stock
- ✅ Gate passes
- ✅ Raw-material substitutes

### 💼 Sales
- ✅ Quotations
- ✅ Proforma invoices
- ✅ Sales orders
- ✅ Delivery notes
- ✅ Customer invoices
- ✅ Customer pricing
- ✅ Customer returns
- ✅ Receipts
- ✅ Credit and debit notes

### 💰 Accounting & Finance
- ✅ Chart of accounts
- ✅ Journal entries
- ✅ General ledger / inventory GL
- ✅ Budgets
- ✅ Cost centers
- ✅ Costing rates
- ✅ Financial years
- ✅ Exchange rates
- ✅ Financial intelligence
- ✅ Payments and receipts
- ✅ Expenses
- ✅ Fixed assets
- ✅ Bank accounts and bank facilities
- ✅ Bank reconciliation
- ✅ VAT / statutory workflows
- ✅ Letter of Credit (LC)
- ✅ Financial reporting

### 🛡️ Quality, Compliance & Governance
- ✅ QC inspections
- ✅ Quarantine disposition
- ✅ Compliance records
- ✅ Approval workflows
- ✅ Audit logs
- ✅ Role and permission management
- ✅ Notifications
- ✅ Attachments and document handling

---

## ⭐ Key Capabilities

| Capability | Implementation |
|---|---|
| 🔐 Authentication | ASP.NET Core Identity + JWT Bearer |
| 🛂 Authorization | Role and permission based access control |
| 🏗️ Architecture | Layered / Clean Architecture style |
| 🧩 Application layer | Domain-focused feature modules |
| 💾 Data access | Entity Framework Core |
| 🗄️ Database | Microsoft SQL Server |
| ✔️ Validation | FluentValidation |
| 🔀 Mediator pattern | MediatR |
| 📡 Real-time communication | ASP.NET Core SignalR |
| ⏱️ Background jobs | Hangfire |
| 📝 Logging | Serilog |
| 📘 API documentation | Swagger / OpenAPI |
| 📊 Dashboard & charts | Angular + PrimeNG + Chart.js |
| 📷 QR / scanning | html5-qrcode / QR capabilities |
| 🐳 Deployment | Docker Compose |
| 🌐 Reverse proxy / web serving | Angular build served through nginx |
| 🧪 Testing | Dedicated API test project |

---

## 🏛️ Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    ANGULAR 21 FRONTEND                       │
│  PrimeNG • TypeScript • RxJS • Chart.js • SignalR • QR      │
└──────────────────────────────┬───────────────────────────────┘
                               │ HTTPS / REST / SignalR
                               ▼
┌──────────────────────────────────────────────────────────────┐
│                 ASP.NET CORE 8 WEB API                       │
│  Controllers • Authentication • Authorization • Middleware  │
└──────────────────────────────┬───────────────────────────────┘
                               ▼
┌──────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                         │
│ Commands • Queries • DTOs • Validation • Business Workflows │
│ MediatR • Feature-oriented modules                           │
└──────────────────────────────┬───────────────────────────────┘
                               ▼
┌──────────────────────────────────────────────────────────────┐
│                      DOMAIN LAYER                            │
│ Entities • Value Objects • Repository Contracts • Rules     │
└──────────────────────────────┬───────────────────────────────┘
                               ▼
┌──────────────────────────────────────────────────────────────┐
│                  INFRASTRUCTURE LAYER                        │
│ EF Core • SQL Server • Identity • Persistence • Services     │
└──────────────────────────────┬───────────────────────────────┘
                               ▼
                    ┌────────────────────┐
                    │   SQL Server 2022  │
                    └────────────────────┘
```

---

## 🧩 Application Modules

The backend is organized into dedicated business modules, including:

🔸 Accounting · 🔸 Approvals · 🔸 Attendance · 🔸 AuditLog · 🔸 Authentication · 🔸 Banking · 🔸 Bank Reconciliation · 🔸 BOM · 🔸 Company · 🔸 Compliance · 🔸 Customers · 🔸 Customer Invoices · 🔸 Customer Pricing · 🔸 Customer Returns · 🔸 Dashboard · 🔸 Delivery Notes · 🔸 Employees · 🔸 Expenses · 🔸 Factory · 🔸 Fixed Assets · 🔸 Gate Passes · 🔸 Goods Receipts · 🔸 Inventory · 🔸 Job Cards · 🔸 LC · 🔸 Leaves · 🔸 Machine Maintenance · 🔸 MRP · 🔸 Notifications · 🔸 Opening Stock · 🔸 Payroll · 🔸 Permissions · 🔸 Products · 🔸 Production · 🔸 Proforma Invoices · 🔸 Purchase Orders · 🔸 Purchase Requisitions · 🔸 QC Inspection · 🔸 Quarantine · 🔸 Quotations · 🔸 Raw Materials · 🔸 Receipts · 🔸 Reports · 🔸 Roles · 🔸 Sales Orders · 🔸 Samples · 🔸 Stock Lots · 🔸 Stock Transfers · 🔸 Subcontracting · 🔸 Suppliers · 🔸 Supplier Invoices · 🔸 Supplier Quotations · 🔸 Supplier Returns · 🔸 VAT · 🔸 Warehouses · 🔸 Wastage · 🔸 Work Centers

📌 The exact implementation is organized under the application's **Domain**, **Application**, **Infrastructure**, **Shared**, and **API** projects.

---

## 🔄 Core Business Workflows

### 🛒 Procurement to Payment
```
Purchase Requisition
        ↓
Supplier Quotation / RFQ
        ↓
Purchase Order
        ↓
Goods Receipt
        ↓
Supplier Invoice
        ↓
Payment
```

### 💼 Sales to Cash
```
Quotation
    ↓
Proforma Invoice (when applicable)
    ↓
Sales Order
    ↓
Delivery Note
    ↓
Customer Invoice
    ↓
Receipt
```

### 🏭 Production
```
Sales / Production Requirement
            ↓
          BOM
            ↓
          MRP
            ↓
   Production Order
            ↓
        Job Card
            ↓
       Production
            ↓
     QC / Inspection
            ↓
      Finished Goods
```

### 📦 Inventory
```
Opening Stock
     │
     ├── Purchase → Goods Receipt
     │
     ├── Production → Finished Goods
     │
     ├── Stock Transfer
     │
     └── Stock Adjustment
              ↓
        Stock Movements
              ↓
        Stock On Hand
```

### 👥 HR & Attendance
```
Employee
   ↓
Designation / Role
   ↓
Office Location / Shift
   ↓
Attendance
   ↓
Correction / Supervisor Review
   ↓
Payroll
```

---

## 🔐 Security & Access Control

The system includes application-level security mechanisms for enterprise use:

- 🔑 JWT-based authentication
- 🪪 ASP.NET Core Identity integration
- 👤 Role management
- 🛂 Permission management
- 🔒 Protected API endpoints
- 🧭 Designation-driven access concepts
- 📋 Audit logging
- 🖐️ Device/fingerprint-related authentication support
- 🌍 Configurable CORS
- 🗝️ Environment-based secrets
- 🔐 Password hashing through the identity stack

⚠️ **Sensitive values** such as database passwords, JWT secrets, fingerprint salts, SMTP credentials, and seed credentials should be supplied through environment configuration and must not be committed to source control.

---

## ⚡ Real-Time & Background Processing

### 📡 SignalR
SignalR is used for real-time application communication, including notification-oriented scenarios.

```
ERP Event
   ↓
ASP.NET Core Hub
   ↓
Connected Angular Clients
   ↓
Real-time UI Update
```

### ⏱️ Hangfire
Background processing is integrated with SQL Server storage for scheduled/background jobs such as operational maintenance tasks.

---

## 📊 Reporting & Documents

The system contains reporting and document-oriented capabilities across business areas, including:

- 📈 Operational reports
- 📦 Inventory reports
- 💼 Sales reports
- 🛒 Purchase reports
- 👥 HR / attendance reports
- 💰 Financial reports
- 🧾 VAT/statutory reporting
- 📄 Document generation
- 📎 Attachments
- 📧 Email audit/logging

---

## 🛠️ Technology Stack

### ⚙️ Backend
- C#
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- ASP.NET Core Identity
- JWT Bearer Authentication
- MediatR
- FluentValidation
- Hangfire
- Serilog
- Swagger / OpenAPI
- SignalR

### 🎨 Frontend
- Angular 21
- TypeScript
- RxJS
- PrimeNG
- PrimeIcons
- Chart.js
- HTML5 QR Code
- FingerprintJS

### 🗄️ Database
- Microsoft SQL Server 2022
- Entity Framework Core
- Hangfire SQL Server storage

### 🚀 DevOps
- Docker
- Docker Compose
- nginx
- Environment-based configuration
- Persistent Docker volumes
- Database backup support

---

## 📁 Project Structure

```
BengalTex.ERP/
│
├── client/                         # Angular 21 application
│
├── src/
│   ├── BengalTex.ERP.Domain/      # Domain entities & rules
│   ├── BengalTex.ERP.Application/ # Business features, commands, queries, DTOs
│   ├── BengalTex.ERP.Infrastructure/
│   │                                 # EF Core, persistence & infrastructure
│   ├── BengalTex.ERP.Shared/      # Shared contracts/utilities
│   └── BengalTex.ERP.Api/         # REST API, auth, middleware, hubs
│
├── tests/
│   └── BengalTex.ERP.Api.Tests/   # API tests
│
├── docs/
│   ├── UAT-TESTING-GUIDE.md
│   └── UAT-DATA-EXACT-FIELDS.md
│
├── docker-compose.yml
├── DEPLOYMENT.md
├── .env.example
└── BengalTex.ERP.sln
```

---

## 🚀 Getting Started

### ✅ Prerequisites

Install:

- 🧰 .NET 8 SDK
- 📦 Node.js / npm
- 🅰️ Angular CLI 21
- 🗄️ SQL Server 2022, or Docker Desktop
- 🔧 Git

💡 For the containerized setup, Docker and Docker Compose are recommended.

### 🐳 Option 1 — Run with Docker Compose

**1️⃣ Clone**
```bash
git clone https://github.com/YOUR-USERNAME/BengalTex-ERP.git
cd BengalTex-ERP
```

**2️⃣ Create environment file**
```bash
cp .env.example .env
```
On Windows PowerShell:
```powershell
Copy-Item .env.example .env
```
Update `.env` with your own secrets and environment values.

**3️⃣ Start the full stack**
```bash
docker compose up -d --build
```
The compose configuration provisions:
```
SQL Server
   +
ASP.NET Core API
   +
Angular / nginx Web App
```

**4️⃣ Check running containers**
```bash
docker compose ps
```

**5️⃣ View logs**
```bash
docker compose logs -f api
```
or:
```bash
docker compose logs -f web
```

**6️⃣ Stop**
```bash
docker compose down
```

📌 For production deployment, follow the repository's `DEPLOYMENT.md` and environment configuration requirements rather than using development/default secrets.

### 🖥️ Option 2 — Run Backend & Frontend Separately

**⚙️ Backend**

From the repository root:
```bash
dotnet restore
dotnet build
dotnet run --project src/BengalTex.ERP.Api
```
- 🔌 The API configuration and launch settings determine the local HTTP/HTTPS ports.
- 📘 Swagger/OpenAPI is available when enabled by the application environment.

**🎨 Frontend**
```bash
cd client
npm install
npm start
```
Angular development server:
```
http://localhost:4200
```
Build for production:
```bash
npm run build
```

---

## 🧪 Testing

The repository contains a dedicated API test project.

Run the test suite with:
```bash
dotnet test
```

Angular unit tests can be executed with:
```bash
cd client
npm test
```

📌 For end-to-end testing, use the project's documented testing setup and UAT guide.

---

## 📋 UAT & Demo Data

The repository includes dedicated documentation for functional testing:

- 📄 `docs/UAT-TESTING-GUIDE.md`
- 📄 `docs/UAT-DATA-EXACT-FIELDS.md`

The UAT guide covers the business flow from master data and user setup through HR, attendance, procurement, production, inventory, sales, returns, accounting, VAT, banking, and LC-related workflows.

✨ This makes the repository useful not only as source code, but also as a structured demonstration and testing environment.

---

## 💾 Database & Backup

The Docker Compose environment uses SQL Server 2022 and persistent Docker volumes.

The deployment configuration also supports:

- 🗄️ Database initialization through EF Core migrations
- 💽 Persistent SQL Server data
- 🗃️ Database backup storage
- 📤 API upload persistence
- 📝 API log persistence
- 🧹 Backup retention/cleanup configuration

### ✅ Before production use:

- 🔐 Set strong secrets.
- 🔑 Change any initial/default administrative credentials.
- 🌐 Configure the correct allowed origins/hosts.
- 📧 Configure SMTP if email delivery is required.
- 💾 Test database backup and restore.
- 🔒 Enable TLS/HTTPS when exposed beyond a trusted internal network.
- 📖 Review the full deployment checklist in `DEPLOYMENT.md`.

---

## ⚙️ Configuration

Copy the example environment file:
```bash
cp .env.example .env
```

📌 Important configuration areas include:

- 🗄️ Database connection
- 🔑 JWT secret
- 🖐️ Device fingerprint salt
- 🌐 CORS allowed origin
- 🌍 Allowed hosts
- 👤 Initial administrator credentials
- 📧 Email / SMTP
- 💾 Database backup settings

⚠️ Never commit real secrets, passwords, tokens, or production connection strings.

---

## 🖼️ Screenshots

Add your actual application screenshots here:

```
assets/
└── screenshots/
    ├── dashboard.png
    ├── hr-payroll.png
    ├── procurement.png
    ├── inventory.png
    ├── production.png
    ├── sales.png
    ├── accounting.png
    └── reports.png
```

Example Markdown:
```markdown
![ERP Dashboard](./assets/screenshots/dashboard.png)
```

📌 Use real screenshots from the running application rather than placeholder images.

---

## 💡 Why Bengal TEX ERP?

Bengal TEX ERP is designed around connected business processes rather than isolated CRUD screens.

The platform links:

**👥 People → 🛒 Procurement → 📦 Inventory → 🏭 Production → 🛡️ Quality → 💼 Sales → 💰 Accounting**

✨ This enables business transactions to move through controlled workflows while keeping operational and financial information connected.

---

## 🏆 Engineering Highlights

- 🧩 Feature-oriented application modules
- 🏗️ Layered architecture separating Domain, Application, Infrastructure and API concerns
- 🔗 RESTful API design
- 💾 EF Core persistence
- 🔐 JWT and role/permission based security
- 🔀 CQRS-style command/query organization through MediatR
- ✔️ Centralized validation with FluentValidation
- 📡 Real-time communication with SignalR
- ⏱️ Background processing with Hangfire
- 📝 Structured logging with Serilog
- 📘 Swagger/OpenAPI documentation
- 🧪 Dedicated API test project
- 🐳 Dockerized full-stack deployment
- 💽 Persistent database, uploads, logs and backup volumes
- 📋 UAT documentation and exact demo-data guidance

---

## 📌 Project Status

🟢 **Active** enterprise application / development project.

Features and workflows may continue to evolve as business requirements change.

---

## ⚠️ Disclaimer

This repository is intended to demonstrate the architecture, engineering practices, business workflows, and technical capabilities of the Bengal TEX ERP platform.

Production deployment should use organization-specific configuration, credentials, security policies, backup procedures, and infrastructure.

---

## ✍️ Author

**MD. Enamul Haque**
🎓 ASP.NET Core Full-Stack Developer | Software Engineer | Statistics Graduate

**🧠 Core technologies:**

`C#` `ASP.NET Core` `Angular` `React` `SQL Server` `Entity Framework Core` `REST API` `JavaScript` `TypeScript` `Node.js` `MongoDB` `.NET MAUI`

---

<p align="center"> <strong>🏢 Bengal TEX ERP — Connecting Operations, People & Finance in One Platform. 🚀</strong> </p>

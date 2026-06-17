/**
 * Centralized ERP navigation menu — single source of truth for the sidebar.
 *
 * Each item carries: title, icon (PrimeIcons), route (leaf only), permission (the SAME
 * "Module.Action" string the backend [HasPermission] + JWT use), and optional children.
 * `permission` undefined → always visible. A group (has children) is shown by the sidebar
 * only when at least one child is visible (role-based visibility — Phase 2).
 *
 * Adding a menu item = add an entry here. No component changes needed.
 */
export interface NavItem {
  title: string;
  icon?: string;            // e.g. 'pi pi-users'
  route?: string;           // absolute route for leaf items
  permission?: string;      // existing permission string; undefined = always visible
  children?: NavItem[];
}

export const NAV_MENU: NavItem[] = [
  {
    title: 'Dashboard',
    icon: 'pi pi-th-large',
    route: '/'
    // no permission → always visible after login
  },

  {
    title: 'Sales',
    icon: 'pi pi-shopping-cart',
    children: [
      { title: 'Quotations',        icon: 'pi pi-file-edit',   route: '/quotations',        permission: 'Quotations.View' },
      { title: 'Samples',           icon: 'pi pi-box',         route: '/samples',           permission: 'Samples.View' },
      { title: 'Sales Orders',      icon: 'pi pi-shopping-bag',route: '/sales-orders',      permission: 'SalesOrders.View' },
      { title: 'Proforma Invoices', icon: 'pi pi-file',        route: '/proforma-invoices', permission: 'ProformaInvoices.View' },
      { title: 'Delivery Notes',    icon: 'pi pi-truck',       route: '/delivery-notes',    permission: 'DeliveryNotes.View' },
      { title: 'Customer Invoices', icon: 'pi pi-receipt',     route: '/customer-invoices', permission: 'Invoices.View' },
      { title: 'Receipts',          icon: 'pi pi-wallet',      route: '/receipts',          permission: 'Payments.View' },
      { title: 'Credit Notes',      icon: 'pi pi-minus-circle',route: '/credit-notes',      permission: 'CreditNotes.View' },
      { title: 'Customer Returns',  icon: 'pi pi-replay',      route: '/customer-returns',  permission: 'Returns.View' }
    ]
  },

  {
    title: 'Purchase',
    icon: 'pi pi-shopping-bag',
    children: [
      { title: 'Purchase Requisitions', icon: 'pi pi-list',        route: '/purchase-requisitions', permission: 'PurchaseRequisitions.View' },
      { title: 'Purchase Orders',       icon: 'pi pi-file-edit',   route: '/purchase-orders',       permission: 'PurchaseOrders.View' },
      { title: 'Goods Receipts (GRN)',  icon: 'pi pi-inbox',       route: '/goods-receipts',        permission: 'GoodsReceipts.View' },
      { title: 'Supplier Invoices',     icon: 'pi pi-receipt',     route: '/supplier-invoices',     permission: 'Invoices.View' },
      { title: 'Payments',              icon: 'pi pi-credit-card', route: '/payments',              permission: 'Payments.View' },
      { title: 'Debit Notes',           icon: 'pi pi-plus-circle', route: '/debit-notes',           permission: 'DebitNotes.View' },
      { title: 'Supplier Returns',      icon: 'pi pi-replay',      route: '/supplier-returns',      permission: 'Returns.View' },
      { title: 'Gate Passes',           icon: 'pi pi-sign-out',    route: '/gate-passes',           permission: 'GatePasses.View' }
    ]
  },

  {
    title: 'Inventory',
    icon: 'pi pi-box',
    children: [
      { title: 'Stock on Hand',    icon: 'pi pi-database',     route: '/stock-on-hand',     permission: 'Inventory.View' },
      { title: 'Stock Movements',  icon: 'pi pi-arrows-h',     route: '/stock-movements',   permission: 'Inventory.View' },
      { title: 'Stock Lots',       icon: 'pi pi-tags',         route: '/stock-lots',        permission: 'Inventory.View' },
      { title: 'Stock Adjustments',icon: 'pi pi-sliders-h',    route: '/stock-adjustments', permission: 'Inventory.View' },
      { title: 'Stock Transfers',  icon: 'pi pi-arrow-right-arrow-left', route: '/stock-transfers', permission: 'Inventory.View' }
    ]
  },

  {
    title: 'Production',
    icon: 'pi pi-cog',
    children: [
      { title: 'Production Orders', icon: 'pi pi-cog',       route: '/production-orders',  permission: 'Production.View' },
      { title: 'Job Cards',         icon: 'pi pi-id-card',   route: '/job-cards',          permission: 'JobCards.View' },
      { title: 'QR Scanner',        icon: 'pi pi-qrcode',    route: '/job-cards/scan',     permission: 'JobCards.Scan' },
      { title: 'Machines',          icon: 'pi pi-server',    route: '/job-cards/machines', permission: 'Machines.View' },
      { title: 'Subcontracting',    icon: 'pi pi-send',      route: '/subcontract-orders', permission: 'Subcontracting.View' },
      { title: 'Wastage',           icon: 'pi pi-trash',     route: '/wastage',            permission: 'Wastage.View' },
      { title: 'Wastage Reasons',   icon: 'pi pi-list',      route: '/wastage/reasons',    permission: 'Wastage.ManageReasons' },
      { title: 'Wastage Summary',   icon: 'pi pi-chart-pie', route: '/wastage/summary',    permission: 'Wastage.View' }
    ]
  },

  {
    title: 'Quality',
    icon: 'pi pi-verified',
    children: [
      { title: 'QC Inspections',          icon: 'pi pi-check-square', route: '/qc-inspections',          permission: 'Qc.View' },
      { title: 'Quarantine Dispositions', icon: 'pi pi-ban',          route: '/quarantine-dispositions', permission: 'Qc.View' }
    ]
  },

  {
    title: 'Accounts',
    icon: 'pi pi-dollar',
    children: [
      { title: 'Chart of Accounts', icon: 'pi pi-sitemap',  route: '/accounting/accounts',       permission: 'Accounting.View' },
      { title: 'Journal Entries',   icon: 'pi pi-book',      route: '/accounting/journals',       permission: 'Accounting.View' },
      { title: 'Cash Book',         icon: 'pi pi-money-bill',route: '/accounting/cash-book',      permission: 'Accounting.View' },
      { title: 'Bank Book',         icon: 'pi pi-building',  route: '/accounting/bank-book',      permission: 'Accounting.View' },
      { title: 'Day Book',          icon: 'pi pi-calendar',  route: '/accounting/day-book',       permission: 'Accounting.View' },
      { title: 'General Ledger',    icon: 'pi pi-list',      route: '/accounting/general-ledger', permission: 'Accounting.View' },
      { title: 'Trial Balance',     icon: 'pi pi-table',     route: '/accounting/trial-balance',  permission: 'Accounting.View' },
      { title: 'Profit & Loss',     icon: 'pi pi-chart-line',route: '/accounting/profit-loss',    permission: 'Accounting.View' },
      { title: 'Balance Sheet',     icon: 'pi pi-chart-bar', route: '/accounting/balance-sheet',  permission: 'Accounting.View' },
      { title: 'Cash Flow',         icon: 'pi pi-arrows-v',  route: '/accounting/cash-flow',      permission: 'Accounting.View' },
      { title: 'Expenses',          icon: 'pi pi-wallet',    route: '/expenses',                  permission: 'Expenses.View' },
      { title: 'Expense Categories',icon: 'pi pi-tags',      route: '/expenses/categories',       permission: 'Expenses.ManageCategories' },
      { title: 'Expense Summary',   icon: 'pi pi-chart-pie', route: '/expenses/summary',          permission: 'Expenses.View' },
      { title: 'VAT Challans',      icon: 'pi pi-percentage',route: '/vat-challans',              permission: 'Invoices.View' },
      { title: 'Fixed Assets',      icon: 'pi pi-desktop',   route: '/fixed-assets',              permission: 'FixedAssets.View' },
      { title: 'Depreciation Runs', icon: 'pi pi-calendar-minus', route: '/fixed-assets/depreciation-runs', permission: 'FixedAssets.View' }
    ]
  },

  {
    title: 'Banking',
    icon: 'pi pi-building-columns',
    children: [
      { title: 'Letters of Credit',  icon: 'pi pi-file',      route: '/letters-of-credit',    permission: 'Banking.View' },
      { title: 'Bank Reconciliation',icon: 'pi pi-check-circle', route: '/bank-reconciliation', permission: 'BankReconciliation.View' }
    ]
  },

  {
    title: 'HR & Payroll',
    icon: 'pi pi-users',
    children: [
      { title: 'Employees',         icon: 'pi pi-id-card',     route: '/employees',         permission: 'Employees.View' },
      { title: 'Attendance',        icon: 'pi pi-clock',       route: '/attendance',        permission: 'Attendance.View' },
      { title: 'Self Check-In',     icon: 'pi pi-map-marker',  route: '/attendance/check-in', permission: 'Attendance.CheckIn' },
      { title: 'Leaves',            icon: 'pi pi-calendar-times', route: '/leaves',         permission: 'Leaves.View' },
      { title: 'Holiday Calendar',  icon: 'pi pi-calendar',    route: '/leaves/holidays',   permission: 'Leaves.View' },
      { title: 'Leave Types',       icon: 'pi pi-list',        route: '/leaves/types',      permission: 'Leaves.ManageTypes' },
      { title: 'Leave Balances',    icon: 'pi pi-sliders-h',   route: '/leaves/balances',   permission: 'Leaves.ManageBalances' },
      { title: 'Payroll',           icon: 'pi pi-money-bill',   route: '/payroll',           permission: 'Payroll.View' },
      { title: 'Loans & Bonuses',   icon: 'pi pi-gift',        route: '/loans-bonuses',     permission: 'EmployeeLoans.View' },
      { title: 'Festival Bonuses',  icon: 'pi pi-star',        route: '/loans-bonuses/festival-bonuses', permission: 'FestivalBonuses.View' },
      { title: 'Final Settlements', icon: 'pi pi-sign-out',     route: '/final-settlements', permission: 'Payroll.View' }
    ]
  },

  {
    title: 'Master Data',
    icon: 'pi pi-folder',
    children: [
      { title: 'Customers',         icon: 'pi pi-id-card',   route: '/customers',         permission: 'Customers.View' },
      { title: 'Customer Pricing',  icon: 'pi pi-tags',      route: '/customer-pricing',  permission: 'Customers.View' },
      { title: 'Suppliers',         icon: 'pi pi-truck',     route: '/suppliers',         permission: 'Suppliers.View' },
      { title: 'Products',          icon: 'pi pi-box',       route: '/products',          permission: 'Products.View' },
      { title: 'Product Categories',icon: 'pi pi-sitemap',   route: '/product-categories',permission: 'ProductCategories.View' },
      { title: 'Raw Materials',     icon: 'pi pi-th-large',  route: '/raw-materials',     permission: 'RawMaterials.View' },
      { title: 'BOMs',              icon: 'pi pi-list',      route: '/boms',              permission: 'Boms.View' },
      { title: 'Styles',            icon: 'pi pi-palette',   route: '/styles',            permission: 'Styles.View' },
      { title: 'Currencies',        icon: 'pi pi-dollar',    route: '/currencies',        permission: 'Currencies.View' },
      { title: 'Units of Measure',  icon: 'pi pi-percentage',route: '/units-of-measure',  permission: 'UnitsOfMeasure.View' },
      { title: 'Warehouses',        icon: 'pi pi-warehouse', route: '/warehouses',        permission: 'Warehouses.View' }
    ]
  },

  {
    title: 'Reports',
    icon: 'pi pi-chart-bar',
    children: [
      { title: 'Stock Summary',       icon: 'pi pi-database',   route: '/reports/stock-summary',       permission: 'Reports.ViewInventory' },
      { title: 'Stock Ledger',        icon: 'pi pi-book',       route: '/reports/stock-ledger',        permission: 'Reports.ViewInventory' },
      { title: 'Dead Stock',          icon: 'pi pi-hourglass',  route: '/reports/dead-stock',          permission: 'Reports.ViewInventory' },
      { title: 'AR Ageing',           icon: 'pi pi-clock',      route: '/reports/ar-ageing',           permission: 'Reports.ViewFinance' },
      { title: 'AP Ageing',           icon: 'pi pi-clock',      route: '/reports/ap-ageing',           permission: 'Reports.ViewFinance' },
      { title: 'Sales Summary',       icon: 'pi pi-chart-line', route: '/reports/sales-summary',       permission: 'Reports.ViewSales' },
      { title: 'VAT Summary',         icon: 'pi pi-percentage', route: '/reports/vat-summary',         permission: 'Reports.ViewFinance' },
      { title: 'Margin',              icon: 'pi pi-chart-line', route: '/reports/margin',              permission: 'Reports.ViewFinance' },
      { title: 'WIP',                 icon: 'pi pi-cog',        route: '/reports/wip',                 permission: 'Reports.ViewProduction' },
      { title: 'Production Summary',  icon: 'pi pi-chart-bar',  route: '/reports/production-summary',  permission: 'Reports.ViewProduction' },
      { title: 'Wastage Variance',    icon: 'pi pi-percentage', route: '/reports/wastage-variance',    permission: 'Reports.ViewProduction' },
      { title: 'Productivity',        icon: 'pi pi-users',      route: '/reports/productivity',        permission: 'Reports.ViewProduction' },
      { title: 'Buyer Order Book',    icon: 'pi pi-book',       route: '/reports/buyer-order-book',    permission: 'Reports.ViewSales' },
      { title: 'EPB Export Register', icon: 'pi pi-globe',      route: '/reports/epb-export-register', permission: 'Reports.ViewSales' },
      { title: 'Customer Statement',  icon: 'pi pi-file',       route: '/reports/customer-statement',  permission: 'Reports.ViewFinance' },
      { title: 'Supplier Statement',  icon: 'pi pi-file',       route: '/reports/supplier-statement',  permission: 'Reports.ViewFinance' }
    ]
  },

  {
    title: 'Compliance',
    icon: 'pi pi-verified',
    children: [
      { title: 'Compliance Dashboard', icon: 'pi pi-verified',      route: '/compliance',              permission: 'Compliance.View' },
      { title: 'Certificates',         icon: 'pi pi-id-card',       route: '/compliance/certificates', permission: 'Compliance.View' },
      { title: 'Audits',               icon: 'pi pi-check-square',  route: '/compliance/audits',       permission: 'Compliance.View' }
    ]
  },

  {
    title: 'Administration',
    icon: 'pi pi-cog',
    children: [
      { title: 'Users',               icon: 'pi pi-user',      route: '/users',               permission: 'Users.View' },
      { title: 'Roles',               icon: 'pi pi-lock',      route: '/roles',               permission: 'Roles.View' },
      { title: 'Company',             icon: 'pi pi-building',  route: '/company',             permission: 'Companies.View' },
      { title: 'Factories',           icon: 'pi pi-warehouse', route: '/factories',           permission: 'Factories.View' },
      { title: 'Master Setup',        icon: 'pi pi-sliders-h', route: '/master-setup',        permission: 'MasterSetup.View' },
      { title: 'Departments',         icon: 'pi pi-sitemap',   route: '/master-setup/departments',   permission: 'MasterSetup.ManageDepartments' },
      { title: 'Designations',        icon: 'pi pi-briefcase', route: '/master-setup/designations',  permission: 'MasterSetup.ManageDesignations' },
      { title: 'Shifts',              icon: 'pi pi-clock',     route: '/master-setup/shifts',        permission: 'MasterSetup.ManageShifts' },
      { title: 'Bank Accounts',       icon: 'pi pi-building-columns', route: '/master-setup/bank-accounts', permission: 'MasterSetup.ManageBankAccounts' },
      { title: 'Approvals',           icon: 'pi pi-check-circle', route: '/approvals',        permission: 'Approvals.View' },
      { title: 'Notifications',       icon: 'pi pi-bell',      route: '/notifications',       permission: 'Notifications.View' },
      { title: 'Emails',              icon: 'pi pi-envelope',  route: '/emails',              permission: 'Emails.View' },
      { title: 'Machine Maintenance', icon: 'pi pi-wrench',    route: '/machine-maintenance', permission: 'MachineMaintenance.View' },
      { title: 'Audit Log',           icon: 'pi pi-history',   route: '/audit-log',           permission: 'AuditLog.View' }
    ]
  }
];

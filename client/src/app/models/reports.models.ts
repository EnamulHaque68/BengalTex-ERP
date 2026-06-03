// ─── Stock Summary ────────────────────────────────────────────────────────

export interface StockSummaryRowDto {
  itemType: string;                  // "RawMaterial" | "Product"
  rawMaterialId: number | null;
  productId: number | null;
  code: string;
  name: string;
  unitOfMeasureCode: string;
  totalQuantity: number;
  warehouseCount: number;
  unitCost: number;                  // weighted-average cost per unit (Phase 14)
  value: number;                     // totalQuantity × unitCost
}

export interface StockSummaryReportDto {
  generatedAt: string;
  warehouseId: number | null;
  warehouseName: string | null;
  itemType: string | null;
  rowCount: number;
  totalRawMaterialQuantity: number;
  totalProductQuantity: number;
  totalInventoryValue: number;       // Σ value across all rows (Phase 14)
  rows: StockSummaryRowDto[];
}

// ─── AR Ageing ────────────────────────────────────────────────────────────

export interface ArAgeingInvoiceDto {
  invoiceId: number;
  invoiceCode: string;
  salesOrderCode: string;
  invoiceDate: string;
  dueDate: string;
  daysPastDue: number;
  bucket: string;                    // "Current" | "1-30" | "31-60" | "61-90" | "90+"
  totalAmount: number;
  amountPaid: number;
  amountDue: number;
}

export interface ArAgeingCustomerDto {
  customerId: number;
  customerCode: string;
  customerName: string;
  current: number;
  days1To30: number;
  days31To60: number;
  days61To90: number;
  days90Plus: number;
  totalOutstanding: number;
  invoiceCount: number;
  invoices: ArAgeingInvoiceDto[];
}

export interface ArAgeingReportDto {
  asOfDate: string;
  customerCount: number;
  invoiceCount: number;
  totalCurrent: number;
  total1To30: number;
  total31To60: number;
  total61To90: number;
  total90Plus: number;
  totalOutstanding: number;
  customers: ArAgeingCustomerDto[];
}

// ─── AP Ageing ────────────────────────────────────────────────────────────

export interface ApAgeingInvoiceDto {
  invoiceId: number;
  invoiceCode: string;
  purchaseOrderCode: string;
  supplierInvoiceNumber: string | null;
  invoiceDate: string;
  dueDate: string;
  daysPastDue: number;
  bucket: string;
  totalAmount: number;
  amountPaid: number;
  amountDue: number;
}

export interface ApAgeingSupplierDto {
  supplierId: number;
  supplierCode: string;
  supplierName: string;
  current: number;
  days1To30: number;
  days31To60: number;
  days61To90: number;
  days90Plus: number;
  totalOutstanding: number;
  invoiceCount: number;
  invoices: ApAgeingInvoiceDto[];
}

export interface ApAgeingReportDto {
  asOfDate: string;
  supplierCount: number;
  invoiceCount: number;
  totalCurrent: number;
  total1To30: number;
  total31To60: number;
  total61To90: number;
  total90Plus: number;
  totalOutstanding: number;
  suppliers: ApAgeingSupplierDto[];
}

// ─── Sales Summary ────────────────────────────────────────────────────────

export interface SalesSummaryRowDto {
  customerId: number;
  customerCode: string;
  customerName: string;
  salesOrderCount: number;
  salesOrderTotal: number;
  deliveryNoteCount: number;
  deliveryNoteValue: number;
  invoiceCount: number;
  invoicedNet: number;                  // Phase 12
  vatCollected: number;                 // Phase 12
  invoicedTotal: number;                // gross
  amountCollected: number;
  amountOutstanding: number;
}

export interface SalesSummaryReportDto {
  fromDate: string;
  toDate: string;
  customerId: number | null;
  customerName: string | null;
  customerCount: number;
  salesOrderCount: number;
  salesOrderTotal: number;
  deliveryNoteCount: number;
  deliveryNoteValue: number;
  invoiceCount: number;
  invoicedNet: number;
  vatCollected: number;
  invoicedTotal: number;
  amountCollected: number;
  amountOutstanding: number;
  rows: SalesSummaryRowDto[];
}

// ─── VAT Summary ──────────────────────────────────────────────────────────

export interface VatSummaryMonthDto {
  year: number;
  month: number;
  monthLabel: string;
  outputVatNet: number;
  outputVatAmount: number;
  inputVatNet: number;
  inputVatAmount: number;
  netVatLiability: number;
}

export interface VatSummaryReportDto {
  fromDate: string;
  toDate: string;
  customerInvoiceCount: number;
  outputVatNet: number;
  outputVatAmount: number;
  outputVatGross: number;
  supplierInvoiceCount: number;
  inputVatNet: number;
  inputVatAmount: number;
  inputVatGross: number;
  netVatLiability: number;              // OutputVat − InputVat (positive = owe NBR)
  months: VatSummaryMonthDto[];
}

// ─── Dashboard KPIs ───────────────────────────────────────────────────────

// ─── Margin (COGS) Report ─────────────────────────────────────────────────

export interface MarginReportRowDto {
  productId: number;
  productCode: string;
  productName: string;
  unitOfMeasureCode: string;
  quantitySold: number;
  revenue: number;
  cogs: number;
  unitCost: number;
  margin: number;
  marginPercent: number;
}

export interface MarginReportDto {
  fromDate: string;
  toDate: string;
  customerId: number | null;
  customerName: string | null;
  productCount: number;
  totalRevenue: number;
  totalCogs: number;
  totalMargin: number;
  overallMarginPercent: number;
  rows: MarginReportRowDto[];
}

export interface DashboardKpisDto {
  generatedAt: string;
  stockItemCount: number;
  totalStockValue: number;           // Σ (qty × WAC) across RM + Product (Phase 14)
  outstandingArAmount: number;
  outstandingArInvoiceCount: number;
  outstandingApAmount: number;
  outstandingApInvoiceCount: number;
  thisMonthSalesAmount: number;
  thisMonthSalesInvoiceCount: number;
  monthStart: string;
  monthEnd: string;
}

// ─── WIP Report ───────────────────────────────────────────────────────────

export interface WipReportRowDto {
  productionOrderId: number;
  code: string;
  productId: number;
  productCode: string;
  productName: string;
  targetQuantity: number;
  plannedStartDate: string | null;
  plannedEndDate: string | null;
  actualStartDate: string | null;
  daysRunning: number;
  isOverdue: boolean;
  totalStages: number;
  completedStages: number;
  stageProgressPercent: number;
  currentStageName: string | null;
  issueWarehouseName: string;
  receiveWarehouseName: string;
}

export interface WipReportDto {
  asOfDate: string;
  totalOrdersInProgress: number;
  totalTargetQuantity: number;
  rows: WipReportRowDto[];
}

// ─── Production Summary Report ────────────────────────────────────────────

export interface ProductionSummaryRowDto {
  productId: number;
  productCode: string;
  productName: string;
  orderCount: number;
  totalQuantityProduced: number;
  averageQuantityPerOrder: number;
  averageCycleTimeDays: number;
}

export interface ProductionSummaryReportDto {
  fromDate: string;
  toDate: string;
  totalOrdersCompleted: number;
  totalQuantityProduced: number;
  rows: ProductionSummaryRowDto[];
}

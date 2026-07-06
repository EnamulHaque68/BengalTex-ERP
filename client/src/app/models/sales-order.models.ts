// ─── Sales Order ──────────────────────────────────────────────────────────

export const SO_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Pending Approval', value: 'PendingApproval' },
  { label: 'Confirmed', value: 'Confirmed' },
  { label: 'Partially Dispatched', value: 'PartiallyDispatched' },
  { label: 'Dispatched', value: 'Dispatched' },
  { label: 'Delivered', value: 'Delivered' },
  { label: 'Closed', value: 'Closed' },
  { label: 'Cancelled', value: 'Cancelled' }
];

export interface SalesOrderLineDto {
  id: number;
  productId: number;
  productCode: string;
  productName: string;
  unitOfMeasureCode: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  dispatchedQuantity?: number;            // populated once DNs start posting against this SO
  invoicedQuantity?: number;             // qty already billed (remaining-to-invoice = quantity − this)
  sortOrder: number;
  lineNotes: string | null;
  producedQuantity?: number;              // Phase 1 — Σ qty of Completed production orders on this line
  allocatedQuantity?: number;             // Phase 1 — Σ qty of all non-cancelled production orders on this line
  styleId: number | null;      // Phase A3
  styleName: string | null;
}

export interface SalesOrderDto {
  id: number;
  code: string;
  customerId: number;
  customerCode: string;
  customerName: string;
  orderDate: string;                       // DateOnly → "YYYY-MM-DD"
  requiredDeliveryDate: string | null;
  customerPoRef: string | null;
  deliveryAddress: string | null;
  status: string;
  currencyId: number;
  currencyCode: string;
  currencySymbol: string;
  exchangeRate: number;
  confirmedAt: string | null;
  confirmedBy: string | null;
  notes: string | null;
  totalAmount: number;                     // document currency
  baseTotalAmount: number;                 // BDT
  lines: SalesOrderLineDto[];
  // Phase 1 — production progress (computed from linked production orders)
  orderedQuantity?: number;
  producedQuantity?: number;
  productionProgressPercent?: number;
  productionStatus?: string;               // NotStarted | Planning | PartiallyProduced | Produced
  // Sales A3 — invoice coverage summary + traceability
  invoicedQuantity?: number;
  invoicedAmount?: number;                 // document currency
  invoiceStatus?: string;                  // NotInvoiced | PartiallyInvoiced | FullyInvoiced
  relatedInvoices?: SalesOrderInvoiceRefDto[];
}

export interface SalesOrderInvoiceRefDto {
  id: number;
  code: string;
  status: string;
  invoiceDate: string;
  totalAmount: number;
  amountPaid: number;
}

export interface SalesOrderListItemDto {
  id: number;
  code: string;
  customerId: number;
  customerName: string;
  orderDate: string;
  requiredDeliveryDate: string | null;
  status: string;
  currencyCode: string;
  exchangeRate: number;
  lineCount: number;
  totalAmount: number;                     // document currency
  baseTotalAmount: number;                 // BDT
  productionProgressPercent?: number;      // Phase 1
  productionStatus?: string;               // Phase 1
  orderedQuantity?: number;                // Sales A2 — invoice coverage
  invoicedQuantity?: number;
  invoiceStatus?: string;                  // NotInvoiced | PartiallyInvoiced | FullyInvoiced
}

export interface SalesOrderLineInput {
  productId: number;
  styleId?: number | null;   // Phase A3
  quantity: number;
  unitPrice: number;
  lineNotes: string | null;
}

export interface CreateSalesOrderRequest {
  customerId: number;
  orderDate: string;
  requiredDeliveryDate: string | null;
  customerPoRef: string | null;
  deliveryAddress: string | null;
  notes: string | null;
  currencyId: number;
  exchangeRate: number;
  lines: SalesOrderLineInput[];
}

export interface UpdateSalesOrderRequest {
  customerId: number;
  orderDate: string;
  requiredDeliveryDate: string | null;
  customerPoRef: string | null;
  deliveryAddress: string | null;
  notes: string | null;
  currencyId: number;
  exchangeRate: number;
  lines: SalesOrderLineInput[];
}

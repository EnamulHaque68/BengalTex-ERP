// ─── Sales Order ──────────────────────────────────────────────────────────

export const SO_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
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
  sortOrder: number;
  lineNotes: string | null;
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
  confirmedAt: string | null;
  confirmedBy: string | null;
  notes: string | null;
  totalAmount: number;
  lines: SalesOrderLineDto[];
}

export interface SalesOrderListItemDto {
  id: number;
  code: string;
  customerId: number;
  customerName: string;
  orderDate: string;
  requiredDeliveryDate: string | null;
  status: string;
  lineCount: number;
  totalAmount: number;
}

export interface SalesOrderLineInput {
  productId: number;
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
  lines: SalesOrderLineInput[];
}

export interface UpdateSalesOrderRequest {
  customerId: number;
  orderDate: string;
  requiredDeliveryDate: string | null;
  customerPoRef: string | null;
  deliveryAddress: string | null;
  notes: string | null;
  lines: SalesOrderLineInput[];
}

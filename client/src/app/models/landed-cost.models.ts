// ─── Landed Cost Voucher ────────────────────────────────────────────────────

export const LANDED_COST_STATUSES: { label: string; value: string }[] = [
  { label: 'Draft', value: 'Draft' },
  { label: 'Posted', value: 'Posted' }
];

export const LANDED_COST_ALLOCATION_BASES: { label: string; value: string }[] = [
  { label: 'By Value (qty × price)', value: 'ByValue' },
  { label: 'By Quantity', value: 'ByQuantity' }
];

export const LANDED_COST_CHARGE_TYPES: { label: string; value: string }[] = [
  { label: 'Freight', value: 'Freight' },
  { label: 'Customs Duty', value: 'CustomsDuty' },
  { label: 'Clearing & Handling', value: 'ClearingHandling' },
  { label: 'Insurance', value: 'Insurance' },
  { label: 'Other', value: 'Other' }
];

export const LANDED_COST_PAYMENT_METHODS: { label: string; value: string }[] = [
  { label: 'Cash', value: 'Cash' },
  { label: 'Bank Transfer', value: 'BankTransfer' },
  { label: 'Cheque', value: 'Cheque' },
  { label: 'Mobile Banking', value: 'MobileBanking' },
  { label: 'Other', value: 'Other' }
];

export interface LandedCostChargeInput {
  chargeType: string;
  amount: number;
  notes: string | null;
}

export interface LandedCostChargeDto {
  id: number;
  chargeType: string;
  amount: number;
  notes: string | null;
  sortOrder: number;
}

export interface LandedCostAllocationLineDto {
  rawMaterialId: number;
  rawMaterialCode: string;
  rawMaterialName: string;
  receivedQuantity: number;
  lineValue: number;
  allocatedAmount: number;
  addedUnitCost: number;
}

export interface LandedCostVoucherDto {
  id: number;
  code: string;
  voucherDate: string;
  goodsReceiptNoteId: number;
  goodsReceiptCode: string;
  purchaseOrderCode: string;
  supplierName: string;
  allocationBasis: string;
  paymentMethod: string;
  status: string;
  postedAt: string | null;
  postedBy: string | null;
  notes: string | null;
  totalCharges: number;
  charges: LandedCostChargeDto[];
  allocation: LandedCostAllocationLineDto[];
}

export interface LandedCostVoucherListItemDto {
  id: number;
  code: string;
  voucherDate: string;
  goodsReceiptCode: string;
  supplierName: string;
  allocationBasis: string;
  status: string;
  chargeCount: number;
  totalCharges: number;
}

export interface SaveLandedCostRequest {
  voucherDate: string;
  goodsReceiptNoteId: number;
  allocationBasis: string;
  paymentMethod: string;
  notes: string | null;
  charges: LandedCostChargeInput[];
}

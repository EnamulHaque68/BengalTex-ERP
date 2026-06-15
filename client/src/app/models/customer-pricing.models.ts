export interface CustomerProductPriceDto {
  id: number;
  customerId: number;
  customerName: string;
  productId: number;
  productCode: string;
  productName: string;
  productUnitOfMeasureCode: string;
  defaultSalesPrice: number;
  unitPrice: number;
  isActive: boolean;
  notes: string | null;
}

export interface CreateCustomerProductPriceRequest {
  customerId: number;
  productId: number;
  unitPrice: number;
  notes: string | null;
}

export interface UpdateCustomerProductPriceRequest {
  id: number;
  unitPrice: number;
  isActive: boolean;
  notes: string | null;
}

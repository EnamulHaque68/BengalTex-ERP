// ─── Phase A6c — dated exchange rates ──────────────────────────────────────

export interface ExchangeRateDto {
  id: number;
  currencyId: number;
  currencyCode: string;
  rateDate: string;
  rate: number;
  source: string | null;
}

export interface SetExchangeRateRequest {
  currencyId: number;
  rateDate: string;
  rate: number;
  source: string | null;
}

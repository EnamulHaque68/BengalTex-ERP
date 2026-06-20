// ─── Raw Material Substitutes (alternative materials) ───────────────────────

export interface RawMaterialSubstituteDto {
  id: number;
  rawMaterialId: number;
  substituteRawMaterialId: number;
  substituteCode: string;
  substituteName: string;
  substituteUnit: string;
  conversionFactor: number;
  substituteOnHand: number;
  notes: string | null;
  isActive: boolean;
}

export interface CreateRawMaterialSubstituteRequest {
  rawMaterialId: number;
  substituteRawMaterialId: number;
  conversionFactor: number;
  notes: string | null;
  isActive: boolean;
}

export interface UpdateRawMaterialSubstituteRequest {
  id: number;
  conversionFactor: number;
  notes: string | null;
  isActive: boolean;
}

// ─── Work Center (Phase 4 — capacity & resource planning) ──────────────────

export interface WorkCenterDto {
  id: number;
  code: string;
  name: string;
  type: string | null;
  location: string | null;
  capacityPerDay: number | null;
  costPerHour: number | null;
  notes: string | null;
  isActive: boolean;
  plannedLoad: number;          // Σ planned qty of open stages on this work center
  openStageCount: number;
  loadPercent: number | null;   // plannedLoad / capacityPerDay × 100 (null if no capacity)
}

export interface CreateWorkCenterRequest {
  code: string;
  name: string;
  type: string | null;
  location: string | null;
  capacityPerDay: number | null;
  costPerHour: number | null;
  notes: string | null;
}

export interface UpdateWorkCenterRequest {
  name: string;
  type: string | null;
  location: string | null;
  capacityPerDay: number | null;
  costPerHour: number | null;
  notes: string | null;
  isActive: boolean;
}

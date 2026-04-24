import { fetchApi } from './api';

export interface FleetAsset {
  id: number;
  code: string;
  siteName: string;
  location: string;
  mode: string;
  powerKw: number;
  socPct: number;
  sohPct: number;
  temperatureC: number;
  nextAction: string;
  nextActionWindow: string;
  faultCode: string | null;
  chemistry: string;
  powerRatingKw: number;
  capacityKwh: number;
  durationH: number;
}

export interface FleetSummary {
  totalCapacityMwh: number;
  availableNowMwh: number;
  netPowerMwh: number;
  assetCount: number;
  assets: FleetAsset[];
}

export interface BatteryHistoryPoint {
  timestamp: string;
  powerKw: number;
  socPct: number;
}

export async function getFleetSummary(): Promise<FleetSummary> {
  return fetchApi<FleetSummary>('/api/batteries/fleet');
}

export async function getBatteryHistory(code: string): Promise<BatteryHistoryPoint[]> {
  return fetchApi<BatteryHistoryPoint[]>(`/api/batteries/${encodeURIComponent(code)}/history24h`);
}

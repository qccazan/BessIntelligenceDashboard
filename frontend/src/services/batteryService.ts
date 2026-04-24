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
}

export interface FleetSummary {
  totalCapacityMwh: number;
  availableNowMwh: number;
  netPowerMwh: number;
  assetCount: number;
  assets: FleetAsset[];
}

export async function getFleetSummary(): Promise<FleetSummary> {
  return fetchApi<FleetSummary>('/api/batteries/fleet');
}

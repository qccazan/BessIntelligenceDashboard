import { fetchApi } from './api';

export interface SolarSummary {
  totalCapacityMwp: number;
  totalPanelCount: number;
  yesterdayProductionMwh: number;
}

export async function getSolarSummary(): Promise<SolarSummary> {
  return fetchApi<SolarSummary>('/api/solar/summary');
}

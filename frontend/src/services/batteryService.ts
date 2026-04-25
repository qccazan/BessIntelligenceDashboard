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

/** Simple deterministic hash for mock data variation (mirrors backend HashNoise). */
function hashNoise(seed: number): number {
  let h = (seed * 2654435761) >>> 0;
  h = ((h >> 16) ^ h) * 0x45d9f3b;
  h = ((h >> 16) ^ h) >>> 0;
  return (h % 201 - 100) / 100; // -1 … +1
}

/**
 * Fills gaps in the 24 h history so the replay always has 96 data points.
 * Missing intervals get idle-like mock power and interpolated SoC.
 */
function fillHistoryGaps(data: BatteryHistoryPoint[]): BatteryHistoryPoint[] {
  const INTERVAL_MS = 15 * 60 * 1000; // 15 min
  const EXPECTED = 96;

  // Determine the 24 h window: midnight-yesterday → midnight-today (Amsterdam local)
  const now = new Date();
  const midnightToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const midnightYesterday = new Date(midnightToday.getTime() - 24 * 60 * 60 * 1000);

  // Index existing points by their rounded slot
  const existing = new Map<number, BatteryHistoryPoint>();
  for (const p of data) {
    const slot = Math.round((new Date(p.timestamp).getTime() - midnightYesterday.getTime()) / INTERVAL_MS);
    if (slot >= 0 && slot < EXPECTED) existing.set(slot, p);
  }

  // If the data is already complete, return as-is
  if (existing.size >= EXPECTED) return data;

  const filled: BatteryHistoryPoint[] = [];
  // Use last known SoC for forward-fill; default 50 %
  let lastSoc = existing.get(0)?.socPct ?? 50;

  for (let i = 0; i < EXPECTED; i++) {
    const real = existing.get(i);
    if (real) {
      lastSoc = real.socPct;
      filled.push(real);
    } else {
      // Small idle-like fluctuation
      const noise = hashNoise(i * 137);
      const powerKw = Math.round(noise * 5 * 10) / 10; // -5 … +5 kW (idle range)
      const socDelta = powerKw * 0.25 / 1000 * 100; // tiny
      lastSoc = Math.max(10, Math.min(95, lastSoc + socDelta));
      const ts = new Date(midnightYesterday.getTime() + i * INTERVAL_MS);
      filled.push({
        timestamp: ts.toISOString(),
        powerKw,
        socPct: Math.round(lastSoc * 10) / 10,
      });
    }
  }

  return filled;
}

export async function getBatteryHistory(code: string): Promise<BatteryHistoryPoint[]> {
  const data = await fetchApi<BatteryHistoryPoint[]>(`/api/batteries/${encodeURIComponent(code)}/history24h`);
  return fillHistoryGaps(data);
}

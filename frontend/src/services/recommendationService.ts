import { fetchApi } from './api';

export interface BatteryAction {
  id: number;
  batteryId: number;
  batteryCode: string;
  action: string;
  windowStart: string;
  windowEnd: string;
  reason: string;
}

export interface Recommendation {
  id: number;
  generatedAt: string;
  portfolioAction: string;
  chargeWindowStart: string;
  chargeWindowEnd: string;
  dischargeWindowStart: string;
  dischargeWindowEnd: string;
  chargePrice: number;
  dischargePrice: number;
  priceSpreadMultiplier: number;
  avg30dSpreadMultiplier: number;
  confidencePct: number;
  explanation: string;
  estimatedCaptureEur: number;
  batteryActions: BatteryAction[];
}

export async function getLatestRecommendation(): Promise<Recommendation> {
  return fetchApi<Recommendation>('/api/recommendations/latest');
}

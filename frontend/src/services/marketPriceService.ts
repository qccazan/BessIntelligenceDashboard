import { fetchApi } from './api';

export interface MarketPrice {
  id: number;
  hourStart: string;
  priceEurMwh: number;
  market: string;
  currency: string;
}

export async function getMarketPrices(date?: string): Promise<MarketPrice[]> {
  const params = date ? `?date=${date}` : '';
  return fetchApi<MarketPrice[]>(`/api/market-prices${params}`);
}

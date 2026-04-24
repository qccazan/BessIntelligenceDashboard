import { useEffect, useState } from 'react';
import { getMarketPrices } from '../services/marketPriceService';
import type { MarketPrice } from '../services/marketPriceService';
import { getLatestRecommendation } from '../services/recommendationService';
import type { Recommendation } from '../services/recommendationService';
import { PerBatteryStrip } from './PerBatteryStrip';

interface MarketForecastCardProps {
  selectedAssetId: string;
  onSelectAsset: (assetCode: string) => void;
}

export function MarketForecastCard({ selectedAssetId, onSelectAsset }: MarketForecastCardProps) {
  const [prices, setPrices] = useState<MarketPrice[]>([]);
  const [recommendation, setRecommendation] = useState<Recommendation | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    Promise.all([getMarketPrices(), getLatestRecommendation()])
      .then(([p, r]) => {
        setPrices(p);
        setRecommendation(r);
      })
      .catch((err) => setError(err.message));
  }, []);

  const updatedTime = recommendation
    ? new Date(recommendation.generatedAt).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })
    : '';

  const isHold = recommendation?.portfolioAction === 'Hold';

  return (
    <div className="bg-white rounded-[14px] p-5 border border-[rgba(88,70,180,0.14)] col-span-full" data-testid="market-forecast-card">
      {/* Card header */}
      <div className="flex items-start justify-between mb-4 gap-2">
        <div>
          <p className="text-[15px] font-medium m-0 text-[#1C1B2E]">Market forecast — next 24 hours</p>
          <p className="text-xs text-[#8C8AA8] mt-0.5 m-0">
            Day-ahead prices · Portfolio recommendation{updatedTime ? ` · Updated ${updatedTime}` : ''}
          </p>
        </div>
        <span className="text-[11px] px-[11px] py-[5px] rounded-full font-medium bg-[#F0ECFE] text-[#4A30B5] whitespace-nowrap inline-flex items-center gap-1.5" data-testid="ai-active-badge">
          AI active
        </span>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {/* Recommendation block */}
      {recommendation && (
        <div
          className="rounded-[10px] p-4 px-[18px] mb-4 border border-[rgba(123,92,246,0.2)]"
          style={{ background: 'linear-gradient(135deg, #F0ECFE 0%, #DFECFF 100%)' }}
          data-testid="recommendation-block"
        >
          <p className="text-[11px] text-[#4A30B5] m-0 mb-2 font-medium uppercase tracking-wider">
            Portfolio recommendation
          </p>

          {isHold ? (
            <HoldLayout recommendation={recommendation} />
          ) : (
            <ChargeDischargeLayout recommendation={recommendation} />
          )}

          {/* Explainability sentence (US-07-04) */}
          <p className="text-[13px] text-[#4A30B5] mt-2.5 m-0 leading-relaxed" data-testid="rec-explanation">
            {recommendation.explanation}
          </p>

          {/* Per-battery strip (US-07-05) */}
          <PerBatteryStrip
            batteryActions={recommendation.batteryActions}
            selectedAssetId={selectedAssetId}
            onSelectAsset={onSelectAsset}
          />
        </div>
      )}

      {/* Price chart (US-07-01 + US-07-02) */}
      {prices.length > 0 && recommendation && (
        <PriceChart prices={prices} recommendation={recommendation} />
      )}
    </div>
  );
}

function ChargeDischargeLayout({ recommendation }: { recommendation: Recommendation }) {
  return (
    <>
      <div className="flex items-baseline gap-3 flex-wrap" data-testid="rec-main">
        <span className="text-[22px] font-medium text-[#261761]" data-testid="rec-action">
          {recommendation.portfolioAction === 'Arbitrage' ? 'Coordinated charge' : recommendation.portfolioAction}
        </span>
        <div className="flex flex-col gap-0.5 text-sm text-[#4A30B5]">
          <span data-testid="rec-charge-window">
            {recommendation.chargeWindowStart} – {recommendation.chargeWindowEnd} · ~{Math.round(recommendation.chargePrice)} €/MWh
          </span>
          <span data-testid="rec-discharge-window">
            {recommendation.dischargeWindowStart} – {recommendation.dischargeWindowEnd} · ~{Math.round(recommendation.dischargePrice)} €/MWh
          </span>
        </div>
        <span
          className="text-[11px] text-white bg-[#7B5CF6] px-[11px] py-1 rounded-full font-medium ml-auto"
          data-testid="rec-confidence"
        >
          {recommendation.confidencePct}% confidence
        </span>
      </div>
    </>
  );
}

function HoldLayout({ recommendation }: { recommendation: Recommendation }) {
  return (
    <>
      <div className="flex items-baseline gap-3 flex-wrap" data-testid="rec-main">
        <span className="text-[22px] font-medium text-[#261761]" data-testid="rec-action">
          Hold — no cycle recommended
        </span>
        <span
          className="text-[11px] text-white bg-[#7B5CF6] px-[11px] py-1 rounded-full font-medium ml-auto"
          data-testid="rec-confidence"
        >
          {recommendation.confidencePct}% confidence
        </span>
      </div>
      <p className="text-sm text-[#4A30B5] mt-2 m-0" data-testid="hold-message">
        Insufficient price spread — battery longevity protected
      </p>
    </>
  );
}

/* ─── Price Chart (SVG) ──────────────────────────────────────────── */

function PriceChart({ prices, recommendation }: { prices: MarketPrice[]; recommendation: Recommendation }) {
  const isHold = recommendation.portfolioAction === 'Hold';

  // Chart dimensions (matching mock)
  const width = 600;
  const height = 160;
  const left = 30;
  const right = 580;
  const top = 15;
  const bottom = 140;
  const chartW = right - left;
  const chartH = bottom - top;

  // Price range
  const priceValues = prices.map((p) => p.priceEurMwh);
  const maxPrice = Math.max(...priceValues);
  const minPriceVal = Math.min(...priceValues);

  // Dynamic Y-axis levels: 0, and steps up to cover max
  const step = maxPrice <= 100 ? 50 : maxPrice <= 200 ? 100 : 100;
  const yLevels: number[] = [];
  for (let v = 0; v <= maxPrice + step; v += step) {
    yLevels.push(v);
  }
  const yMax = yLevels[yLevels.length - 1];

  function priceToY(price: number) {
    return bottom - (price / yMax) * chartH;
  }

  function indexToX(i: number) {
    return left + (i / (prices.length - 1)) * chartW;
  }

  // Build the curve path
  const linePoints = prices.map((p, i) => `${indexToX(i)} ${priceToY(p.priceEurMwh)}`);
  const linePath = `M ${linePoints.join(' L ')}`;
  const areaPath = `${linePath} L ${right} ${bottom} L ${left} ${bottom} Z`;

  // Min/max price points
  const minIdx = priceValues.indexOf(minPriceVal);
  const maxIdx = priceValues.indexOf(maxPrice);
  const minDot = { x: indexToX(minIdx), y: priceToY(minPriceVal) };
  const maxDot = { x: indexToX(maxIdx), y: priceToY(maxPrice) };

  // Charge/discharge window positions
  function hourToIndex(hourStr: string): number {
    const hours = prices.map((p) => new Date(p.hourStart).getHours());
    const target = parseInt(hourStr.split(':')[0], 10);
    const idx = hours.indexOf(target);
    return idx >= 0 ? idx : 0;
  }

  let chargeZone = null;
  let dischargeZone = null;

  if (!isHold) {
    const chargeStartIdx = hourToIndex(recommendation.chargeWindowStart);
    const chargeEndIdx = hourToIndex(recommendation.chargeWindowEnd);
    const dischargeStartIdx = hourToIndex(recommendation.dischargeWindowStart);
    const dischargeEndIdx = hourToIndex(recommendation.dischargeWindowEnd);

    const cX1 = indexToX(chargeStartIdx);
    const cX2 = indexToX(chargeEndIdx > chargeStartIdx ? chargeEndIdx : chargeStartIdx + 2);
    const dX1 = indexToX(dischargeStartIdx);
    const dX2 = indexToX(dischargeEndIdx > dischargeStartIdx ? dischargeEndIdx : dischargeStartIdx + 2);

    chargeZone = { x: cX1, w: cX2 - cX1 };
    dischargeZone = { x: dX1, w: dX2 - dX1 };
  }

  // Time tick labels (7 evenly spaced)
  const tickCount = 7;
  const tickIndices = Array.from({ length: tickCount }, (_, i) =>
    Math.round((i / (tickCount - 1)) * (prices.length - 1))
  );
  const tickLabels = tickIndices.map((idx) => {
    const d = new Date(prices[idx].hourStart);
    return d.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });
  });

  return (
    <div data-testid="price-chart">
      <svg viewBox={`0 0 ${width} ${height}`} width="100%" height="180" preserveAspectRatio="none" style={{ display: 'block' }}>
        <defs>
          <linearGradient id="priceAreaGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#7B5CF6" stopOpacity={0.32} />
            <stop offset="100%" stopColor="#7B5CF6" stopOpacity={0.02} />
          </linearGradient>
          <pattern id="chargeZonePattern" width="6" height="6" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">
            <rect width="6" height="6" fill="#DDF7EE" />
            <line x1="0" y1="0" x2="0" y2="6" stroke="#17B890" strokeWidth="1" opacity="0.35" />
          </pattern>
          <pattern id="dischargeZonePattern" width="6" height="6" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">
            <rect width="6" height="6" fill="#FFE8DE" />
            <line x1="0" y1="0" x2="0" y2="6" stroke="#FF7A3D" strokeWidth="1" opacity="0.4" />
          </pattern>
        </defs>

        {/* Charge zone overlay (US-07-02) */}
        {chargeZone && (
          <>
            <rect
              x={chargeZone.x} y={top} width={chargeZone.w} height={chartH}
              fill="url(#chargeZonePattern)" rx="4"
              data-testid="charge-zone"
            />
            <text
              x={chargeZone.x + chargeZone.w / 2} y={top + 13}
              textAnchor="middle" fontSize="10" fill="#0B7757" fontWeight="500"
              data-testid="charge-label"
            >
              CHARGE
            </text>
          </>
        )}

        {/* Discharge zone overlay (US-07-02) */}
        {dischargeZone && (
          <>
            <rect
              x={dischargeZone.x} y={top} width={dischargeZone.w} height={chartH}
              fill="url(#dischargeZonePattern)" rx="4"
              data-testid="discharge-zone"
            />
            <text
              x={dischargeZone.x + dischargeZone.w / 2} y={top + 13}
              textAnchor="middle" fontSize="10" fill="#B8461A" fontWeight="500"
              data-testid="discharge-label"
            >
              DISCHARGE
            </text>
          </>
        )}

        {/* Grid lines (US-07-01 AC-4) */}
        {yLevels.map((level) => (
          <line
            key={level}
            x1={left} y1={priceToY(level)} x2={right} y2={priceToY(level)}
            stroke="#DCD3FB" strokeWidth="0.5"
            strokeDasharray={level === 0 ? undefined : '2,3'}
            data-testid="grid-line"
          />
        ))}

        {/* Y-axis labels (US-07-01 AC-3) */}
        {yLevels.map((level) => (
          <text
            key={`label-${level}`}
            x={left - 4} y={priceToY(level) + 3}
            textAnchor="end" fontSize="9" fill="#8C8AA8"
            data-testid="price-level-label"
          >
            {level}
          </text>
        ))}

        {/* Area fill (US-07-01 AC-5) */}
        <path d={areaPath} fill="url(#priceAreaGrad)" data-testid="price-area" />

        {/* Price curve (US-07-01 AC-1) */}
        <path d={linePath} fill="none" stroke="#7B5CF6" strokeWidth="1.8" strokeLinejoin="round" data-testid="price-curve" />

        {/* Min/Max dots (US-07-02 AC-4) */}
        <circle cx={minDot.x} cy={minDot.y} r="4" fill="#17B890" stroke="white" strokeWidth="1.5" data-testid="min-price-dot" />
        <circle cx={maxDot.x} cy={maxDot.y} r="4" fill="#FF7A3D" stroke="white" strokeWidth="1.5" data-testid="max-price-dot" />
      </svg>

      {/* Time axis (US-07-01 AC-2) */}
      <div className="flex justify-between text-[10px] text-[#8C8AA8] px-1 mt-1.5" data-testid="time-axis">
        {tickLabels.map((label, i) => (
          <span key={i} data-testid="time-tick">{label}</span>
        ))}
      </div>
    </div>
  );
}

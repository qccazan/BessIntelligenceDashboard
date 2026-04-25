import { useState, useEffect, useRef, useCallback } from 'react';
import type { BatteryHistoryPoint, FleetAsset } from '../services/batteryService';
import { getBatteryHistory } from '../services/batteryService';

interface ReplayCardProps {
  selectedAssetCode: string;
  capacity: number;
  asset?: FleetAsset | null;
}

const STEP_MS = 100;
const BAR_COLOURS = { charging: '#17B890', discharging: '#FF7A3D', idle: '#C4BFE0' };
const PILL_STYLES: Record<string, { bg: string; text: string; dot: string }> = {
  Charging: { bg: 'bg-[#DDF7EE]', text: 'text-[#0B7757]', dot: 'bg-[#17B890]' },
  Discharging: { bg: 'bg-[#FFE8DE]', text: 'text-[#B8461A]', dot: 'bg-[#FF7A3D]' },
  Idle: { bg: 'bg-[#E8E6F2]', text: 'text-[#5C5A7A]', dot: 'bg-[#8C8AA8]' },
};

function getMode(powerKw: number): string {
  if (powerKw > 5) return 'Charging';
  if (powerKw < -5) return 'Discharging';
  return 'Idle';
}

function getBarColour(powerKw: number): string {
  if (powerKw > 5) return BAR_COLOURS.charging;
  if (powerKw < -5) return BAR_COLOURS.discharging;
  return BAR_COLOURS.idle;
}

function formatTime(ts: string): string {
  const d = new Date(ts);
  return d.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false });
}

export function ReplayCard({ selectedAssetCode, capacity, asset }: ReplayCardProps) {
  const [history, setHistory] = useState<BatteryHistoryPoint[]>([]);
  const [loading, setLoading] = useState(true);
  const [playheadIdx, setPlayheadIdx] = useState(0);
  const [playing, setPlaying] = useState(false);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const scrubRef = useRef<HTMLDivElement>(null);

  // Fetch data on asset change
  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setPlaying(false);
    if (intervalRef.current) clearInterval(intervalRef.current);

    getBatteryHistory(selectedAssetCode)
      .then((data) => {
        if (!cancelled) {
          setHistory(data);
          setPlayheadIdx(0);
          setLoading(false);
          // Auto-play after load
          setPlaying(true);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setHistory([]);
          setLoading(false);
        }
      });
    return () => { cancelled = true; };
  }, [selectedAssetCode]);

  // Animation interval
  useEffect(() => {
    if (playing && history.length > 0) {
      intervalRef.current = setInterval(() => {
        setPlayheadIdx((prev) => {
          if (prev >= history.length - 1) {
            setPlaying(false);
            return prev;
          }
          return prev + 1;
        });
      }, STEP_MS);
    }
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [playing, history.length]);

  const togglePlay = useCallback(() => {
    if (playheadIdx >= history.length - 1) {
      setPlayheadIdx(0);
      setPlaying(true);
    } else {
      setPlaying((p) => !p);
    }
  }, [playheadIdx, history.length]);

  const handleScrubClick = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    if (!scrubRef.current || history.length === 0) return;
    const rect = scrubRef.current.getBoundingClientRect();
    const ratio = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
    const idx = Math.round(ratio * (history.length - 1));
    setPlayheadIdx(idx);
  }, [history.length]);

  // Current data point
  const current = history[playheadIdx];
  const mode = current ? getMode(current.powerKw) : 'Idle';
  const pillStyle = PILL_STYLES[mode] ?? PILL_STYLES.Idle;

  // Energy calculations
  const charged = history.reduce((sum, p) => p.powerKw > 0 ? sum + p.powerKw * 0.25 : sum, 0);
  const discharged = history.reduce((sum, p) => p.powerKw < 0 ? sum + Math.abs(p.powerKw) * 0.25 : sum, 0);
  const netCycles = capacity > 0 ? ((charged + discharged) / 2 / capacity) : 0;

  // SVG dimensions
  const svgW = 320;
  const svgH = 110;
  const barW = history.length > 0 ? svgW / history.length : 3;
  const maxAbsPower = Math.max(1, ...history.map(p => Math.abs(p.powerKw)));

  // Build SoC path
  const socPoints = history.map((p, i) => {
    const x = (i / Math.max(1, history.length - 1)) * svgW;
    const y = svgH - 5 - (p.socPct / 100) * (svgH - 10);
    return { x, y };
  });
  const socLine = socPoints.map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');
  const socArea = socLine
    ? `${socLine} L${svgW},${svgH - 5} L0,${svgH - 5} Z`
    : '';

  // Playhead position
  const playheadX = history.length > 1 ? (playheadIdx / (history.length - 1)) * svgW : 0;
  const playheadY = socPoints[playheadIdx]?.y ?? svgH / 2;

  // Time axis ticks
  const ticks = history.length >= 4
    ? [0, Math.floor(history.length * 0.25), Math.floor(history.length * 0.5), Math.floor(history.length * 0.75), history.length - 1]
    : history.map((_, i) => i);

  if (loading) {
    return (
      <div className="bg-white rounded-[14px] p-5 border border-[rgba(88,70,180,0.14)] flex items-center justify-center min-h-[120px]" data-testid="replay-card">
        <div className="flex items-center gap-3">
          <div className="w-5 h-5 border-2 border-[#7B5CF6] border-t-transparent rounded-full animate-spin" />
          <span className="text-[#8C8AA8] text-sm">Loading history…</span>
        </div>
      </div>
    );
  }

  if (history.length === 0) {
    return (
      <div className="bg-white rounded-[14px] p-5 border border-[rgba(88,70,180,0.14)] flex items-center justify-center min-h-[120px]" data-testid="replay-card">
        <p className="text-[#8C8AA8] text-sm">No history data available</p>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-[14px] p-5 border border-[rgba(88,70,180,0.14)]" data-testid="replay-card" data-asset={selectedAssetCode}>
      {/* Header */}
      <div className="flex items-start justify-between mb-3 gap-2">
        <div>
          <p className="text-[15px] font-medium m-0">Last 24 hours — replay</p>
          <p className="text-xs text-[#8C8AA8] mt-[3px] m-0">Selected battery · Watch the day unfold</p>
        </div>
        <span
          className={`inline-flex items-center gap-1.5 px-[10px] py-[3px] rounded-full text-[11px] font-medium whitespace-nowrap ${pillStyle.bg} ${pillStyle.text}`}
          data-testid="replay-state-pill"
        >
          <span className={`w-[7px] h-[7px] rounded-full ${pillStyle.dot}`} />
          {mode}
        </span>
      </div>

      {/* Selected battery description */}
      {asset && (
        <>
          <div className="flex items-center gap-2 mb-2">
            <span className="inline-block px-2 py-0.5 rounded text-[10px] font-medium bg-[#F0ECFE] text-[#4A30B5]">Selected</span>
            <span className="text-[13px] font-medium text-[#261761]" data-testid="replay-selected-name">{asset.code}</span>
            <span className="text-[11px] text-[#5C5A7A]" data-testid="replay-selected-site">{asset.siteName} · {asset.location}</span>
          </div>
          <div className="flex items-center gap-4 flex-wrap text-[11px] text-[#5C5A7A] mb-3 px-1" data-testid="replay-specs-row">
            <span>
              <span className="inline-block px-[7px] py-[1px] rounded bg-[#F0ECFE] text-[#4A30B5] font-medium text-[10px] tracking-[0.03em]">{asset.chemistry}</span>
            </span>
            <span>Power rating <strong className="text-[#261761] font-medium">{asset.powerRatingKw} kW</strong></span>
            <span>Capacity <strong className="text-[#261761] font-medium">{asset.capacityKwh.toLocaleString()} kWh</strong></span>
            <span>Duration <strong className="text-[#261761] font-medium">{asset.durationH.toFixed(1)} h</strong></span>
          </div>
        </>
      )}

      {/* Readout */}
      <div className="flex items-baseline gap-3.5 mb-2.5 flex-wrap" data-testid="replay-readout">
        <span className="text-2xl font-medium text-[#261761] tabular-nums" data-testid="replay-time">
          {current ? formatTime(current.timestamp) : '--:--'}
        </span>
        <span className="text-[13px] text-[#4A30B5]">
          SoC <strong data-testid="replay-soc">{current ? `${Math.round(current.socPct)}%` : '—'}</strong> · Power <strong data-testid="replay-power">{current ? `${current.powerKw.toFixed(1)} kW` : '—'}</strong>
        </span>
      </div>

      {/* Chart */}
      <div className="relative w-full h-[130px] mt-1" data-testid="replay-chart">
        <svg viewBox={`0 0 ${svgW} ${svgH}`} preserveAspectRatio="none" className="block w-full h-full">
          <defs>
            <linearGradient id="socGrad" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#7B5CF6" stopOpacity="0.22" />
              <stop offset="100%" stopColor="#7B5CF6" stopOpacity="0" />
            </linearGradient>
          </defs>
          <line x1="0" y1={svgH / 2} x2={svgW} y2={svgH / 2} stroke="#DCD3FB" strokeWidth="0.5" />
          {/* Power bars */}
          <g data-testid="power-bars">
            {history.map((p, i) => {
              const barH = (Math.abs(p.powerKw) / maxAbsPower) * (svgH / 2 - 5);
              const x = i * barW;
              const y = p.powerKw >= 0 ? svgH / 2 - barH : svgH / 2;
              const opacity = i <= playheadIdx ? 1 : 0.25;
              return (
                <rect
                  key={i}
                  x={x}
                  y={y}
                  width={Math.max(barW - 0.5, 1)}
                  height={barH}
                  fill={getBarColour(p.powerKw)}
                  opacity={opacity}
                  data-testid={`power-bar-${i}`}
                  data-mode={getMode(p.powerKw).toLowerCase()}
                />
              );
            })}
          </g>
          {/* SoC area + line */}
          {socArea && <path d={socArea} fill="url(#socGrad)" data-testid="soc-area" />}
          {socLine && <path d={socLine} fill="none" stroke="#7B5CF6" strokeWidth="1.6" strokeLinejoin="round" data-testid="soc-line" />}
          {/* Playhead */}
          <line x1={playheadX} y1={5} x2={playheadX} y2={svgH - 5} stroke="#F05B8A" strokeWidth="1.2" opacity="0.85" data-testid="playhead-line" />
          <circle cx={playheadX} cy={playheadY} r="3.5" fill="#F05B8A" stroke="white" strokeWidth="1.5" data-testid="playhead-dot" />
        </svg>
      </div>

      {/* Time axis */}
      <div className="flex justify-between text-[11px] text-[#8C8AA8] px-0 mt-1" data-testid="time-axis">
        {ticks.map((idx) => (
          <span key={idx}>{history[idx] ? formatTime(history[idx].timestamp) : ''}</span>
        ))}
      </div>

      {/* Play row */}
      <div className="flex items-center gap-3 mt-3.5" data-testid="play-row">
        <button
          className="w-9 h-9 rounded-full border-none bg-[#7B5CF6] text-white cursor-pointer flex items-center justify-center shrink-0 p-0 transition-colors duration-150 hover:bg-[#5B3EC4]"
          onClick={togglePlay}
          aria-label={playing ? 'Pause' : 'Play'}
          data-testid="play-btn"
        >
          {playing ? (
            <svg width="14" height="14" viewBox="0 0 12 12" fill="white">
              <rect x="2" y="2" width="3" height="8" rx="0.5" />
              <rect x="7" y="2" width="3" height="8" rx="0.5" />
            </svg>
          ) : (
            <svg width="14" height="14" viewBox="0 0 12 12" fill="white">
              <path d="M3 2 L10 6 L3 10 Z" />
            </svg>
          )}
        </button>
        <div
          ref={scrubRef}
          className="flex-1 h-1.5 bg-[#F0ECFE] rounded-full relative overflow-hidden cursor-pointer"
          onClick={handleScrubClick}
          data-testid="scrub-bar"
        >
          <div
            className="absolute inset-y-0 left-0 rounded-full"
            style={{
              width: `${history.length > 1 ? (playheadIdx / (history.length - 1)) * 100 : 0}%`,
              background: 'linear-gradient(90deg, #17B890, #7B5CF6, #FF7A3D)',
              transition: 'width 60ms linear',
            }}
            data-testid="scrub-fill"
          />
        </div>
      </div>

      {/* Summary chips */}
      <div className="grid grid-cols-3 gap-[10px] mt-4" data-testid="summary-chips">
        <div className="bg-[#DDF7EE] rounded-[10px] px-3 py-2.5" data-testid="chip-charged">
          <p className="text-[11px] text-[#0B7757] m-0 mb-[3px]">Charged</p>
          <p className="text-[15px] font-medium m-0 text-[#053E2D]" data-testid="charged-value">{Math.round(charged)} kWh</p>
        </div>
        <div className="bg-[#FFE8DE] rounded-[10px] px-3 py-2.5" data-testid="chip-discharged">
          <p className="text-[11px] text-[#B8461A] m-0 mb-[3px]">Discharged</p>
          <p className="text-[15px] font-medium m-0 text-[#5C210A]" data-testid="discharged-value">{Math.round(discharged)} kWh</p>
        </div>
        <div className="bg-[#F0ECFE] rounded-[10px] px-3 py-2.5" data-testid="chip-cycles">
          <p className="text-[11px] text-[#4A30B5] m-0 mb-[3px]">Net cycles</p>
          <p className="text-[15px] font-medium m-0 text-[#261761]" data-testid="cycles-value">{netCycles.toFixed(1)}×</p>
        </div>
      </div>
    </div>
  );
}

import { useState, useEffect } from 'react';
import type { BatteryAction } from '../services/recommendationService';

interface PerBatteryStripProps {
  batteryActions: BatteryAction[];
  selectedAssetId: string;
  onSelectAsset: (assetCode: string) => void;
}

export function PerBatteryStrip({ batteryActions, selectedAssetId, onSelectAsset }: PerBatteryStripProps) {
  const [offset, setOffset] = useState(0);
  const [visibleCount, setVisibleCount] = useState(5);

  useEffect(() => {
    function handleResize() {
      const w = window.innerWidth;
      if (w <= 560) setVisibleCount(3);
      else if (w <= 780) setVisibleCount(4);
      else setVisibleCount(5);
    }
    handleResize();
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  useEffect(() => {
    if (offset > Math.max(0, batteryActions.length - visibleCount)) {
      setOffset(Math.max(0, batteryActions.length - visibleCount));
    }
  }, [visibleCount, batteryActions.length, offset]);

  const maxOffset = Math.max(0, batteryActions.length - visibleCount);
  const first = offset + 1;
  const last = Math.min(offset + visibleCount, batteryActions.length);

  function tileClasses(action: BatteryAction) {
    const isSelected = action.batteryCode === selectedAssetId;
    const base = 'flex-none text-center cursor-pointer transition-all duration-150 rounded-[10px]';
    const sizing = isSelected
      ? 'p-[7px] border-2 border-[#7B5CF6]'
      : 'p-2 border border-[rgba(88,70,180,0.14)]';

    let colorClass = 'border-[rgba(88,70,180,0.18)]';
    if (action.action === 'Charge') colorClass = 'border-[rgba(23,184,144,0.35)] bg-[rgba(221,247,238,0.5)]';
    else if (action.action === 'Discharge') colorClass = 'border-[rgba(255,122,61,0.35)] bg-[rgba(255,232,222,0.5)]';

    if (isSelected) colorClass = '';

    return `${base} ${sizing} ${colorClass}`;
  }

  function verbClasses(action: string) {
    if (action === 'Charge') return 'text-xs font-medium text-[#0B7757]';
    if (action === 'Discharge') return 'text-xs font-medium text-[#B8461A]';
    return 'text-xs font-medium text-[#5C5A7A]';
  }

  const tileWidth = `calc((100% - ${(visibleCount - 1) * 8}px) / ${visibleCount})`;
  const translatePct = (offset / visibleCount) * 100;

  return (
    <div className="relative mt-3.5" data-testid="per-battery-strip">
      <div className="flex items-center justify-between mb-2">
        <p className="text-[11px] text-[#4A30B5] font-medium uppercase tracking-wider m-0">
          Per-battery actions
        </p>
        <div className="flex items-center gap-1.5">
          <button
            className="w-[26px] h-[26px] rounded-full border border-[rgba(123,92,246,0.3)] bg-white text-[#4A30B5] flex items-center justify-center p-0 cursor-pointer transition-colors hover:bg-[#7B5CF6] hover:text-white hover:border-[#7B5CF6] disabled:opacity-35 disabled:cursor-not-allowed"
            aria-label="Previous"
            disabled={offset === 0}
            onClick={() => setOffset(Math.max(0, offset - visibleCount))}
          >
            <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
              <path d="M7.5 2.5 L4 6 L7.5 9.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
          </button>
          <span className="text-[11px] text-[#4A30B5] tabular-nums min-w-[44px] text-center" data-testid="pb-counter">
            {first}–{last} / {batteryActions.length}
          </span>
          <button
            className="w-[26px] h-[26px] rounded-full border border-[rgba(123,92,246,0.3)] bg-white text-[#4A30B5] flex items-center justify-center p-0 cursor-pointer transition-colors hover:bg-[#7B5CF6] hover:text-white hover:border-[#7B5CF6] disabled:opacity-35 disabled:cursor-not-allowed"
            aria-label="Next"
            disabled={offset >= maxOffset}
            onClick={() => setOffset(Math.min(maxOffset, offset + visibleCount))}
          >
            <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
              <path d="M4.5 2.5 L8 6 L4.5 9.5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
          </button>
        </div>
      </div>

      <div className="overflow-hidden relative mx-[-2px]">
        <div
          className="flex gap-2 p-[2px] transition-transform duration-300 ease-out"
          style={{ transform: `translateX(-${translatePct}%)` }}
          data-testid="pb-track"
        >
          {batteryActions.map((ba) => (
            <div
              key={ba.batteryCode}
              className={tileClasses(ba)}
              style={{ width: tileWidth, flexShrink: 0 }}
              data-testid={`pb-tile-${ba.batteryCode}`}
              data-action={ba.action.toLowerCase()}
              onClick={() => onSelectAsset(ba.batteryCode)}
            >
              <p className="text-[10px] text-[#8C8AA8] font-medium m-0 mb-0.5 whitespace-nowrap overflow-hidden text-ellipsis">
                {ba.batteryCode}
              </p>
              <p className={`${verbClasses(ba.action)} m-0 whitespace-nowrap`}>
                {ba.action}
              </p>
              <p className="text-[10px] text-[#8C8AA8] m-0 mt-0.5 whitespace-nowrap">
                {ba.action === 'Hold' ? '—' : `${ba.windowStart} – ${ba.windowEnd}`}
              </p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

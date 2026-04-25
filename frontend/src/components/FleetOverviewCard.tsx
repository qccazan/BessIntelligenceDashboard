import { useState, useEffect, useCallback } from 'react';
import type { FleetAsset, FleetSummary } from '../services/batteryService';
import { getFleetSummary } from '../services/batteryService';

interface FleetOverviewCardProps {
  selectedAssetId: string;
  onSelectAsset: (assetId: string) => void;
}

const STATE_COLOURS: Record<string, { icon: string; badge: string; badgeText: string; dot: string; barFill: string }> = {
  charging: {
    icon: 'bg-[#17B890]',
    badge: 'bg-[#DDF7EE]',
    badgeText: 'text-[#0B7757]',
    dot: 'bg-[#17B890]',
    barFill: 'bg-[#17B890]',
  },
  discharging: {
    icon: 'bg-[#FF7A3D]',
    badge: 'bg-[#FFE8DE]',
    badgeText: 'text-[#B8461A]',
    dot: 'bg-[#FF7A3D]',
    barFill: 'bg-[#FF7A3D]',
  },
  idle: {
    icon: 'bg-[#7B5CF6]',
    badge: 'bg-[#E8E6F2]',
    badgeText: 'text-[#5C5A7A]',
    dot: 'bg-[#8C8AA8]',
    barFill: 'bg-[#7B5CF6]',
  },
  fault: {
    icon: 'bg-[#F05B8A]',
    badge: 'bg-[#FFE4EE]',
    badgeText: 'text-[#A82155]',
    dot: 'bg-[#F05B8A]',
    barFill: 'bg-[#F05B8A]',
  },
};

function capitalize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}

export function FleetOverviewCard({ selectedAssetId, onSelectAsset }: FleetOverviewCardProps) {
  const [fleet, setFleet] = useState<FleetSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(5);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    getFleetSummary()
      .then((data) => {
        if (!cancelled) {
          setFleet(data);
          setError(null);
        }
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => { cancelled = true; };
  }, []);

  // Auto-navigate to the page containing the selected asset
  const navigateToAsset = useCallback((assetId: string, assets: FleetAsset[], size: number) => {
    const idx = assets.findIndex(a => a.code === assetId);
    if (idx >= 0) {
      const targetPage = Math.floor(idx / size) + 1;
      setCurrentPage(targetPage);
    }
  }, []);

  useEffect(() => {
    if (fleet) {
      navigateToAsset(selectedAssetId, fleet.assets, pageSize);
    }
  }, [selectedAssetId, fleet, pageSize, navigateToAsset]);

  if (loading) {
    return (
      <div className="bg-white rounded-[14px] p-5 border border-[rgba(88,70,180,0.14)] col-span-full flex items-center justify-center min-h-[120px]" data-testid="fleet-overview">
        <div className="flex items-center gap-3">
          <div className="w-5 h-5 border-2 border-[#7B5CF6] border-t-transparent rounded-full animate-spin" />
          <span className="text-[#8C8AA8] text-sm">Loading fleet data…</span>
        </div>
      </div>
    );
  }

  if (error || !fleet) {
    return (
      <div className="bg-white rounded-[14px] p-5 border border-[rgba(88,70,180,0.14)] col-span-full flex items-center justify-center min-h-[120px]" data-testid="fleet-overview">
        <p className="text-red-500 text-sm">{error ?? 'Failed to load fleet data'}</p>
      </div>
    );
  }

  const { assets } = fleet;
  const totalPages = Math.max(1, Math.ceil(assets.length / pageSize));
  const safePage = Math.min(currentPage, totalPages);
  const startIdx = (safePage - 1) * pageSize;
  const endIdx = Math.min(startIdx + pageSize, assets.length);
  const pageAssets = assets.slice(startIdx, endIdx);

  const pageNumbers = Array.from({ length: totalPages }, (_, i) => i + 1);

  return (
    <div className="bg-white rounded-[14px] p-5 border border-[rgba(88,70,180,0.14)] col-span-full" data-testid="fleet-overview">
      {/* Header */}
      <div className="flex items-start justify-between mb-4 gap-2">
        <div>
          <p className="text-[15px] font-medium m-0">Fleet overview</p>
          <p className="text-xs text-[#8C8AA8] mt-[3px] m-0">{assets.length} assets · click a row to drill in</p>
        </div>
        <span className="text-[11px] px-[11px] py-[5px] rounded-full font-medium bg-[#F0ECFE] text-[#4A30B5] whitespace-nowrap">
          {assets.length} assets · live
        </span>
      </div>

      {/* Table */}
      <div className="border border-[rgba(88,70,180,0.14)] rounded-[10px] overflow-x-auto">
        <table className="w-full border-collapse text-[13px] tabular-nums" data-testid="fleet-table">
          <thead>
            <tr>
              <th className="text-left text-[10px] font-medium uppercase tracking-[0.05em] text-[#5C5A7A] px-[14px] py-[10px] bg-[#F0ECFE] border-b border-[rgba(88,70,180,0.14)] whitespace-nowrap">Asset</th>
              <th className="text-left text-[10px] font-medium uppercase tracking-[0.05em] text-[#5C5A7A] px-[14px] py-[10px] bg-[#F0ECFE] border-b border-[rgba(88,70,180,0.14)] whitespace-nowrap">State</th>
              <th className="text-right text-[10px] font-medium uppercase tracking-[0.05em] text-[#5C5A7A] px-[14px] py-[10px] bg-[#F0ECFE] border-b border-[rgba(88,70,180,0.14)] whitespace-nowrap">Capacity</th>
              <th className="text-right text-[10px] font-medium uppercase tracking-[0.05em] text-[#5C5A7A] px-[14px] py-[10px] bg-[#F0ECFE] border-b border-[rgba(88,70,180,0.14)] whitespace-nowrap">State of charge</th>
              <th className="text-right text-[10px] font-medium uppercase tracking-[0.05em] text-[#5C5A7A] px-[14px] py-[10px] bg-[#F0ECFE] border-b border-[rgba(88,70,180,0.14)] whitespace-nowrap max-[899px]:hidden" data-column="soh">SoH</th>
              <th className="text-right text-[10px] font-medium uppercase tracking-[0.05em] text-[#5C5A7A] px-[14px] py-[10px] bg-[#F0ECFE] border-b border-[rgba(88,70,180,0.14)] whitespace-nowrap max-[899px]:hidden" data-column="temp">Temp</th>
            </tr>
          </thead>
          <tbody data-testid="fleet-rows">
            {pageAssets.map((asset) => {
              const isSelected = asset.code === selectedAssetId;
              const colours = STATE_COLOURS[asset.mode] ?? STATE_COLOURS.idle;
              const num = asset.code.split('-')[1];
              const isFault = asset.mode === 'fault';
              const tempWarn = asset.temperatureC >= 28;

              return (
                <tr
                  key={asset.id}
                  className={`cursor-pointer transition-colors duration-[120ms] ${isSelected ? 'bg-[#F0ECFE] shadow-[inset_3px_0_0_#7B5CF6]' : 'hover:bg-[#FAF9FE]'}`}
                  data-testid={`fleet-row-${asset.code}`}
                  data-mode={asset.mode}
                  data-selected={isSelected ? 'true' : undefined}
                  onClick={() => onSelectAsset(asset.code)}
                >
                  {/* Asset */}
                  <td className="px-[14px] py-3 border-b border-[rgba(88,70,180,0.14)] align-middle">
                    <div className="flex items-center gap-[10px]">
                      <div
                        className={`w-[30px] h-[30px] rounded-lg flex items-center justify-center text-[11px] font-medium text-white shrink-0 ${colours.icon} ${isFault ? 'animate-[faultPulse_1.5s_ease-in-out_infinite]' : ''}`}
                        data-testid={`asset-icon-${asset.code}`}
                      >
                        {num}
                      </div>
                      <div className="min-w-0">
                        <p className="text-[13px] font-medium text-[#261761] m-0 leading-[1.2]">{asset.code}</p>
                        <p className="text-[11px] text-[#5C5A7A] m-0 mt-[2px] leading-[1.2]">{asset.siteName} · {asset.location}</p>
                      </div>
                    </div>
                  </td>

                  {/* State */}
                  <td className="px-[14px] py-3 border-b border-[rgba(88,70,180,0.14)] align-middle">
                    <span className={`inline-flex items-center gap-1.5 px-[10px] py-[3px] rounded-full text-[11px] font-medium whitespace-nowrap ${colours.badge} ${colours.badgeText}`} data-testid={`state-badge-${asset.code}`}>
                      <span className={`w-[7px] h-[7px] rounded-full ${colours.dot}`} />
                      {capitalize(asset.mode)}
                    </span>
                  </td>

                  {/* Capacity */}
                  <td className="px-[14px] py-3 border-b border-[rgba(88,70,180,0.14)] align-middle text-right">
                    <span className="font-medium text-[#261761]" data-testid={`capacity-${asset.code}`}>{asset.capacityKwh.toLocaleString()} kWh</span>
                  </td>

                  {/* SoC */}
                  <td className="px-[14px] py-3 border-b border-[rgba(88,70,180,0.14)] align-middle text-right">
                    <div className="flex items-center gap-[10px] justify-end">
                      <div className="flex-[0_0_80px] h-1.5 bg-[#F0ECFE] rounded-full overflow-hidden relative">
                        <div
                          className={`absolute inset-y-0 left-0 rounded-full transition-[width] duration-500 ease-out ${colours.barFill}`}
                          style={{ width: `${Math.round(asset.socPct)}%` }}
                        />
                      </div>
                      <span className="font-medium text-[#261761] min-w-[36px] text-right" data-testid={`soc-${asset.code}`}>
                        {Math.round(asset.socPct)}%
                      </span>
                    </div>
                  </td>

                  {/* SoH */}
                  <td className="px-[14px] py-3 border-b border-[rgba(88,70,180,0.14)] align-middle text-right max-[899px]:hidden" data-column="soh">
                    <span className="font-medium text-[#1C1B2E]" data-testid={`soh-${asset.code}`}>{Math.round(asset.sohPct)}%</span>
                  </td>

                  {/* Temp */}
                  <td className="px-[14px] py-3 border-b border-[rgba(88,70,180,0.14)] align-middle text-right max-[899px]:hidden" data-column="temp">
                    <span className={`font-medium ${tempWarn ? 'text-[#B8461A]' : 'text-[#1C1B2E]'}`} data-testid={`temp-${asset.code}`}>
                      {asset.temperatureC.toFixed(1)}°C
                    </span>
                  </td>

                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      <div className="flex items-center justify-between mt-[14px] gap-3 flex-wrap" data-testid="fleet-pagination">
        <div className="text-xs text-[#5C5A7A]" data-testid="page-info">
          Showing <strong className="text-[#261761] font-medium">{startIdx + 1}–{endIdx}</strong> of <strong className="text-[#261761] font-medium">{assets.length}</strong> assets
        </div>

        <div className="flex items-center gap-2 text-xs text-[#5C5A7A]">
          <span>Show</span>
          <select
            className="bg-white border border-[rgba(88,70,180,0.22)] rounded-[10px] px-2 py-1 text-xs text-[#1C1B2E] cursor-pointer focus:outline-none focus:border-[#7B5CF6] focus:ring-2 focus:ring-[rgba(123,92,246,0.15)]"
            value={pageSize}
            data-testid="page-size-select"
            onChange={(e) => {
              const newSize = Number(e.target.value);
              setPageSize(newSize);
              setCurrentPage(1);
            }}
          >
            <option value={5}>5</option>
            <option value={10}>10</option>
            <option value={25}>25</option>
          </select>
          <span>per page</span>
        </div>

        <div className="flex items-center gap-1" data-testid="page-controls">
          <button
            className="min-w-[30px] h-[30px] px-2 border border-[rgba(88,70,180,0.22)] bg-white text-[#1C1B2E] rounded-[10px] text-xs cursor-pointer inline-flex items-center justify-center transition-colors duration-[120ms] hover:bg-[#F0ECFE] hover:border-[#7B5CF6] hover:text-[#4A30B5] disabled:opacity-40 disabled:cursor-not-allowed"
            disabled={safePage <= 1}
            data-testid="page-prev"
            onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
            aria-label="Previous page"
          >
            ‹
          </button>
          {pageNumbers.map((n) => (
            <button
              key={n}
              className={`min-w-[30px] h-[30px] px-2 border rounded-[10px] text-xs cursor-pointer inline-flex items-center justify-center transition-colors duration-[120ms] ${
                n === safePage
                  ? 'bg-[#7B5CF6] text-white border-[#7B5CF6]'
                  : 'bg-white text-[#1C1B2E] border-[rgba(88,70,180,0.22)] hover:bg-[#F0ECFE] hover:border-[#7B5CF6] hover:text-[#4A30B5]'
              }`}
              data-testid={`page-btn-${n}`}
              data-active={n === safePage ? 'true' : undefined}
              onClick={() => setCurrentPage(n)}
            >
              {n}
            </button>
          ))}
          <button
            className="min-w-[30px] h-[30px] px-2 border border-[rgba(88,70,180,0.22)] bg-white text-[#1C1B2E] rounded-[10px] text-xs cursor-pointer inline-flex items-center justify-center transition-colors duration-[120ms] hover:bg-[#F0ECFE] hover:border-[#7B5CF6] hover:text-[#4A30B5] disabled:opacity-40 disabled:cursor-not-allowed"
            disabled={safePage >= totalPages}
            data-testid="page-next"
            onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
            aria-label="Next page"
          >
            ›
          </button>
        </div>
      </div>

      {/* Legend */}
      <div className="flex justify-end gap-[14px] mt-[14px] text-[11px] text-[#5C5A7A] flex-wrap" data-testid="fleet-legend">
        <div className="flex items-center gap-[5px]"><span className="w-[10px] h-[10px] rounded-full bg-[#17B890]" />Charging</div>
        <div className="flex items-center gap-[5px]"><span className="w-[10px] h-[10px] rounded-full bg-[#FF7A3D]" />Discharging</div>
        <div className="flex items-center gap-[5px]"><span className="w-[10px] h-[10px] rounded-full bg-[#7B5CF6]" />Idle</div>
        <div className="flex items-center gap-[5px]"><span className="w-[10px] h-[10px] rounded-full bg-[#F05B8A]" />Fault</div>
      </div>
    </div>
  );
}

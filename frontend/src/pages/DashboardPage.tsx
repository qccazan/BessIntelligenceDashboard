import { useState, useEffect } from 'react';
import { MarketForecastCard } from '../components/MarketForecastCard';
import { FleetOverviewCard } from '../components/FleetOverviewCard';
import { CurrentStateCard } from '../components/CurrentStateCard';
import { ReplayCard } from '../components/ReplayCard';
import type { FleetSummary } from '../services/batteryService';
import { getFleetSummary } from '../services/batteryService';
import type { SolarSummary } from '../services/solarService';
import { getSolarSummary } from '../services/solarService';

interface DashboardPageProps {
  onLogout: () => void;
}

export function DashboardPage({ onLogout }: DashboardPageProps) {
  const [selectedAssetId, setSelectedAssetId] = useState('BESS-01');
  const [fleetSummary, setFleetSummary] = useState<FleetSummary | null>(null);
  const [solarSummary, setSolarSummary] = useState<SolarSummary | null>(null);

  useEffect(() => {
    getFleetSummary().then(setFleetSummary).catch(() => {});
    getSolarSummary().then(setSolarSummary).catch(() => {});
  }, []);

  const selectedAsset = fleetSummary?.assets.find(a => a.code === selectedAssetId);

  return (
    <div className="max-w-[1280px] mx-auto p-5" data-testid="dashboard">
      {/* Topbar */}
      <div
        className="flex items-center justify-between px-5 py-3.5 rounded-[14px] mb-4 border border-[rgba(88,70,180,0.14)]"
        style={{ background: 'linear-gradient(135deg, #F0ECFE 0%, #DFECFF 100%)' }}
      >
        <div className="flex items-center gap-2.5 font-medium text-[15px] text-[#261761]">
          <div className="w-8 h-8 rounded-[9px] bg-[#7B5CF6] flex items-center justify-center">
            <svg width="16" height="16" viewBox="0 0 14 14" fill="none">
              <path d="M5.5 1L2 7.5H5L4 12.5L9 6H6L7.5 1H5.5Z" fill="white"/>
            </svg>
          </div>
          <span>BESS Intelligence</span>
        </div>
        <div className="flex items-center gap-2.5 text-[13px] text-[#4A30B5]">
          <span>Admin</span>
          <div className="w-[30px] h-[30px] rounded-full bg-[#F05B8A] text-white flex items-center justify-center text-xs font-medium">
            AD
          </div>
          <button
            className="bg-transparent border border-[rgba(88,70,180,0.22)] text-[#4A30B5] text-xs px-3 py-1.5 rounded-[10px] cursor-pointer ml-1 hover:bg-white/60"
            onClick={onLogout}
          >
            Sign out
          </button>
        </div>
      </div>

      {/* Page header */}
      <div className="bg-white rounded-[14px] mb-4 border border-[rgba(88,70,180,0.14)] px-5 py-[18px]">
        <div className="flex justify-between items-center flex-wrap gap-4 mb-4">
          <div>
            <p className="text-xl font-medium m-0" data-testid="portfolio-heading">My portfolio</p>
            <p className="text-[13px] text-[#5C5A7A] m-0 mt-1 flex items-center gap-2">
              <span className="w-2 h-2 rounded-full bg-[#17B890] shadow-[0_0_0_3px_rgba(23,184,144,0.18)]"></span>
              {fleetSummary ? `${fleetSummary.assetCount} assets online` : '12 assets online'} · Last sync 12 seconds ago
            </p>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* Battery section */}
          <div data-testid="battery-section">
            <p className="text-[11px] font-medium uppercase tracking-wider text-[#4A30B5] m-0 mb-2 flex items-center gap-1.5">
              <svg width="14" height="14" viewBox="0 0 14 14" fill="none"><rect x="2" y="4" width="8" height="6" rx="1" stroke="#4A30B5" strokeWidth="1.2"/><rect x="10" y="5.5" width="2" height="3" rx="0.5" fill="#4A30B5"/></svg>
              Battery storage
            </p>
            <div className="flex gap-3 flex-wrap">
              <div className="bg-[#F0ECFE] px-3.5 py-2 rounded-[10px] text-right">
                <p className="text-[10px] text-[#4A30B5] font-medium uppercase tracking-wider m-0">Total capacity</p>
                <p className="text-base font-medium text-[#261761] mt-0.5 m-0" data-testid="total-capacity">{fleetSummary ? `${fleetSummary.totalCapacityMwh} MWh` : '—'}</p>
              </div>
              <div className="bg-[#DDF7EE] px-3.5 py-2 rounded-[10px] text-right">
                <p className="text-[10px] text-[#0B7757] font-medium uppercase tracking-wider m-0">Available now</p>
                <p className="text-base font-medium text-[#053E2D] mt-0.5 m-0" data-testid="available-now">{fleetSummary ? `${fleetSummary.availableNowMwh} MWh` : '—'}</p>
              </div>
              <div className="bg-[#FFE8DE] px-3.5 py-2 rounded-[10px] text-right">
                <p className="text-[10px] text-[#B8461A] font-medium uppercase tracking-wider m-0">Net power</p>
                <p className="text-base font-medium text-[#5C210A] mt-0.5 m-0" data-testid="net-power">{fleetSummary ? `${fleetSummary.netPowerMwh} MWh` : '—'}</p>
              </div>
            </div>
          </div>

          {/* Solar section */}
          <div data-testid="solar-section">
            <p className="text-[11px] font-medium uppercase tracking-wider text-[#B8860B] m-0 mb-2 flex items-center gap-1.5">
              <svg width="14" height="14" viewBox="0 0 14 14" fill="none"><circle cx="7" cy="7" r="3" stroke="#B8860B" strokeWidth="1.2"/><path d="M7 1v1.5M7 11.5V13M1 7h1.5M11.5 7H13M2.8 2.8l1.1 1.1M10.1 10.1l1.1 1.1M11.2 2.8l-1.1 1.1M3.9 10.1l-1.1 1.1" stroke="#B8860B" strokeWidth="1.1" strokeLinecap="round"/></svg>
              Solar panels
            </p>
            <div className="flex gap-3 flex-wrap">
              <div className="bg-[#FFF8E1] px-3.5 py-2 rounded-[10px] text-right">
                <p className="text-[10px] text-[#B8860B] font-medium uppercase tracking-wider m-0">Total capacity</p>
                <p className="text-base font-medium text-[#7A5800] mt-0.5 m-0" data-testid="solar-capacity">{solarSummary ? `${solarSummary.totalCapacityMwp} MWp` : '—'}</p>
              </div>
              <div className="bg-[#FFF8E1] px-3.5 py-2 rounded-[10px] text-right">
                <p className="text-[10px] text-[#B8860B] font-medium uppercase tracking-wider m-0">Panel count</p>
                <p className="text-base font-medium text-[#7A5800] mt-0.5 m-0" data-testid="solar-panel-count">{solarSummary ? solarSummary.totalPanelCount.toLocaleString() : '—'}</p>
              </div>
              <div className="bg-[#FFF8E1] px-3.5 py-2 rounded-[10px] text-right">
                <p className="text-[10px] text-[#B8860B] font-medium uppercase tracking-wider m-0">Yesterday production</p>
                <p className="text-base font-medium text-[#7A5800] mt-0.5 m-0" data-testid="solar-production">{solarSummary ? `${solarSummary.yesterdayProductionMwh} MWh` : '—'}</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Dashboard grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Fleet Overview (F-03) */}
        <FleetOverviewCard
          selectedAssetId={selectedAssetId}
          onSelectAsset={setSelectedAssetId}
        />

        {/* Market Forecast Card (F-07) */}
        <MarketForecastCard
          selectedAssetId={selectedAssetId}
          onSelectAsset={setSelectedAssetId}
        />

        {/* Current State Card (F-04) */}
        <CurrentStateCard asset={selectedAsset} />

        {/* Replay Card (F-05) */}
        <ReplayCard
          selectedAssetCode={selectedAssetId}
          capacity={selectedAsset?.capacityKwh ?? 1000}
          asset={selectedAsset}
        />
      </div>

      <p className="text-center text-[11px] text-[#8C8AA8] mt-5 mb-2.5 italic">
        BESS Intelligence Layer — PoC · Synthetic data · Read-only overlay
      </p>
    </div>
  );
}

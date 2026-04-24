import { useState } from 'react';
import { MarketForecastCard } from '../components/MarketForecastCard';

interface DashboardPageProps {
  onLogout: () => void;
}

export function DashboardPage({ onLogout }: DashboardPageProps) {
  const [selectedAssetId, setSelectedAssetId] = useState('BESS-01');

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
      <div className="bg-white rounded-[14px] mb-4 border border-[rgba(88,70,180,0.14)] px-5 py-[18px] flex justify-between items-center flex-wrap gap-4">
        <div>
          <p className="text-xl font-medium m-0">My battery portfolio</p>
          <p className="text-[13px] text-[#5C5A7A] m-0 mt-1 flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-[#17B890] shadow-[0_0_0_3px_rgba(23,184,144,0.18)]"></span>
            12 assets online · Last sync 12 seconds ago
          </p>
        </div>
        <div className="flex gap-3 flex-wrap">
          <div className="bg-[#F0ECFE] px-3.5 py-2 rounded-[10px] text-right">
            <p className="text-[10px] text-[#4A30B5] font-medium uppercase tracking-wider m-0">Total capacity</p>
            <p className="text-base font-medium text-[#261761] mt-0.5 m-0">11.2 MWh</p>
          </div>
          <div className="bg-[#DDF7EE] px-3.5 py-2 rounded-[10px] text-right">
            <p className="text-[10px] text-[#0B7757] font-medium uppercase tracking-wider m-0">Available now</p>
            <p className="text-base font-medium text-[#053E2D] mt-0.5 m-0">6.48 MWh</p>
          </div>
          <div className="bg-[#FFE8DE] px-3.5 py-2 rounded-[10px] text-right">
            <p className="text-[10px] text-[#B8461A] font-medium uppercase tracking-wider m-0">Net power</p>
            <p className="text-base font-medium text-[#5C210A] mt-0.5 m-0">−0.47 MWh</p>
          </div>
        </div>
      </div>

      {/* Dashboard grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Fleet Overview placeholder (F-03) */}
        <div className="bg-white rounded-[14px] p-5 border border-[rgba(88,70,180,0.14)] col-span-full flex items-center justify-center min-h-[120px]">
          <p className="text-[#8C8AA8] text-sm">Fleet overview — coming soon</p>
        </div>

        {/* Market Forecast Card (F-07) */}
        <MarketForecastCard
          selectedAssetId={selectedAssetId}
          onSelectAsset={setSelectedAssetId}
        />

        {/* Current State placeholder (F-04) */}
        <div className="bg-white rounded-[14px] p-5 border border-[rgba(88,70,180,0.14)] flex items-center justify-center min-h-[120px]">
          <p className="text-[#8C8AA8] text-sm">Current state — coming soon</p>
        </div>

        {/* Replay placeholder (F-05) */}
        <div className="bg-white rounded-[14px] p-5 border border-[rgba(88,70,180,0.14)] flex items-center justify-center min-h-[120px]">
          <p className="text-[#8C8AA8] text-sm">Last 24 hours replay — coming soon</p>
        </div>
      </div>

      <p className="text-center text-[11px] text-[#8C8AA8] mt-5 mb-2.5 italic">
        BESS Intelligence Layer — PoC · Synthetic data · Read-only overlay
      </p>
    </div>
  );
}

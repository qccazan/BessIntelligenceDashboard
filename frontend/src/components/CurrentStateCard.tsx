import type { FleetAsset } from '../services/batteryService';

const STATE_COLOURS: Record<string, { ring: string; pill: string; pillText: string; pillDot: string; powerClass: string }> = {
  charging: { ring: '#17B890', pill: 'bg-[#DDF7EE]', pillText: 'text-[#0B7757]', pillDot: 'bg-[#17B890]', powerClass: 'text-[#0B7757]' },
  discharging: { ring: '#FF7A3D', pill: 'bg-[#FFE8DE]', pillText: 'text-[#B8461A]', pillDot: 'bg-[#FF7A3D]', powerClass: 'text-[#B8461A]' },
  idle: { ring: '#7B5CF6', pill: 'bg-[#E8E6F2]', pillText: 'text-[#5C5A7A]', pillDot: 'bg-[#8C8AA8]', powerClass: 'text-[#5C5A7A]' },
  fault: { ring: '#F05B8A', pill: 'bg-[#FFE4EE]', pillText: 'text-[#A82155]', pillDot: 'bg-[#F05B8A]', powerClass: 'text-[#5C5A7A]' },
};

function capitalize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}

interface CurrentStateCardProps {
  asset: FleetAsset | undefined;
}

export function CurrentStateCard({ asset }: CurrentStateCardProps) {
  if (!asset) {
    return (
      <div className="bg-white rounded-[14px] p-5 border border-[rgba(88,70,180,0.14)] flex items-center justify-center min-h-[120px]" data-testid="current-state-card">
        <p className="text-[#8C8AA8] text-sm">No asset selected</p>
      </div>
    );
  }

  const colours = STATE_COLOURS[asset.mode] ?? STATE_COLOURS.idle;
  const socPct = Math.round(asset.socPct);
  const circumference = 2 * Math.PI * 38; // r=38
  const arcLength = (socPct / 100) * circumference;
  const gapLength = circumference - arcLength;
  const tempWarn = asset.temperatureC >= 28;

  const powerDirection = asset.mode === 'charging' ? 'charging' : asset.mode === 'discharging' ? 'discharging' : 'idle';
  const powerSign = asset.powerKw > 0 ? '+' : '';
  const powerDisplay = asset.mode === 'fault' ? 'offline' : asset.mode === 'idle' ? '0.0' : `${powerSign}${asset.powerKw.toFixed(1)}`;
  const powerLabel = asset.mode === 'fault' ? '' : `kW (${powerDirection})`;

  return (
    <div className="bg-white rounded-[14px] p-5 border border-[rgba(88,70,180,0.14)]" data-testid="current-state-card" data-asset={asset.code}>
      {/* Card header */}
      <div className="flex items-start justify-between mb-4 gap-2">
        <div>
          <p className="text-[15px] font-medium m-0">Current state — selected battery</p>
          <p className="text-xs text-[#8C8AA8] mt-[3px] m-0">Live · 5s refresh</p>
        </div>
        <span
          className={`inline-flex items-center gap-1.5 px-[10px] py-[3px] rounded-full text-[11px] font-medium whitespace-nowrap ${colours.pill} ${colours.pillText}`}
          data-testid="current-state-pill"
        >
          <span className={`w-[7px] h-[7px] rounded-full ${colours.pillDot}`} />
          {capitalize(asset.mode)}
        </span>
      </div>

      {/* Selected banner */}
      <div className="flex items-center gap-2 mb-3">
        <span className="inline-block px-2 py-0.5 rounded text-[10px] font-medium bg-[#F0ECFE] text-[#4A30B5]">Selected</span>
        <span className="text-[13px] font-medium text-[#261761]" data-testid="selected-name">{asset.code}</span>
        <span className="text-[11px] text-[#5C5A7A]" data-testid="selected-site">{asset.siteName} · {asset.location}</span>
      </div>

      {/* Specs row */}
      <div className="flex items-center gap-4 flex-wrap text-[11px] text-[#5C5A7A] mb-4 px-1" data-testid="specs-row">
        <span>
          <span className="inline-block px-[7px] py-[1px] rounded bg-[#F0ECFE] text-[#4A30B5] font-medium text-[10px] tracking-[0.03em]" data-testid="chemistry-badge">
            {asset.chemistry}
          </span>
        </span>
        <span>Power rating <strong className="text-[#261761] font-medium" data-testid="power-rating">{asset.powerRatingKw} kW</strong></span>
        <span>Capacity <strong className="text-[#261761] font-medium" data-testid="capacity">{asset.capacityKwh.toLocaleString()} kWh</strong></span>
        <span>Duration <strong className="text-[#261761] font-medium" data-testid="duration">{asset.durationH.toFixed(1)} h</strong></span>
      </div>

      {/* SoC row: ring gauge + power readout */}
      <div className="flex items-center gap-4 mb-4" data-testid="soc-row">
        <div className="relative w-[100px] h-[100px] shrink-0" data-testid="soc-ring-gauge">
          <svg width="100" height="100" viewBox="0 0 92 92">
            <circle cx="46" cy="46" r="38" fill="none" stroke="#F0ECFE" strokeWidth="9" />
            <circle
              cx="46" cy="46" r="38"
              fill="none"
              stroke={colours.ring}
              strokeWidth="9"
              strokeLinecap="round"
              strokeDasharray={`${arcLength.toFixed(1)} ${gapLength.toFixed(1)}`}
              transform="rotate(-90 46 46)"
              data-testid="soc-ring-arc"
            />
          </svg>
          <div className="absolute inset-0 flex flex-col items-center justify-center">
            <span className="text-2xl font-medium leading-none text-[#261761]" data-testid="soc-ring-value">{socPct}%</span>
            <span className="text-[11px] text-[#8C8AA8] mt-[3px]">SoC</span>
          </div>
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium m-0 mb-1.5 text-[#261761]" data-testid="soc-asset-label">{asset.code} · {asset.siteName}</p>
          <div>
            <span className={`text-[22px] font-medium ${colours.powerClass}`} data-testid="current-power-value">{powerDisplay}</span>
            {powerLabel && <span className="text-xs text-[#8C8AA8] ml-1" data-testid="current-power-label">{powerLabel}</span>}
          </div>
        </div>
      </div>

      {/* Metric tiles */}
      <div className="grid grid-cols-2 gap-[10px]" data-testid="metric-tiles">
        <div className="bg-[#DDF7EE] rounded-[10px] px-[14px] py-3" data-testid="soh-tile">
          <p className="text-[11px] text-[#0B7757] m-0 mb-[3px]">State of health</p>
          <p className="text-[22px] font-medium m-0 leading-[1.2] text-[#053E2D]" data-testid="soh-value">
            {Math.round(asset.sohPct)}<span className="text-[13px] opacity-70 ml-[3px] font-normal">%</span>
          </p>
        </div>
        <div className={`rounded-[10px] px-[14px] py-3 ${tempWarn ? 'bg-[#FFE8DE]' : 'bg-[#DDF7EE]'}`} data-testid="temp-tile" data-amber={tempWarn ? 'true' : undefined}>
          <p className={`text-[11px] m-0 mb-[3px] ${tempWarn ? 'text-[#B8461A]' : 'text-[#0B7757]'}`}>Temperature</p>
          <p className={`text-[22px] font-medium m-0 leading-[1.2] ${tempWarn ? 'text-[#5C210A]' : 'text-[#053E2D]'}`} data-testid="temp-value">
            {asset.temperatureC.toFixed(1)}<span className="text-[13px] opacity-70 ml-[3px] font-normal">°C</span>
          </p>
        </div>
      </div>
    </div>
  );
}

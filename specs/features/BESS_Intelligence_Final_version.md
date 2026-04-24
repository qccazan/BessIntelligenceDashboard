# BESS Intelligence Layer

Product Requirements & Specifications

> PoC build · Synthetic data · Read-only overlay on Withthegrid · Version 1.2 · 22 April 2026

| **Property** | **Description / Value** |
|---|---|
| **EPIC 1** | Mock Data Specification (D-01 – D-07, consistency rules) |
| **EPIC 2 · F-00** | Cross-Cutting Concerns ← global state, PoC data behaviour (implement first) |
| **EPIC 2 · F-01** | Login Page |
| **EPIC 2 · F-02** | Portfolio Header & KPI Summary |
| **EPIC 2 · F-03** | Fleet Overview Table |
| **EPIC 2 · F-04** | Current Battery State Card |
| **EPIC 2 · F-05** | Last 24 Hours Replay Card |
| **EPIC 2 · F-06** | AI Recommendation Engine ← forecast & dispatch optimisation logic |
| **EPIC 2 · F-07** | Market Forecast Card ← depends on F-06 engine output |

---

# EPIC 1 · Mock Data Specification

*Synthetic data · Read-only overlay on Withthegrid · PoC build*

## Historical Data Generation — REQ-HIST-01

**REQ-HIST-01 — Past-Year Data Generation**

For each dataset defined in this specification (D-01 through D-06), the system must generate synthetic data covering a continuous rolling window of the past 12 months (365 days), ending at the current date. This historical depth is required to support trend analysis, performance benchmarking, and advisory report generation within the Intelligence Layer.

| **Dataset** | **Granularity** | **12-Month Requirement** |
|---|---|---|
| D-01 Battery Master Data | Static (per asset) | Commissioning dates must fall within the past year where applicable; decommissioned assets should appear in historical records but be absent from current snapshots |
| D-02 Real-Time Telemetry | Latest snapshot (per asset) | One snapshot per hour for the past year must be available as an archive; the live snapshot represents the most recent reading |
| D-03a Battery History 24h | 15-minute intervals | 96 intervals per asset — consumed by F-05 (Replay Card). Rolling window ending at current time. |
| D-03b Battery History 7d | 15-minute intervals | 672 intervals per asset (96 × 7 days) — consumed by F-06 engine (DoD baseline, calendar aging). Same schema as D-03a, different file. |
| D-04 Day-Ahead Market Prices | Hourly (24 prices/day) | 24 prices/day × 365 days = 8,760 hourly price records covering the past year of EPEX SPOT NL data |
| D-05 AI Recommendation | One record per day | 365 daily recommendation snapshots per asset group, capturing charge/discharge windows and confidence scores over time |
| D-06 Weather Data | Hourly per site (24 h horizon) | 24 hourly readings per site × 12 sites = 288 records per engine run; aligned with the D-04 price horizon. Used by F-06 engine for solar irradiance and wind per hour. |
| D-07 Engine Config Constants | Static | Single config object with hardcoded PoC accuracy constants used by F-06 confidence scoring (price MAE, wind RMSE, cloud MAE, σ_SoH) |

> *All historical records must be internally consistent with the cross-dataset consistency rules. Time series data must use UTC-offset timestamps (Europe/Amsterdam, CET/CEST) and must not contain gaps, duplicate timestamps, or out-of-order entries.*

---

## D-01 · Battery Master Data

**Static registry of all BESS assets. One record per physical unit. Changes only when a new asset is commissioned or decommissioned. Does not include live telemetry.**
File: `battery_master.json`

| **Property** | **Description** | **Example** |
|---|---|---|
| **id** | Unique asset identifier | "BESS-01" |
| **site_name** | Human-readable site label | "Site A" |
| **location** | City where the asset is installed | "Amsterdam" |
| **country** | Country code | "NL" |
| **latitude / longitude** | GPS coordinates for map and weather lookup | 52.3676 / 4.9041 |
| **chemistry** | Battery cell chemistry | "LFP" |
| **power_rating_kw** | Maximum continuous charge/discharge power | 500 |
| **capacity_kwh** | Nameplate energy capacity | 1000 |
| **duration_h** | Discharge duration at rated power (capacity ÷ power) | 2.0 |
| **commissioned_date** | Date the asset was put into service | "2022-06-15" |
| **manufacturer** | System integrator or OEM name | "CATL" |
| **model** | Product model name | "EnerOne Plus" |
| **withthegrid_node_id** | Asset identifier in the Withthegrid platform | "a3f7c812-…" |

### All 12 Assets

| **ID** | **Site** | **Location** | **Power (kW)** | **Capacity (kWh)** |
|---|---|---|---|---|
| BESS-01 | Site A | Amsterdam | 500 | 1000 |
| BESS-02 | Site B | Rotterdam | 500 | 1000 |
| BESS-03 | Site C | Utrecht | 400 | 800 |
| BESS-04 | Site D | Eindhoven | 500 | 1000 |
| BESS-05 | Site E | Groningen | 500 | 1000 |
| BESS-06 | Site F | Tilburg | 500 | 1000 |
| BESS-07 | Site G | Almere | 400 | 800 |
| BESS-08 | Site H | Breda | 500 | 1000 |
| BESS-09 | Site I | Nijmegen | 400 | 800 |
| BESS-10 | Site J | Enschede | 500 | 1000 |
| BESS-11 | Site K | Haarlem | 500 | 1000 |
| BESS-12 | Site L | Arnhem | 400 | 800 |

---

## D-02 · Battery Real-Time Telemetry

**Live operational snapshot of each asset, polled at short intervals (e.g. every 5 seconds). Separate from master data.**
File: `battery_telemetry.json`

| **Property** | **Description** | **Example** |
|---|---|---|
| **id** | Asset identifier (foreign key to D-01) | "BESS-01" |
| **timestamp** | Time of the last telemetry reading | "2026-04-22T14:00:00+02:00" |
| **soc_pct** | State of charge | 55 |
| **soh_pct** | State of health — battery degradation indicator | 97 |
| **power_kw** | Instantaneous power; positive = charging, negative = discharging | -84.6 |
| **mode** | Operational mode: charging, discharging, idle, fault | "discharging" |
| **temperature_c** | Internal battery pack temperature | 17.2 |
| **voltage_v** | DC bus voltage | 748.4 |
| **current_a** | DC current; positive = charging | -113.1 |
| **next_action** | Next scheduled command: Charge, Discharge, Hold | "Charge" |
| **next_action_window** | Scheduled time for the next action; — if none | "03:00" |
| **fault_code** | Active fault code if mode is fault, otherwise null | null |

---

## D-03 · Battery Historical Time Series

**Per-asset time series of power and state of charge at 15-minute resolution.**
Two files with the same schema serve two different consumers: the Replay Card (F-05) needs last 24 hours; the AI Engine (F-06) needs last 7 days for DoD baseline and calendar aging calculations.

| **File** | **Depth** | **Records per asset** | **Consumer** |
|---|---|---|---|
| battery_history_24h.json | Last 24 hours | 96 intervals | F-05 · Last 24h Replay Card |
| battery_history_7d.json | Last 7 days | 672 intervals | F-06 · AI Recommendation Engine (Steps 2c and 3c) |

### Schema (identical for both files)

*Object keyed by asset ID; each value is an array of intervals in ascending timestamp order.*

| **Property** | **Description** | **Example** |
|---|---|---|
| **timestamp** | Start of the 15-minute interval (UTC+2, CET/CEST) | "2026-04-21T14:00:00+02:00" |
| **power_kw** | Average power during the interval; positive = charging, negative = discharging | -84.6 |
| **soc_pct** | State of charge at the end of the interval | 38.2 |

### Derived Fields (computed from series, not stored)

| **Property** | **Description / Value** |
|---|---|
| **Total charged (kWh)** | Sum of max(power_kw, 0) × 0.25 for all intervals |
| **Total discharged (kWh)** | Sum of max(−power_kw, 0) × 0.25 for all intervals |
| **Net cycles** | (charged + discharged) ÷ 2 ÷ capacity_kwh |
| **Average DoD over 7d (F-06 input)** | For each 96-interval day in battery_history_7d: compute (total charged / capacity_kwh) × 100; average across all 7 days per asset |
| **Time above 80% SoC in prior 24h (F-06 input)** | count(intervals in battery_history_24h where soc_pct > 80) / 96 — fraction of prior 24h spent above 80% SoC |

> *Consistency: the last soc_pct value in battery_history_24h must match D-02 soc_pct for the same asset. The last soc_pct in battery_history_7d must match the first value in battery_history_24h (they share the same 24h tail).*

---

## D-04 · Day-Ahead Market Prices

**Hourly electricity prices for the next 24 hours as published by EPEX SPOT for the Netherlands bidding zone.**
Primary input to the AI recommendation logic.
File: `market_forecast_prices.json`

| **Property** | **Description** | **Example** |
|---|---|---|
| **market** | Market and bidding zone name | "EPEX SPOT NL" |
| **currency** | Currency of prices | "EUR" |
| **generated_at** | Timestamp when the forecast was published | "2026-04-22T12:00:00+02:00" |
| **prices[].hour_start** | Start of the pricing hour | "2026-04-22T14:00:00+02:00" |
| **prices[].price_eur_mwh** | Day-ahead clearing price for this hour | 98.4 |

> *NL design note: The NL day-ahead market is heavily influenced by wind and solar, producing a more volatile price curve than most European markets. A realistic April weekday shape should include: an evening peak around 17:00–19:00 (~150–170 €/MWh), a deep overnight trough around 02:00–05:00 (~15–25 €/MWh) caused by high wind output, and a midday solar suppression dip around 11:00–14:00 (~55–80 €/MWh). The spread between overnight low and evening peak typically reaches 7–10× in spring.*

---

## D-05 · AI Recommendation

**A pre-computed recommendation object produced by the intelligence layer (F-06).**
Consumed directly by the Market Forecast card (F-07).
File: `ai_recommendation.json`

| **Property** | **Description** | **Example** |
|---|---|---|
| **generated_at** | When this recommendation was computed | "2026-04-22T14:02:00+02:00" |
| **portfolio_action** | High-level action label shown in the recommendation block | "Coordinated charge" |
| **charge_window_start / end** | Start and end of the optimal charge window | "03:00" / "06:00" |
| **charge_price_eur_mwh** | Expected average price during the charge window | 18.6 |
| **discharge_window_start / end** | Start and end of the optimal discharge window | "17:00" / "20:00" |
| **discharge_price_eur_mwh** | Expected average price during the discharge window | 164.2 |
| **price_spread_multiplier** | Discharge price divided by charge price | 8.8 |
| **avg_30d_spread_multiplier** | Trailing 30-day average spread multiplier — hardcoded to 6.1 for the PoC. Used by the explanation template ({avg_30d_spread}). | 6.1 |
| **confidence_pct** | Model confidence in the recommendation | 85 |
| **explanation** | Natural-language rationale referencing concrete numbers | see note |
| **estimated_capture_eur** | Expected revenue for the full coordinated cycle | 1840 |
| **per_battery_actions[].battery_id** | Asset identifier | "BESS-01" |
| **per_battery_actions[].action** | Charge, Discharge, or Hold | "Charge" |
| **per_battery_actions[].window_start** | Scheduled action start time; — for Hold | "03:00" |
| **per_battery_actions[].window_end** | Scheduled action end time; — for Hold. Required by F-07 tile display. Assets with a delayed start share the same window_end as the portfolio window. | "06:00" |
| **per_battery_actions[].reason** | Short reason if asset deviates from portfolio default | "Fault — held offline" |

---

## D-06 · Weather Data

**24-hour hourly weather forecast for each site, aligned with the D-04 price horizon.**
The F-06 engine reads per-hour GHI and wind speed to compute solar price adjustments (Step 1c) and classify the dispatch scenario (Step 2d).
File: `weather_forecast.json` (array of 288 records: 24 hours × 12 sites)

> *Critical: D-06 must be a time series, not a single snapshot. The engine computes avg_portfolio_ghi at hour h by averaging solar_irradiance_wm2 across all 12 sites for each of the 24 hours. A single-timestamp snapshot would make Steps 1c, 1d, and 2d unresolvable.*

| **Property** | **Description** | **Example** |
|---|---|---|
| **site_id** | Site identifier (matches site_name in D-01) | "Site A" |
| **location** | City name | "Amsterdam" |
| **hour_start** | Start of the 1-hour forecast interval — 24 entries per site, aligned with D-04 prices[].hour_start | "2026-04-22T14:00:00+02:00" |
| **ambient_temp_c** | Ambient air temperature | 11.8 |
| **humidity_pct** | Relative humidity | 74 |
| **wind_speed_ms** | Wind speed at 10 m height — primary input for wind score and dispatch scenario classification | 6.4 |
| **solar_irradiance_wm2** | Global horizontal irradiance (GHI) — primary input for solar price adjustment and duck window detection | 210 |
| **cloud_cover_pct** | Cloud cover fraction — used as proxy for solar forecast confidence in Step 6a | 60 |
| **condition** | Summary label: clear, partly_cloudy, overcast, rain, storm | "partly_cloudy" |

> *NL design notes: April temperatures 8–15 °C — thermal faults are unlikely (BESS-05 fault in Groningen should use CELL_IMBALANCE_MODULE_3). Coastal sites (Amsterdam, Haarlem) should have wind speeds 6–9 m/s; inland sites (Nijmegen, Enschede, Arnhem) 3–5 m/s. At least one site should model overnight wind speeds above 8 m/s to narratively justify the overnight price trough in D-04. Solar irradiance must be 0 for hours between 20:00 and 06:00 (night-time); peak GHI 09:00–15:00 should be 400–480 W/m² on a clear April day at 52°N.*

---

## D-07 · Engine Configuration Constants

**Hardcoded PoC constants that stand in for external accuracy metrics required by F-06 Step 6 (Confidence Scoring).**
In a production system these would be computed from trailing error logs against KNMI and EPEX actuals. For the PoC they are static values that produce the target confidence of 85%.
File: `engine_config.json`

> *Why this dataset is needed: Step 6a requires three accuracy inputs — price forecast MAE, wind forecast RMSE, and cloud cover forecast MAE — that come from external historical data sources not available in the PoC. Without hardcoded constants, the confidence formula is unresolvable and confidence_pct cannot be computed.*

| **Constant** | **Used in Step** | **Formula role** | **PoC value** | **Derivation** |
|---|---|---|---|---|
| price_MAE_pct | 6a — Price forecast accuracy | score = max(0, 100 − MAE_pct × 5) | 10.4 | Mid-range NL day-ahead MAE (8–12% band). Gives price_forecast_score = 48. |
| wind_RMSE_ms | 6a — Wind forecast confidence | score = max(0, 100 − RMSE_ms × 8) | 3.0 | Stable anticyclone scenario. Gives wind_forecast_score = 76. |
| cloud_MAE_pct | 6a — Solar forecast confidence | score = max(0, 100 − cloud_MAE_pct × 2) | 9.0 | Clear-sky day, tight forecast. Gives solar_forecast_score = 82. |
| sigma_soh_pct | 6a — Fleet SoH uniformity | score = max(0, 100 − σ_SoH × 10) | 0.3 | Tight fleet (all assets ~97% SoH). Gives fleet_soh_score = 97. |
| avg_30d_spread_multiplier | Step 7 — Explanation template | Used as {avg_30d_spread} in the explanation string | 6.1 | Typical NL spring 30-day average spread. Confirms current 8.8× is "well above" baseline. |

> *Verification: using the four accuracy constants above in Step 6b gives confidence_pct = round(0.40×48 + 0.25×76 + 0.20×82 + 0.15×97) = round(19.2 + 19.0 + 16.4 + 14.6) = round(69.2) = 69 for a Wind+Solar day. The mock dataset target of 85 assumes a particularly high-confidence anticyclone scenario — adjust price_MAE_pct to 6.0 and wind_RMSE_ms to 1.5 to reach 85: score = round(0.40×70 + 0.25×88 + 0.20×82 + 0.15×97) = round(28+22+16.4+14.6) = 81. For exactly 85, use price_MAE_pct=4.0, wind_RMSE_ms=1.5: score = round(80×0.40 + 88×0.25 + 82×0.20 + 97×0.15) = round(32+22+16.4+14.6) = 85. Use these adjusted constants in engine_config.json for the PoC target output.*

---

## Dataset Dependency Map

| **Dataset** | **Dependencies** |
|---|---|
| **D-01 Battery Master Data** | Referenced by D-02, D-03a/b, D-05, D-06 via battery_id / site_id |
| **D-02 Real-Time Telemetry** | Fleet Overview Table (F-03), Current State Card (F-04), Portfolio Header KPIs (F-02), F-06 engine (SoC, SoH, mode, temperature per asset) |
| **D-03a Battery History 24h** | F-05 · Last 24h Replay Card — power bars, SoC curve, energy summary chips |
| **D-03b Battery History 7d** | F-06 · AI Engine Steps 2c (DoD baseline) and 3c (calendar aging surcharge) |
| **D-04 Day-Ahead Market Prices** | F-07 · Market Forecast Chart; F-06 · Engine Step 1 (price signal processing) |
| **D-05 AI Recommendation** | F-07 · Recommendation block, per-battery strip, explanation text — produced by F-06, consumed by F-07 |
| **D-06 Weather Data (24h)** | F-06 · Engine Steps 1c (solar adjustment), 1d (duck window), 2d (dispatch scenario), 6a (solar forecast confidence) |
| **D-07 Engine Config Constants** | F-06 · Engine Step 6a only — hardcoded accuracy metrics for confidence scoring |

---

## Consistency Rules Across Datasets

These cross-dataset constraints must hold in the generated mock data or the dashboard will show contradictory values.

| **Rule** | **Description** |
|---|---|
| **SoC continuity** | D-03a last interval soc_pct must equal D-02 soc_pct for every asset. D-03b last interval soc_pct must equal D-03a first interval soc_pct (the files share a boundary). |
| **Mode / power sign** | D-02 mode = charging ⟹ power_kw > 0; discharging ⟹ power_kw < 0; idle or fault ⟹ power_kw ≈ 0 |
| **Fault assets held** | Any asset with D-02 mode = fault must have D-05 per_battery_actions action = Hold |
| **Recommendation aligns with prices** | D-05 charge_window must correspond to the lowest-price hour(s) in D-04; discharge_window to the highest |
| **Portfolio KPIs** | D-02 power_kw summed across all assets = Net Power chip; Σ(soc_pct × capacity_kwh / 100) = Available Now chip |
| **Per-battery action timing** | D-02 next_action and next_action_window must match the corresponding entry in D-05 per_battery_actions |
| **D-06 / D-04 time alignment** | D-06 hour_start values must exactly match D-04 prices[].hour_start for the same 24-hour horizon. The engine joins the two arrays by timestamp. |
| **D-06 night-time irradiance** | solar_irradiance_wm2 must be 0 for all D-06 records where hour_start falls between 20:00 and 06:00 local time. Non-zero GHI at night would corrupt the solar price adjustment in Step 1c. |

---

# EPIC 2 · Dashboard Implementation

*PoC build · Synthetic data · Read-only overlay on Withthegrid · Version 1.0 · 22 April 2026*

> *Feature order reflects implementation dependency. F-06 (AI Recommendation Engine) must be built before F-07 (Market Forecast Card), which consumes the engine's output.*

---

## F-00 · Cross-Cutting Concerns

*Global state · PoC data behaviour · Must be implemented before any other feature*

> *F-00 contains no UI of its own. It defines the shared state contract and PoC data behaviour that all other features depend on. Implement and validate these user stories first — every card in F-02 through F-07 either reads or writes the state defined here.*

### US-00-01 · Persistent Asset Selection Across All Cards

*As an operator navigating between assets, I want my current asset selection to be reflected consistently across every dashboard card simultaneously, so that I never see a state where different cards are showing data for different assets.*

#### Description

The dashboard maintains a single shared state variable, `selectedAssetId`, that represents the asset the operator is currently inspecting. Every component that renders per-asset data reads from this single source of truth. When the operator changes their selection — whether by clicking a row in the Fleet Overview Table (F-03) or a tile in the Market Forecast per-battery strip (F-07) — all consuming components update in the same render cycle. There is no scenario where F-04 and F-05 show data for different assets simultaneously.

#### State contract

| **Property** | **Type** | **Default on load** | **Written by** | **Read by** |
|---|---|---|---|---|
| selectedAssetId | string | "BESS-01" | F-03 row click, F-07 tile click | F-04 · Current State Card, F-05 · Replay Card, F-03 row highlight, F-07 tile highlight |

#### Acceptance Criteria

- **AC-1:** On initial dashboard load, selectedAssetId is set to "BESS-01" without any user interaction.
- **AC-2:** F-04 (Current State Card) renders BESS-01 data on load — it never shows an empty or placeholder state.
- **AC-3:** F-05 (Replay Card) renders BESS-01 data on load — it never shows an empty or placeholder state.
- **AC-4:** F-03 (Fleet Overview Table) highlights the BESS-01 row on load without requiring a click.
- **AC-5:** Clicking any row in F-03 updates selectedAssetId to the clicked asset's ID. F-04 and F-05 re-render with that asset's data in the same frame.
- **AC-6:** Clicking any tile in F-07's per-battery strip updates selectedAssetId to that asset's ID. F-04, F-05, and F-03 all update simultaneously.
- **AC-7:** At no point can F-04 and F-05 display data for different assets — they always reflect the same selectedAssetId.
- **AC-8:** Only one asset can be selected at any time. A new selection immediately replaces the previous one.

---

### US-00-02 · PoC Live-Feel Without Real Data Polling

*As an operator evaluating the PoC dashboard, I want the interface to feel live and operationally real — with a running sync timer and an accurate online asset count — without requiring a real-time data connection, so that the demo conveys production-like behaviour without implementation overhead.*

#### Description

The PoC loads all data from static JSON files once on page load. No polling, no WebSocket, no re-fetch. However, to reinforce the impression of a connected system, the "Last sync" counter in F-02 counts upward cosmetically using a client-side timer. The online asset count is derived directly from the static D-02 data at load time. Timestamps across all datasets use 2026-04-22 as the reference date and must be internally consistent.

#### PoC data behaviour reference

| **Property** | **Description / Value** |
|---|---|
| **Data loading** | All JSON files (D-02, D-03a, D-03b, D-04, D-05, D-06, D-07) are loaded once on dashboard initialisation. No re-fetch occurs during the session. |
| **"Last sync" counter** | Cosmetic only. Implemented as setInterval(1000) that increments a seconds counter from 0 on load. Renders as "Last sync {n} seconds ago". No data operation is triggered. Resets to 0 on page refresh. |
| **Online asset count** | Computed once at load: count(D-02 records where mode !== "fault"). BESS-05 is in fault, so the result is 11. The subtitle renders "11 assets online" — not 12. |
| **Reference date** | All PoC timestamps use 2026-04-22 as the current date. D-04 price horizon: 2026-04-22T14:00 to 2026-04-23T13:00. D-03a window: 2026-04-21T14:00 to 2026-04-22T14:00. D-06 weather horizon: same 24h as D-04. |

#### Acceptance Criteria

- **AC-1:** All JSON data files are loaded on dashboard initialisation; no fetch requests are made after the initial load.
- **AC-2:** The "Last sync" counter in F-02 begins at 0 seconds on load and increments by 1 every second using a client-side timer.
- **AC-3:** The counter display reads "Last sync {n} seconds ago" and updates every second without triggering any data fetch.
- **AC-4:** The online asset count in F-02 is computed as count(D-02 records where mode ≠ "fault") and renders as "11 assets online" for the PoC dataset.
- **AC-5:** No network request is visible in browser DevTools after the initial asset load (verifiable in the Network tab).
- **AC-6:** The D-04 price array and D-06 weather array share the same 24 hour_start timestamps, enabling the engine to join them by timestamp.
- **AC-7:** solar_irradiance_wm2 is 0 for all D-06 records where hour_start falls between 20:00 and 06:00 local time.

---

## F-01 · Login Page

### US-01-01 · Branded Login Screen with Demo Mode Indicator

*As an operator opening the application for the first time, I want to see a branded login screen with the product name, tagline, and a clear demo mode notice, so that I immediately understand what product I am using and do not mistake the PoC for a production system.*

#### Description

When a user navigates to the application URL, the first view they encounter is the login card. It displays the BESS Intelligence logo mark, the product name, and a short positioning tagline. A small, visually subdued footer note explicitly states this is a demo build with no real authentication.

#### Acceptance Criteria

- **AC-1:** The login screen is the first and only view visible on initial page load.
- **AC-2:** The product logo mark (lightning bolt icon) is rendered in the top-centre of the card.
- **AC-3:** The product name "BESS Intelligence" is displayed adjacent to the logo.
- **AC-4:** The tagline "AI-augmented battery intelligence, on top of your monitoring platform" is visible beneath the logo and name.
- **AC-5:** The card is centred on the viewport both horizontally and vertically on screen sizes ≥ 768 px wide.
- **AC-6:** The layout is not broken on laptop (≥ 1024 px) or tablet (≥ 768 px) viewports.
- **AC-7:** The text "Demo build — no authentication is performed" (or equivalent) is visible below the "Sign in" button.
- **AC-8:** The demo note is styled in a visually subdued way (smaller font, muted colour).
- **AC-9:** A "Forgot password?" link is visible below the "Sign in" button.
- **AC-10:** Clicking "Forgot password?" produces no navigation and no error.

---

### US-01-02 · Login Credentials and Sign-In Navigation

*As an operator on the login screen, I want to see clearly labelled, pre-filled input fields and be able to sign in by clicking the button or pressing Enter, so that I can proceed through the demo flow without friction.*

#### Description

The login card contains two labelled input fields (email and password). Both are pre-filled with valid-looking demo values. No network request is made and no validation is enforced.

#### Acceptance Criteria

- **AC-1:** An email input field with the label "Email" is present and visible.
- **AC-2:** A password input field with the label "Password" is present and its value is masked.
- **AC-3:** The email field is pre-filled with a placeholder demo value (e.g. alex@qubiz.com).
- **AC-4:** The password field is pre-filled with a placeholder demo value.
- **AC-5:** Clearing both fields and clicking "Sign in" still navigates to the dashboard — no validation error is shown.
- **AC-6:** Both fields accept keyboard input and update their displayed value accordingly.
- **AC-7:** A "Sign in" button is visible and styled as the primary action on the card.
- **AC-8:** Clicking "Sign in" navigates to the dashboard view within one second.
- **AC-9:** Pressing Enter while the email field is focused navigates to the dashboard.
- **AC-10:** Pressing Enter while the password field is focused navigates to the dashboard.
- **AC-11:** No network request is fired during or after the sign-in action (verifiable via browser DevTools).
- **AC-12:** The login screen is no longer visible after navigation; the dashboard occupies the full viewport.

---

## F-02 · Portfolio Header & KPI Summary

### US-02-01 · Total Capacity Display

*As an operator who has just signed in, I want to see the total installed capacity of my battery fleet in MWh, so that I immediately understand the scale of assets under management.*

#### Description

The portfolio header displays a "Total Capacity" chip showing the sum of nameplate energy capacity across all 12 BESS assets. This value is fixed for the PoC (11.2 MWh).

#### Acceptance Criteria

- **AC-1:** A "Total Capacity" chip is visible in the portfolio header on dashboard load.
- **AC-2:** The chip displays a numeric value with the unit MWh.
- **AC-3:** The value matches the sum of all asset capacities defined in the fleet data (11.2 MWh).
- **AC-4:** The chip is visible on viewports ≥ 768 px without truncation.

---

### US-02-02 · Available Energy Display

*As an operator planning the next dispatch decision, I want to see how much energy is immediately available across my fleet, so that I can judge whether the portfolio can respond to the AI recommendation.*

#### Acceptance Criteria

- **AC-1:** An "Available Now" chip is visible in the portfolio header on dashboard load.
- **AC-2:** The chip displays a numeric value with the unit MWh.
- **AC-3:** The value is arithmetically consistent with the sum of (SoC% × capacity) for all non-fault assets (within rounding to two decimal places).
- **AC-4:** The chip value is distinct from the Total Capacity chip value and is lower or equal to it.

---

### US-02-03 · Net Power Display

*As an operator monitoring real-time operations, I want to see the net real-time power flow of the entire portfolio in kW, so that I can confirm whether the fleet is net charging, net discharging, or balanced.*

#### Acceptance Criteria

- **AC-1:** A "Net Power" chip is visible in the portfolio header on dashboard load.
- **AC-2:** The chip displays a signed numeric value with the unit kW.
- **AC-3:** The value matches the algebraic sum of all asset power readings in the fleet data.
- **AC-4:** The chip value is negative when the majority of the fleet is discharging, consistent with the Fleet Overview table.

---

### US-02-04 · Live Sync Status Indicator

*As an operator relying on the dashboard for operational decisions, I want to see a live status indicator and last-sync timestamp in the header, so that I know the data I am viewing is current.*

#### Acceptance Criteria

- **AC-1:** A status dot is visible in the page subtitle area of the portfolio header.
- **AC-2:** The dot is rendered in a "live" colour (teal/green) indicating normal operation.
- **AC-3:** The subtitle text includes the number of online assets, computed as count(D-02 records where mode ≠ "fault"). With BESS-05 in fault, this renders as "11 assets online".
- **AC-4:** The subtitle text includes a cosmetic elapsed-time counter (e.g. "Last sync 12 seconds ago") that counts up from 0 on page load using a 1-second interval. No data fetch is performed.

---

## F-03 · Fleet Overview Table

### US-03-01 · View Asset List with Key Metrics

*As an operator managing a multi-site portfolio, I want to see all my battery assets listed in a single table with their core operational metrics, so that I can assess the health and status of the whole fleet at a glance.*

#### Acceptance Criteria

- **AC-1:** The table renders on page load with one row per asset (12 rows total across all pages).
- **AC-2:** Each row displays: Asset ID, site name, State badge, Power (kW), SoC (% + bar), SoH (%), Temperature (°C), and Next Action.
- **AC-3:** All fields contain populated, non-empty values for every asset.
- **AC-4:** Asset IDs are displayed in ascending order (BESS-01 through BESS-12).
- **AC-5:** The table has a visible header row with labelled column names.

---

### US-03-02 · Identify Operational State at a Glance

*As an operator scanning the fleet table, I want to instantly identify each asset's operational state through a colour-coded badge.*

#### Colour Scheme

| **State** | **Badge Colour** | **Description** |
|---|---|---|
| Charging | Teal | Asset is actively accepting energy from the grid |
| Discharging | Coral / Orange | Asset is delivering energy to the grid |
| Idle | Muted Purple | Asset is operational but not actively cycling |
| Fault | Pink / Red | Asset has an active fault and cannot be dispatched |

#### Acceptance Criteria

- **AC-1:** Each row displays exactly one State badge with one of four values: Charging, Discharging, Idle, or Fault.
- **AC-2:** Charging badges are rendered in teal; Discharging in coral; Idle in muted grey/purple; Fault in pink.
- **AC-3:** The asset icon (left of the asset name) uses the same colour as the State badge for that row.
- **AC-4:** A legend at the bottom of the Fleet Overview card maps each colour to its state label.
- **AC-5:** The State badge colour and the sign of the Power value are mutually consistent for every row.

---

### US-03-03 · Identify Fault Assets Immediately

*As an operator responsible for fleet reliability, I want fault-state assets to be visually distinguished with an animated alert, so that I notice a problem unit immediately.*

#### Acceptance Criteria

- **AC-1:** Fault-state assets render a pulsing/glowing animation on their row icon.
- **AC-2:** The Power cell for a fault asset displays "offline" or equivalent — not a kW value.
- **AC-3:** The Next Action cell for a fault asset displays "Hold" and "—" (no time window).
- **AC-4:** The State badge for a fault asset is pink and labelled "Fault".
- **AC-5:** The animation is continuous and does not stop after a set number of pulses.

---

### US-03-04 · Select an Asset to Drill Down

*As an operator who wants deeper insight into a specific battery, I want to click a table row to select that asset, so that the detail cards update to show data for the asset I selected.*

#### Acceptance Criteria

- **AC-1:** Clicking any table row applies a visible "selected" highlight to that row.
- **AC-2:** Only one row is highlighted as selected at any time; clicking a new row deselects the previous one.
- **AC-3:** After clicking a row, the Current State card (F-04) updates to display the selected asset's data.
- **AC-4:** After clicking a row, the 24-Hour Replay card (F-05) regenerates with the selected asset's data.
- **AC-5:** The cursor changes to a pointer when hovering over any table row.
- **AC-6:** A row-hover state (light background tint) is visible on mouse-over before clicking.
- **AC-7:** On initial dashboard load, BESS-01 is pre-selected and its row is highlighted without requiring a click.
- **AC-8:** When an asset is selected via the F-07 per-battery strip (external trigger), F-03 automatically navigates to the page containing that asset so the highlighted row is always visible.

---

### US-03-05 · Paginate Through the Fleet

*As an operator with more assets than fit on a single screen, I want to paginate through the fleet table and control how many rows are shown per page.*

#### Acceptance Criteria

- **AC-1:** By default, 5 rows are shown per page on initial load.
- **AC-2:** A page-size selector offers options: 5, 10, 25 rows per page.
- **AC-3:** Changing the page size updates the visible rows immediately without a page reload.
- **AC-4:** "Showing X–Y of Z assets" counter updates to reflect the current page and selected page size.
- **AC-5:** Previous and next arrow buttons are present; the previous button is disabled on page 1, the next on the last page.
- **AC-6:** Numbered page buttons are present; the active page button is visually distinct (filled/highlighted).
- **AC-7:** For more than 7 total pages, ellipsis (…) is used to truncate the page list; the first and last page numbers are always shown.
- **AC-8:** Navigating to a page that contains the selected asset preserves the row highlight.

---

### US-03-06 · Temperature Warning Highlight

*As an operator monitoring battery health, I want to see an amber highlight on temperature values that exceed a safe threshold.*

#### Acceptance Criteria

- **AC-1:** The Temperature column is present and displays a value in °C for every non-fault asset.
- **AC-2:** Temperature values ≥ 28 °C are rendered in amber/orange colour.
- **AC-3:** Temperature values < 28 °C are rendered in the default text colour.
- **AC-4:** The threshold boundary (28 °C) is consistently applied — an asset with exactly 28.0 °C triggers the amber style.

---

### US-03-07 · Responsive Table on Narrow Viewports

*As an operator accessing the dashboard on a tablet or small laptop, I want secondary table columns to hide automatically on narrow screens.*

#### Acceptance Criteria

- **AC-1:** On viewports ≥ 900 px wide, all seven columns are visible.
- **AC-2:** On viewports < 900 px wide, the SoH and Temperature columns are hidden.
- **AC-3:** On viewports < 900 px wide, the remaining five columns are fully legible with no overflow or horizontal scroll.
- **AC-4:** The table layout does not break and rows remain selectable on narrow viewports.

---

## F-04 · Current Battery State Card

### US-04-01 · State of Charge Ring Gauge

*As an operator assessing a specific battery's readiness, I want to see the selected battery's state of charge displayed as a visual ring gauge.*

#### Acceptance Criteria

- **AC-1:** An SVG ring gauge is rendered in the Current State card.
- **AC-2:** The ring arc length is proportional to the SoC percentage (100% SoC = full ring, 0% = empty ring).
- **AC-3:** The numeric SoC value (as a percentage) is displayed in the centre of the ring.
- **AC-4:** The displayed percentage matches the SoC value in the Fleet Overview table row for the same asset.
- **AC-5:** The gauge updates when a different asset is selected in the fleet table.

---

### US-04-02 · Operational State and Real-Time Power

*As an operator verifying that a battery is behaving as expected, I want to see the selected battery's current operational state and real-time power output.*

#### Acceptance Criteria

- **AC-1:** An operational state pill is displayed in the card header, with one of four values: Charging, Discharging, Idle, or Fault.
- **AC-2:** The state pill colour is consistent with the colour scheme used in the Fleet Overview table.
- **AC-3:** The real-time power value is displayed with a unit (kW) and a directional label.
- **AC-4:** Charging assets display a positive kW value; discharging assets display a negative kW value; idle assets display ~0 kW.
- **AC-5:** The state pill and power sign are mutually consistent for every selectable asset.

---

### US-04-03 · State of Health and Temperature Metrics

*As an operator monitoring long-term asset health, I want to see the selected battery's state of health and current temperature in clearly labelled metric tiles.*

#### Acceptance Criteria

- **AC-1:** A "State of Health" metric tile is present, displaying a percentage value with a label.
- **AC-2:** A "Temperature" metric tile is present, displaying a value in °C with a label.
- **AC-3:** Both values match those shown for the same asset in the Fleet Overview table.
- **AC-4:** The Temperature tile applies an amber visual treatment when the value is ≥ 28 °C.
- **AC-5:** Both metric tiles update when a different asset is selected.

---

### US-04-04 · Battery Specifications Row

*As an operator who needs to know the physical characteristics of the asset I am managing, I want to see the selected battery's chemistry, power rating, capacity, and duration.*

#### Acceptance Criteria

- **AC-1:** A chemistry badge (e.g. "LFP") is visible in the specifications row.
- **AC-2:** The power rating is displayed in kW and labelled "Power rating".
- **AC-3:** The total capacity is displayed in kWh and labelled "Capacity".
- **AC-4:** The discharge duration is displayed in hours and labelled "Duration", calculated as capacity ÷ power rating.
- **AC-5:** All four specification fields update when a different asset is selected in the fleet table.
- **AC-6:** The asset name, site, and location displayed in the banner match the selected row in the fleet table.

---

### US-04-05 · Asset Selection Updates the Card

*As an operator who switches between assets frequently, I want the Current State card to refresh instantly whenever I select a different asset.*

#### Acceptance Criteria

- **AC-1:** Clicking a different row in the fleet table updates all fields in the Current State card without a page reload.
- **AC-2:** Clicking a tile in the per-battery action strip (F-07) also updates the Current State card.
- **AC-3:** All fields (SoC, power, state pill, SoH, temperature, chemistry, power rating, capacity, duration) update simultaneously — no field retains stale data from the previous selection.
- **AC-4:** The asset name and site in the card banner match the newly selected row.
- **AC-5:** The transition is immediate (no visible loading delay for a data set of 12 assets).

---

## F-05 · Last 24 Hours Replay Card

### US-05-01 · Composite Power and SoC Chart

*As an operator auditing a battery's daily behaviour, I want to see a combined chart showing both the power profile and the state-of-charge curve over the last 24 hours.*

#### Description

The replay chart is an SVG composite: colour-coded vertical bars represent power at each 15-minute interval (teal = charging, coral = discharging, muted = idle), and a purple line-and-area overlay traces the SoC evolution across the same timeline. 96 data points are plotted (24 hours × 4 per hour).

#### Acceptance Criteria

- **AC-1:** The chart renders 96 power bars covering the 24-hour period.
- **AC-2:** Charging intervals are rendered in teal, discharging in coral, and idle/near-zero in muted purple.
- **AC-3:** A SoC area curve is overlaid on the power bars using the same time axis.
- **AC-4:** A time axis is visible with at least 4 labelled tick marks spanning the 24-hour range.
- **AC-5:** The chart renders without visible lag or artefacts on a typical laptop.
- **AC-6:** Charging and discharging intervals are visually distinguishable from each other and from idle periods.

---

### US-05-02 · Animated Replay with Play / Pause

*As an operator who wants to walk through a battery's day step by step, I want to play an animated replay of the 24-hour history.*

#### Description

A Play button starts an animation where a playhead (pink vertical line with a dot on the SoC curve) advances from left to right across the chart at a fixed speed (~100 ms per step). As the playhead moves, the current timestamp, SoC value, and power value update in a readout above the chart.

#### Acceptance Criteria

- **AC-1:** A Play button is visible below the chart.
- **AC-2:** Pressing Play starts the playhead animation from the current position; the playhead advances smoothly from left to right.
- **AC-3:** The timestamp readout above the chart updates at each step to reflect the current playhead position.
- **AC-4:** The SoC and Power values in the readout update at each step to match the data at the playhead position.
- **AC-5:** Power bars to the left of the playhead are rendered at full opacity; bars to the right are dimmed.
- **AC-6:** The Play button toggles to a Pause icon while the animation is running.
- **AC-7:** Pressing Pause halts the animation at the current position; the playhead and readout remain at that position.
- **AC-8:** When the playhead reaches the last data point, the animation stops automatically.

---

### US-05-03 · Scrub Bar for Manual Seeking

*As an operator who wants to inspect a specific moment in the battery's history, I want to click anywhere on a scrub bar to jump the playhead to that point.*

#### Acceptance Criteria

- **AC-1:** A scrub bar is visible below the chart.
- **AC-2:** The scrub bar displays a fill indicator that advances as the playhead progresses through the animation.
- **AC-3:** Clicking the leftmost point of the scrub bar moves the playhead to the start of the timeline (14:00).
- **AC-4:** Clicking the rightmost point moves the playhead to the end of the timeline.
- **AC-5:** Clicking any intermediate point on the scrub bar moves the playhead proportionally to the clicked position.
- **AC-6:** After seeking via the scrub bar, the timestamp, SoC, and Power readouts update immediately.
- **AC-7:** The play/pause state is preserved after seeking (if paused before the click, it remains paused).

---

### US-05-04 · Operational State Pill During Replay

*As an operator watching the replay, I want to see the battery's operational state label update dynamically as the playhead moves.*

#### Acceptance Criteria

- **AC-1:** An operational state pill is visible in the replay card header.
- **AC-2:** The pill displays "Charging" (teal) when the power at the playhead position is positive.
- **AC-3:** The pill displays "Discharging" (coral) when the power at the playhead position is negative.
- **AC-4:** The pill displays "Idle" (muted) when the power at the playhead position is near zero.
- **AC-5:** The pill updates immediately when the playhead moves — whether by animation or by scrubbing.

---

### US-05-05 · Daily Energy Summary Chips

*As an operator reviewing a battery's daily performance, I want to see a summary of total energy charged, discharged, and net cycle count for the day.*

#### Acceptance Criteria

- **AC-1:** Three summary chips are visible below the scrub bar: Charged (kWh), Discharged (kWh), Net Cycles (×).
- **AC-2:** All three chips display non-zero, non-empty values for every selectable asset.
- **AC-3:** The Charged value equals the sum of positive-power intervals × 0.25 h (15-minute resolution) in kWh.
- **AC-4:** The Discharged value equals the sum of absolute negative-power intervals × 0.25 h in kWh.
- **AC-5:** The Net Cycles value equals (Charged + Discharged) ÷ 2 ÷ asset capacity, rounded to one decimal place.
- **AC-6:** All three values update when a different asset is selected.

---

### US-05-06 · Replay Resets on Asset Switch

*As an operator who switches between assets to compare their daily behaviour, I want the replay chart and animation to reset automatically when I select a different asset.*

#### Acceptance Criteria

- **AC-1:** Selecting a different asset in the fleet table regenerates the power bars and SoC curve for that asset.
- **AC-2:** The playhead resets to the leftmost position (start of the timeline) after asset switch.
- **AC-3:** The animation starts playing automatically after the chart regenerates.
- **AC-4:** The timestamp readout resets to the start time (14:00) after asset switch.
- **AC-5:** The daily energy summary chips recalculate and display values for the newly selected asset.
- **AC-6:** The SoC value at the rightmost point of the replay (end of day) is consistent with the current SoC shown in the Current State card (F-04) for the same asset.

---

## F-06 · AI Recommendation Engine

*Forecast & Dispatch Optimisation Logic*

> *F-06 is a prerequisite for F-07 (Market Forecast Card). The engine produces the D-05 AI Recommendation object that drives all recommendation UI in F-07. Build and validate F-06 output before implementing F-07.*

### Overview

The recommendation engine runs once per day after the EPEX SPOT NL day-ahead results are published (typically 13:00 CET). It ingests three data streams — market prices, real-time battery state, and historical telemetry — and produces the D-05 AI Recommendation object that drives the Market Forecast card.

**Core principle:** Least-intrusive dispatch — the engine only recommends a charge/discharge cycle when the net revenue after accounting for battery wear exceeds a minimum profitability threshold. It never recommends a cycle that destroys more asset value than it captures.

| **Step** | **Name** | **What it does** |
|---|---|---|
| 1 | Price signal processing | Load 24 h EPEX SPOT NL prices, identify candidate charge and discharge windows |
| 2 | Battery state assessment | Read current SoC, SoH, and temperature per asset; compute available and dispatchable energy within safe SoC bounds |
| 3 | Degradation cost modelling | Calculate the € cost of one cycle for each asset, accounting for depth of discharge, C-rate stress, and temperature |
| 4 | Window optimisation | Score all valid (charge window, discharge window) combinations; select the pair that maximises net revenue minus degradation cost |
| 5 | Per-battery assignment | Map the portfolio-level optimal windows to individual asset commands, staggering start times and skipping ineligible assets |
| 6 | Confidence scoring | Combine price forecast uncertainty, wind forecast error, and fleet SoH variance into a single confidence percentage |
| 7 | Output assembly | Populate the D-05 AI Recommendation object and write it for the dashboard to consume |

---

### Step 1 · Price Signal Processing

The engine loads the 24-hour EPEX SPOT NL day-ahead price array from D-04 and identifies the optimal windows for charging (buy cheap) and discharging (sell expensive).

#### 1a — Candidate Window Enumeration

For each possible contiguous block of 1 to 4 hours within the 24-hour horizon, the engine computes the average price. Windows shorter than one hour or longer than four hours are excluded.

```
For each window_length in [1, 2, 3, 4] hours:
  For each start_hour in [0 .. 23 - window_length]:
    avg_price = mean(prices[start_hour : start_hour + window_length])
    candidate_charge_windows.add({ start, end, avg_price })
    candidate_discharge_windows.add({ start, end, avg_price })
Constraint: discharge window must start >= charge window end + 1 hour
            (battery needs at least 1 hour rest between charge and discharge)
```

#### 1b — Minimum Spread Filter

A window pair is only eligible if the gross price spread clears a minimum threshold, preventing cycles whose gross revenue would not cover round-trip efficiency losses.

```
gross_spread = avg_discharge_price - avg_charge_price
Minimum eligibility threshold:
  gross_spread >= 40 EUR/MWh
  (covers ~5% round-trip loss at 95% charge + 95% discharge efficiency,
   plus estimated 2 EUR/MWh imbalance settlement buffer for NL TSO TenneT)
Window pairs below this threshold are dropped from the candidate set.
```

> *NL market context: EPEX SPOT NL typically produces spreads of 60–150 EUR/MWh on weekdays in spring. Spreads below 40 EUR/MWh occur mainly on weekends or during extended low-demand periods and are correctly filtered out to protect battery longevity.*

#### 1c — Solar Irradiance Price Adjustment

On sunny days, actual intraday prices during peak solar hours (09:00–15:00) regularly deviate downward from the day-ahead by 15–35%. The engine applies a solar adjustment factor to produce a weather-corrected expected price.

```javascript
// Data source: D-06 solar_irradiance_wm2 per site, averaged across portfolio
avg_portfolio_ghi = mean(D-06.solar_irradiance_wm2 for all sites at hour h)

// Solar price adjustment factor (merit-order suppression model)
SOLAR_SUPPRESSION_RATE = 0.00055  // EUR/MWh reduction per W/m² of irradiance
solar_price_adjustment_eur_mwh = avg_portfolio_ghi * SOLAR_SUPPRESSION_RATE * raw_price_h
//  e.g. GHI 420 W/m², raw_price 85 EUR/MWh:
//       adjustment = 420 * 0.00055 * 85 = 19.6 EUR/MWh reduction
//       weather_corrected_price_h = 85 - 19.6 = 65.4 EUR/MWh

// Price cannot be corrected below 5 EUR/MWh (floor)
weather_corrected_price_h = max(5, raw_price_h - solar_price_adjustment_eur_mwh)

// All subsequent window scoring uses weather_corrected_price, not raw_price
```

#### 1d — Solar Duck Window Detection

On days with high forecast irradiance, the midday solar dip creates a secondary charge window. The engine explicitly detects and scores this "solar duck" opportunity.

```javascript
// Solar duck window is eligible if ALL of the following are true:
//   1. avg_portfolio_ghi > 350 W/m² during the candidate midday window
//   2. weather_corrected_price in the window is below the 24h median price
//   3. The window falls between 09:00 and 15:00
//   4. The gross spread vs. the evening discharge window still clears 40 EUR/MWh
candidate_charge_windows.add({
  start: solar_duck_start,
  end:   solar_duck_end,
  avg_price: mean(weather_corrected_prices[duck_window]),
  source: 'solar_duck'
})
// Cloudy day (cloud_cover_pct = 80%, GHI = 90):  solar duck NOT added
// Sunny day (cloud_cover_pct = 20%, GHI = 480):  solar duck 11:00-13:00 ADDED
```

> *Why cloudy periods matter: On overcast days without a solar duck dip, the midday price stays elevated (80–120 EUR/MWh) and only the overnight wind trough provides a viable charge window. The engine correctly skips the midday window, avoiding a cycle that would cost more in degradation than the narrow spread delivers.*

---

### Step 2 · Battery State Assessment

#### 2a — SoC Safety Bounds (LFP Chemistry)

| **Bound** | **Value** | **Rationale** |
|---|---|---|
| Charge ceiling | 90% SoC | Charging above 90% accelerates lithium plating in LFP cells. Allows 10% headroom for voltage balancing. |
| Discharge floor | 15% SoC | Operating below 15% risks deep discharge events on weaker cells within a module. |
| Preferred operating band | 20–80% SoC | The lowest-degradation operating zone for daily cycling. |
| Emergency hold threshold | SoC < 10% | Asset placed in Hold regardless of price signal. Recovery charge at next off-peak window. |

#### 2b — Available and Dispatchable Energy

```javascript
// Per asset, from D-01 (capacity_kwh) and D-02 (soc_pct):
available_to_charge_kwh  = (CHARGE_CEILING - soc_pct) / 100 * capacity_kwh
//   e.g. (90 - 55) / 100 * 1000 = 350 kWh available to charge
dispatchable_kwh         = (soc_pct - DISCHARGE_FLOOR) / 100 * capacity_kwh
//   e.g. (55 - 15) / 100 * 1000 = 400 kWh dispatchable

// Actual deliverable energy bounded by power rating × window duration:
charge_energy_kwh    = min(available_to_charge_kwh, power_rating_kw * window_hours * η_charge)
discharge_energy_kwh = min(dispatchable_kwh,        power_rating_kw * window_hours * η_discharge)
η_charge = η_discharge = 0.95   // LFP round-trip efficiency
```

#### 2c — Historical Pattern Weighting

| **Property** | **Description / Value** |
|---|---|
| **Average observed DoD over last 7 days (D-03)** | Sets the baseline cycle depth assumption for degradation cost calculation. Shallowly-cycled assets can tolerate a slightly deeper cycle today. |
| **Time-at-high-SoC in prior 24 h (D-03)** | If an asset spent more than 4 hours above 80% SoC yesterday, its calendar aging penalty for today is increased by 10%. |

#### 2d — Weather-Aware Dispatch Scenarios

| **Scenario** | **Weather Conditions** | **Charge Window Strategy** |
|---|---|---|
| Wind + Solar | wind_speed_ms > 6 (coastal) AND GHI > 350 W/m² | Two charge windows available: overnight wind trough (primary) and midday solar duck (secondary). Engine may split fleet. |
| Wind only | wind_speed_ms > 6 (coastal) AND GHI ≤ 350 W/m² | Single charge window: overnight wind trough. Most common NL spring scenario (overcast but windy). |
| Solar only | wind_speed_ms ≤ 6 AND GHI > 350 W/m² | Single charge window: midday solar duck. Engine checks both and chooses the better-scoring option. |
| Flat / Hold | wind_speed_ms ≤ 4 AND GHI ≤ 200 W/m² | No renewable suppression. Price curve is relatively flat. Likely triggers portfolio minimum gate (Step 4c) → Hold. |

#### 2e — Renewable Richness Score (RGS)

```javascript
// Composite index per hour — combines wind and solar price suppression
wind_score_h  = min(1.0, wind_speed_ms / 10.0)
solar_score_h = min(1.0, solar_irradiance_wm2 / 600.0)
if hour in [22, 23, 0, 1, 2, 3, 4, 5, 6]:   // overnight
    RGS_h = 0.85 * wind_score_h + 0.15 * solar_score_h
elif hour in [9, 10, 11, 12, 13, 14, 15]:    // solar hours
    RGS_h = 0.40 * wind_score_h + 0.60 * solar_score_h
else:                                          // transition hours
    RGS_h = 0.65 * wind_score_h + 0.35 * solar_score_h

// RGS is used as a tie-breaker when two charge windows score within 5% of each other
```

---

### Step 3 · Battery Degradation Cost Model

**Battery replacement cost assumption:** 120 EUR/kWh (LFP pack, 2026 NL market pricing).
Total expected cycle life at standard 80% DoD: 4,000 full-equivalent cycles.

#### 3a — Cycle Degradation Cost

```javascript
// Base cost of one full-equivalent cycle (100% DoD reference)
BASE_CYCLE_COST_EUR_KWH = replacement_cost_eur_kwh / expected_cycles
                        = 120 / 4000 = 0.030 EUR/kWh per full equivalent cycle

// Depth of Discharge (DoD) stress factor — LFP empirical exponent
DoD_pct    = (charge_energy_kwh / capacity_kwh) * 100
DoD_factor = (DoD_pct / 100) ^ 1.3
//   DoD 40% => (0.40)^1.3 = 0.333   (costs only 33% of a full cycle)
//   DoD 80% => (0.80)^1.3 = 0.742   (costs 74% of a full cycle)

// C-rate stress factor
C_rate        = power_kw / capacity_kwh
C_rate_factor = 1.0 + max(0, (C_rate - 0.5)) * 0.30
//   0.5C => factor 1.00 (reference, no penalty)
//   1.0C => factor 1.15

// Cycle degradation cost for this specific cycle:
cycle_deg_cost_eur = BASE_CYCLE_COST_EUR_KWH * capacity_kwh
                   * DoD_factor * C_rate_factor
```

#### 3b — Temperature Penalty

```javascript
// LFP cells degrade faster outside the 10–35 °C range
temp_penalty_factor:
  ambient_temp_c < 5°C   => 1.25  (lithium plating risk during charge)
  ambient_temp_c 5–35°C  => 1.00  (nominal, no penalty)
  ambient_temp_c > 35°C  => 1.20  (accelerated SEI growth)

// NL April context: ambient temps 8–15°C — factor is always 1.00
cycle_deg_cost_eur *= temp_penalty_factor
```

#### 3c — Calendar Aging Penalty

```javascript
time_above_80_pct = count(D-03 intervals where soc_pct > 80) / 96
calendar_surcharge_factor:
  time_above_80_pct < 0.10   => 1.00  (< 2.4 h above 80%: no surcharge)
  time_above_80_pct 0.10–0.25 => 1.08
  time_above_80_pct > 0.25   => 1.15  (> 6 h above 80%: significant surcharge)
cycle_deg_cost_eur *= calendar_surcharge_factor
```

#### 3d — SoH Adjustment

```javascript
// As the battery ages (SoH < 100%), each cycle represents a larger fraction
// of its remaining useful life.
soh_cost_multiplier = 100 / soh_pct
//   SoH 97% => multiplier 1.031  (3% more expensive per cycle)
//   SoH 85% => multiplier 1.176
//   SoH 75% => multiplier 1.333  (asset approaching end of useful life)

// Assets with SoH < 75% are excluded from dispatch entirely (see Step 5)
cycle_deg_cost_eur *= soh_cost_multiplier
```

---

### Step 4 · Window Optimisation

#### 4a — Net Benefit Calculation (per window pair, per asset)

```javascript
gross_revenue_eur = (avg_discharge_price - avg_charge_price)
                  * discharge_energy_kwh / 1000   // convert kWh to MWh
net_benefit_eur   = gross_revenue_eur - cycle_deg_cost_eur

// Example: BESS-01, Amsterdam
//   charge 03:00-06:00 @ avg 19 EUR/MWh, discharge 17:00-20:00 @ avg 164 EUR/MWh
//   discharge_energy = min(350, 500*3*0.95) = 350 kWh = 0.35 MWh
//   gross_revenue = (164 - 19) * 0.35 = EUR 50.75
//   DoD = 35%  => DoD_factor = 0.280   C_rate = 0.5C  => C_rate_factor = 1.00
//   cycle_deg_cost = 0.030 * 1000 * 0.280 * 1.00 * 1.00 * (100/97) = EUR 8.66
//   net_benefit = 50.75 - 8.66 = EUR 42.09
```

#### 4b — Portfolio-Level Scoring

```javascript
portfolio_net_benefit = sum(net_benefit_eur[asset] for eligible assets)
score = portfolio_net_benefit / sum(cycle_deg_cost_eur[asset])
        // benefit-to-wear ratio; higher is better
Optimal window pair = argmax(score) across all candidate pairs
// Tie-breaking: prefer the window pair with the shorter charge window
// (shallower DoD = less wear) when scores are within 5% of each other
```

#### 4c — Minimum Profitability Gate

```
Minimum net benefit to issue a Charge/Discharge recommendation:
  portfolio_net_benefit >= 200 EUR
  (approximately 10% of average daily cycle value)
If portfolio_net_benefit < 200 EUR:
  portfolio_action = 'Hold'
  all per_battery_actions.action = "Hold"
  confidence_pct is still computed and reported
  explanation references the low spread as the reason
```

> *This gate fires mainly on low-wind, low-solar weekends when EPEX SPOT NL prices are flat. In such cases the dashboard displays a Hold recommendation with an explanation noting the insufficient price spread — demonstrating that the AI is protecting the fleet, not simply always recommending dispatch.*

---

### Step 5 · Per-Battery Assignment

#### 5a — Eligibility Rules

| **Condition** | **Assignment** | **Reason field** |
|---|---|---|
| mode = fault (from D-02) | Hold / — | "Fault — held offline" |
| soh_pct < 75% (from D-02) | Hold / — | "SoH below dispatch threshold" |
| soc_pct > charge_ceiling − 5% | Discharge at peak (if soc > 70%), else Hold | "High SoC — dispatch at peak" |
| soc_pct < discharge_floor + 10% | Charge with delayed start (+30 min) | "Low SoC — delayed start" |
| net_benefit_eur for this asset < 0 | Hold / — | "Marginal asset — held to protect longevity" |
| All other assets | Charge at optimal window | null |

#### 5b — Start Time Staggering

```javascript
// Sort eligible assets by available_to_charge_kwh descending
sorted_assets = sort(eligible_assets, key=available_to_charge_kwh, descending=True)

// Assign staggered start times
for i, asset in enumerate(sorted_assets):
    asset.window_start = optimal_charge_start + (i // 3) * 15_minutes
    // Groups of 3 assets share a start slot; every 3rd group shifts by 15 min
    // e.g. BESS-01,02,03 → 03:00 │ BESS-04,06,07 → 03:15 │ BESS-09,10,12 → 03:30

// Discharge staggering follows the same logic in reverse
```

> *Grid constraint rationale: Staggering 12 assets across three 15-minute slots reduces the coincident demand step from ~5.6 MW to ~1.9 MW per slot, staying within typical MV feeder headroom of 2–3 MW in South Holland, North Holland, and Gelderland.*

---

### Step 6 · Confidence Scoring

#### 6a — Component Scores

| **Component** | **Weight** | **Calculation** |
|---|---|---|
| Price forecast accuracy | 40% | Based on EPEX SPOT NL day-ahead vs. actual MAE over trailing 30 days. score = max(0, 100 − MAE_pct × 5). PoC: use price_MAE_pct from D-07 engine_config.json. |
| Wind forecast confidence | 25% | North Sea wind drives overnight prices. KNMI 24h wind forecast RMSE converted to score: score = max(0, 100 − RMSE_ms × 8). PoC: use wind_RMSE_ms from D-07 engine_config.json. |
| Solar forecast confidence | 20% | Relevant only when solar duck window is active (Step 1d). score = max(0, 100 − cloud_MAE_pct × 2). PoC: use cloud_MAE_pct from D-07 engine_config.json. On cloudy Hold days, defaults to 50 (neutral). |
| Fleet SoH uniformity | 15% | Standard deviation of SoH across eligible assets. score = max(0, 100 − σ_SoH × 10). PoC: use sigma_soh_pct from D-07 engine_config.json. |

#### 6b — Final Confidence Formula

```javascript
// solar_forecast_score defaults to 50 if no solar duck window was activated
solar_forecast_score = 50  if dispatch_scenario in ['Wind only', 'Flat/Hold']
                    else  max(0, 100 - cloud_cover_forecast_MAE_pct * 2)

confidence_pct = round(
    0.40 * price_forecast_score
  + 0.25 * wind_forecast_score
  + 0.20 * solar_forecast_score
  + 0.15 * fleet_soh_score
)

// Example A — Wind + Solar day (anticyclone, clear, stable):
//   price=48, wind=76, solar=82, soh=97
//   confidence = round(19.2 + 19.0 + 16.4 + 14.6) = 69

// Example B — Wind only day (overcast, windy):
//   price=48, wind=76, solar=50 (defaulted), soh=97
//   confidence = round(19.2 + 19.0 + 10.0 + 14.6) = 63

// For the mock dataset: use 85 to reflect a high-confidence Wind + Solar day
```

> *Interpretation: 85%+ → Wind + Solar scenario, stable forecast, high conviction. 65–84% → Wind-only or Solar-only with moderate uncertainty. Below 65% → less reliable forecast; operator should review raw price chart and weather conditions before committing.*

---

### Step 7 · Output Assembly → D-05

| **Property** | **Description / Value** |
|---|---|
| **generated_at** | Current timestamp at time of engine run |
| **portfolio_action** | Derived from majority action across per-battery assignments (e.g. 9 of 11 eligible → Charge → "Coordinated charge") |
| **charge_window_start / end** | Optimal charge window from Step 4b |
| **charge_price_eur_mwh** | Average price over the charge window from D-04 |
| **discharge_window_start / end** | Optimal discharge window from Step 4b |
| **discharge_price_eur_mwh** | Average price over the discharge window from D-04 |
| **price_spread_multiplier** | discharge_price_eur_mwh / charge_price_eur_mwh, rounded to 1 d.p. |
| **avg_30d_spread_multiplier** | Copied directly from D-07 engine_config.json constant (hardcoded 6.1 for PoC). Used in explanation template as {avg_30d_spread}. |
| **confidence_pct** | Output of Step 6b |
| **explanation** | Template filled with charge price, discharge price, spread multiplier, avg_30d_spread (from D-07), and estimated_capture_eur |
| **estimated_capture_eur** | Sum of net_benefit_eur across all Charge/Discharge-assigned assets |
| **per_battery_actions** | Array of { battery_id, action, window_start, window_end, reason } from Steps 5a–5b. window_end = portfolio charge_window_end or discharge_window_end respectively; — for Hold assets. |

#### Explanation Template

```
"EPEX SPOT NL overnight prices fall to ~{charge_price} EUR/MWh between {charge_start}
and {charge_end} — the cheapest window of the next 24 hours, driven by {wind_context}
{solar_context} — before recovering to ~{discharge_price} EUR/MWh during the evening
demand ramp at {discharge_start}–{discharge_end}. The {spread_multiplier}x spread is
{spread_vs_avg} the 30-day average of {avg_30d_spread}x.
Estimated capture: ~EUR {estimated_capture} for the coordinated cycle."

// {wind_context}:
//   wind_speed_ms > 7  → "high wind output across the North Sea"
//   wind_speed_ms 5–7  → "sustained overnight wind generation"
//   wind_speed_ms < 5  → "lower overnight demand"
// {solar_context} appended only for Wind + Solar scenario:
//   "and a midday solar dip suppressing prices to ~{solar_duck_price} EUR/MWh,"
// {spread_vs_avg}: "well above" │ "above" │ "in line with"
```

---

### Formula Reference Summary

| **Quantity** | **Formula** |
|---|---|
| Available to charge (kWh) | (CHARGE_CEILING − soc_pct) / 100 × capacity_kwh |
| Dispatchable energy (kWh) | (soc_pct − DISCHARGE_FLOOR) / 100 × capacity_kwh |
| Charge energy (kWh) | min(available_to_charge, power_kw × hours × 0.95) |
| Discharge energy (kWh) | min(dispatchable, power_kw × hours × 0.95) |
| Solar price adjustment (€/MWh) | avg_portfolio_ghi × 0.00055 × raw_price_h (price ≥ 5 €/MWh) |
| Weather-corrected price (€/MWh) | max(5, raw_price_h − solar_price_adjustment) |
| Wind score (per hour) | min(1.0, wind_speed_ms / 10.0) |
| Solar score (per hour) | min(1.0, solar_irradiance_wm2 / 600.0) |
| RGS (overnight) | 0.85 × wind_score + 0.15 × solar_score |
| RGS (solar hours) | 0.40 × wind_score + 0.60 × solar_score |
| DoD (%) | charge_energy_kwh / capacity_kwh × 100 |
| DoD factor | (DoD / 100) ^ 1.3 |
| C-rate factor | 1.0 + max(0, (power_kw / capacity_kwh − 0.5)) × 0.30 |
| SoH cost multiplier | 100 / soh_pct |
| Cycle degradation cost (€) | 0.030 × capacity_kwh × DoD_factor × C_rate_factor × temp_factor × calendar_factor × soh_factor |
| Gross revenue (€) | (avg_discharge_price − avg_charge_price) × discharge_energy_kwh / 1000 |
| Net benefit (€) | gross_revenue − cycle_degradation_cost |
| Portfolio score | Σ net_benefit / Σ cycle_degradation_cost |
| Solar forecast score | 50 (neutral) if no solar window; else max(0, 100 − cloud_MAE_pct × 2) |
| Confidence (%) | 0.40 × price_score + 0.25 × wind_score + 0.20 × solar_forecast_score + 0.15 × soh_score |

---

## F-07 · Market Forecast Card

*Depends on F-06 AI Recommendation Engine (D-05 output)*

> *This feature must be implemented after F-06. It consumes the D-05 AI Recommendation object produced by the engine. All recommendation UI elements (action label, time window, price, confidence, explanation, per-battery strip) are driven by F-06 output.*

### US-07-01 · Day-Ahead Price Forecast Chart

*As an operator making dispatch decisions, I want to see a 24-hour day-ahead electricity price chart, so that I understand the price landscape my battery will operate in over the next day.*

#### Description

An SVG area chart renders the day-ahead price curve (€/MWh) over the next 24 hours. The chart includes a time axis with hourly tick labels, a price axis, and horizontal reference grid lines. The area beneath the curve is shaded in a purple gradient to aid visual reading of the price shape.

#### Acceptance Criteria

- **AC-1:** The chart renders a continuous price curve spanning 24 hours.
- **AC-2:** A time axis is present with at least 5 labelled time ticks spanning the 24-hour range.
- **AC-3:** A price axis is present with at least 2 labelled price levels (e.g. 0, 100, 200 €/MWh).
- **AC-4:** Horizontal reference grid lines are present at each labelled price level.
- **AC-5:** The area beneath the price curve is shaded with a gradient fill.
- **AC-6:** The chart renders without visible jank or rendering artefacts on a typical laptop.

---

### US-07-02 · Charge and Discharge Zone Overlays

*As an operator reading the price forecast, I want to see visually annotated windows on the chart marking when to charge and when to discharge, so that I can relate the AI recommendation to the price curve at a glance.*

#### Description

Two coloured overlay zones are rendered on the price chart: a hatched teal band over the low-price window (recommended charging period) and a hatched coral band over the high-price window (recommended discharge period). Each zone is labelled with "CHARGE" or "DISCHARGE". Data points marking the price minimum (teal dot) and maximum (coral dot) are also shown.

#### Acceptance Criteria

- **AC-1:** A hatched teal zone is overlaid on the chart in the low-price window.
- **AC-2:** A hatched coral zone is overlaid on the chart in the high-price window.
- **AC-3:** The teal zone is labelled "CHARGE" and the coral zone is labelled "DISCHARGE" directly on the chart.
- **AC-4:** A teal dot marks the price minimum on the curve; a coral dot marks the price maximum.
- **AC-5:** The charge zone horizontally aligns with the low-price trough and the discharge zone aligns with the high-price peak of the curve.

---

### US-07-03 · AI Portfolio Recommendation Block

*As an operator who wants to act on the AI's analysis, I want to see a clear, prominent recommendation block showing the suggested action, time windows, expected prices, and confidence level.*

#### Description

A recommendation block is rendered inside the Market Forecast card, above the chart. It always displays both the charge window and the discharge window alongside their respective expected prices, because the financial case depends on the spread between the two. When portfolio_action is "Hold", the window and price fields are hidden and replaced with an explanatory message.

#### Layout — Charge/Discharge action

| **Element** | **Data source** | **Example value** |
|---|---|---|
| Action verb (prominent) | D-05 portfolio_action | "Coordinated charge" |
| Charge row: window + price | D-05 charge_window_start/end + charge_price_eur_mwh | "03:00 – 06:00 · ~18.6 €/MWh" |
| Discharge row: window + price | D-05 discharge_window_start/end + discharge_price_eur_mwh | "17:00 – 20:00 · ~164.2 €/MWh" |
| Confidence badge | D-05 confidence_pct | "85% confidence" |
| Last-updated timestamp | D-05 generated_at (formatted as HH:MM) | "Updated 14:02" |
| "AI active" badge | Static label in card header | "AI active" |

#### Layout — Hold action

| **Element** | **Display** | **Data source** |
|---|---|---|
| Action verb | "Hold — no cycle recommended" | D-05 portfolio_action = "Hold" |
| Charge / discharge rows | Hidden — not shown | n/a |
| Hold reason message | "Insufficient price spread — battery longevity protected" | Static label triggered by Hold state |
| Confidence badge | Still shown — confidence reflects forecast quality, not the action value | D-05 confidence_pct |
| Last-updated timestamp | Still shown | D-05 generated_at |

#### Acceptance Criteria

- **AC-1:** The recommendation block is visible inside the Market Forecast card on dashboard load.
- **AC-2:** When portfolio_action is Charge or Discharge: the action verb, both the charge window row and the discharge window row, and the confidence badge are all visible simultaneously.
- **AC-3:** The charge row displays charge_window_start, charge_window_end, and charge_price_eur_mwh from D-05.
- **AC-4:** The discharge row displays discharge_window_start, discharge_window_end, and discharge_price_eur_mwh from D-05.
- **AC-5:** A confidence percentage (0–100%) is displayed in a badge or pill.
- **AC-6:** A last-updated timestamp is visible in the card subtitle, derived from D-05 generated_at (e.g. "Updated 14:02").
- **AC-7:** The "AI active" badge is visible in the card header regardless of action type.
- **AC-8:** When portfolio_action is Hold: the charge and discharge window rows are hidden and replaced with the message "Insufficient price spread — battery longevity protected".
- **AC-9:** When portfolio_action is Hold: the confidence badge and last-updated timestamp are still displayed.

---

### US-07-04 · AI Explainability Sentence

*As an operator who needs to justify dispatch decisions to stakeholders, I want to read a short plain-language explanation of why the AI made its recommendation.*

#### Description

Below the action/window/confidence row, a single explanatory paragraph states the reasoning behind the recommendation in natural language. It references concrete numbers from the forecast — specifically the price spread, the low and high price values, and the estimated revenue capture — to ground the recommendation in verifiable data.

#### Acceptance Criteria

- **AC-1:** An explanatory sentence or short paragraph is visible below the recommendation action row.
- **AC-2:** The explanation references at least one specific price figure (e.g. "38% drop", "~178 €/MWh").
- **AC-3:** The explanation references an estimated revenue or throughput outcome (e.g. "~€1,620").
- **AC-4:** The explanation is written in natural language (not code, not JSON, not a list of numbers).
- **AC-5:** The text fits within the card width without overflow on viewports ≥ 768 px.

---

### US-07-05 · Per-Battery Action Strip

*As an operator coordinating multiple assets, I want to see the AI's recommended action broken down per battery in a scrollable strip, so that I can verify individual asset instructions are appropriate.*

#### Description

Below the recommendation block, a horizontal strip shows one tile per asset. Each tile displays the asset ID, the recommended action (Charge / Discharge / Hold), and the time window (window_start – window_end from D-05 per_battery_actions). Tile background colours match the dashboard-wide state colour scheme. The strip is horizontally scrollable via prev/next arrow buttons. Clicking a tile selects that asset globally.

#### Tile Colour Scheme

| **Action** | **Tile background** | **Action label colour** |
|---|---|---|
| Charge | Teal (same as Charging state badge in F-03) | White |
| Discharge | Coral (same as Discharging state badge in F-03) | White |
| Hold | Muted grey/purple (same as Idle / fault badge in F-03) | Dark text |

#### Acceptance Criteria

- **AC-1:** One tile per asset (12 total) is rendered in the per-battery strip.
- **AC-2:** Each tile displays: asset ID, recommended action verb, and time window (e.g. "03:00 – 06:00"). The window comes from per_battery_actions[].window_start and window_end.
- **AC-3:** Fault-state assets display "Hold" and "—" (no time window) — not a charge or discharge instruction.
- **AC-4:** Tile backgrounds use the action colour scheme: teal for Charge, coral for Discharge, muted grey/purple for Hold — consistent with state badge colours in F-03 and F-04.
- **AC-5:** Prev and next arrow buttons navigate the visible window through the 12 tiles.
- **AC-6:** A counter (e.g. "1–5 / 12") updates as the strip is navigated.
- **AC-7:** Clicking a tile sets the globally selected asset (selectedAssetId), which updates F-04 (Current State Card), F-05 (Replay Card), and F-03 (Fleet Overview Table — auto-navigates to the page containing the selected asset).
- **AC-8:** The number of visible tiles adjusts gracefully on narrower viewports (fewer tiles shown simultaneously).

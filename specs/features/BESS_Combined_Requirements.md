# BESS Intelligence Layer

**Product Requirements & Specifications**

PoC build · Synthetic data · Read-only overlay on Withthegrid · Version 1.2 · 22 April 2026

---

## EPIC 1 · Mock Data Specification

*Synthetic data · Read-only overlay on Withthegrid · PoC build*

---

### Historical Data Generation Requirement

#### REQ-HIST-01 — Past-Year Data Generation

For each dataset defined in this specification (D-01 through D-06), the system must generate synthetic data covering a continuous rolling window of the past 12 months (365 days), ending at the current date. This historical depth is required to support trend analysis, performance benchmarking, and advisory report generation within the Intelligence Layer.

**The following per-dataset constraints apply:**

| Dataset | Granularity | 12-Month Requirement |
| --- | --- | --- |
| D-01 Battery Master Data | Static (per asset) | Commissioning dates must fall within the past year where applicable; decommissioned assets should appear in historical records but be absent from current snapshots |
| D-02 Real-Time Telemetry | Latest snapshot (per asset) | One snapshot per hour for the past year must be available as an archive; the live snapshot represents the most recent reading |
| D-03 Battery Historical Time Series | 15-minute intervals | 96 intervals/day × 365 days = 35,040 records per asset. The past-year corpus is used for trend cards and cycle-count analysis |
| D-04 Day-Ahead Market Prices | Hourly (24 prices/day) | 24 prices/day × 365 days = 8,760 hourly price records covering the past year of EPEX SPOT NL data |
| D-05 AI Recommendation | One record per day | 365 daily recommendation snapshots per asset group, capturing charge/discharge windows and confidence scores over time |
| D-06 Weather Data | Hourly per site | 8,760 hourly readings per site × 12 sites = 105,120 records for the past year; used for thermal context and wind correlation analysis |

All historical records must be internally consistent with the cross-dataset consistency rules defined in the Consistency Rules section. Time series data must use UTC-offset timestamps (Europe/Amsterdam, CET/CEST) and must not contain gaps, duplicate timestamps, or out-of-order entries.

---

### D-01 · Battery Master Data

Static registry of all BESS assets. One record per physical unit. Changes only when a new asset is commissioned or decommissioned. Does not include live telemetry.

**File:** `battery_master.json`

| Property | Description | Example |
| --- | --- | --- |
| `id` | Unique asset identifier | "BESS-01" |
| `site_name` | Human-readable site label | "Site A" |
| `location` | City where the asset is installed | "Amsterdam" |
| `country` | Country code | "NL" |
| `latitude` | GPS latitude for map and weather lookup | 52.3676 |
| `longitude` | GPS longitude | 4.9041 |
| `chemistry` | Battery cell chemistry | "LFP" |
| `power_rating_kw` | Maximum continuous charge / discharge power | 500 |
| `capacity_kwh` | Nameplate energy capacity | 1000 |
| `duration_h` | Discharge duration at rated power (capacity ÷ power) | 2.0 |
| `commissioned_date` | Date the asset was put into service | "2022-06-15" |
| `manufacturer` | System integrator or OEM name | "CATL" |
| `model` | Product model name | "EnerOne Plus" |
| `withthegrid_node_id` | Asset identifier in the Withthegrid platform | "a3f7c812-…" |

#### All 12 Assets

| ID | Site | Location | Power (kW) | Capacity (kWh) |
| --- | --- | --- | --- | --- |
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

### D-02 · Battery Real-Time Telemetry

Live operational snapshot of each asset, polled or streamed at short intervals (e.g. every 5 seconds). Contains everything that can change between refreshes. Separate from master data so the two concerns do not mix.

**File:** `battery_telemetry.json` (array of one record per asset)

| Property | Description | Example |
| --- | --- | --- |
| `id` | Asset identifier (foreign key to D-01) | "BESS-01" |
| `timestamp` | Time of the last telemetry reading | "2026-04-22T14:00:00+02:00" |
| `soc_pct` | State of charge | 55 |
| `soh_pct` | State of health — battery degradation indicator | 97 |
| `power_kw` | Instantaneous power; positive = charging, negative = discharging | -84.6 |
| `mode` | Operational mode: charging, discharging, idle, fault | "discharging" |
| `temperature_c` | Internal battery pack temperature | 17.2 |
| `voltage_v` | DC bus voltage | 748.4 |
| `current_a` | DC current; positive = charging | -113.1 |
| `next_action` | Next scheduled command: Charge, Discharge, Hold | "Charge" |
| `next_action_window` | Scheduled time for the next action; — if none | "03:00" |
| `fault_code` | Active fault code if mode is fault, otherwise null | null |

---

### D-03 · Battery Historical Time Series (24 h)

Per-asset time series of power output and state of charge at 15-minute resolution over the previous 24 hours. 96 data points per asset. This is the primary dataset for the Last 24 Hours Replay card and for calculating daily energy throughput.

**File:** `battery_history_24h.json` (object keyed by asset ID, each value an array of 96 readings)

| Property | Description | Example |
| --- | --- | --- |
| `timestamp` | Start of the 15-minute interval | "2026-04-21T14:00:00+02:00" |
| `power_kw` | Average power during the interval; positive = charging | 182.4 |
| `soc_pct` | State of charge at the end of the interval | 38.2 |

**Derived fields — computed from the series, not stored:**

| Derived value | Formula |
| --- | --- |
| Total charged (kWh) | Sum of max(power_kw, 0) × 0.25 for all intervals |
| Total discharged (kWh) | Sum of max(-power_kw, 0) × 0.25 for all intervals |
| Net cycles | (charged + discharged) ÷ 2 ÷ capacity_kwh |

---

### D-04 · Day-Ahead Market Prices

Hourly electricity prices for the next 24 hours as published by EPEX SPOT for the Netherlands bidding zone. This series drives the price chart in the Market Forecast card and is the primary input to the AI recommendation logic.

**File:** `market_forecast_prices.json`

| Property | Description | Example |
| --- | --- | --- |
| `market` | Market and bidding zone name | "EPEX SPOT NL" |
| `currency` | Currency of prices | "EUR" |
| `generated_at` | Timestamp when the forecast was published | "2026-04-22T12:00:00+02:00" |
| `prices` | Array of 24 hourly price objects | — |
| `prices[].hour_start` | Start of the pricing hour | "2026-04-22T14:00:00+02:00" |
| `prices[].price_eur_mwh` | Day-ahead clearing price for this hour | 98.4 |

**Design note:** The NL day-ahead market is heavily influenced by wind and solar, producing a more volatile price curve than most European markets. A realistic April weekday shape should include: an evening peak around 17:00–19:00 (~150–170 €/MWh), a deep overnight trough around 02:00–05:00 (~15–25 €/MWh), and a midday solar suppression dip around 11:00–14:00 (~55–80 €/MWh). The spread between overnight low and evening peak typically reaches 7–10× in spring.

---

### D-05 · AI Recommendation

A pre-computed recommendation object produced by the intelligence layer. Consumed directly by the Market Forecast card. For the PoC this is static JSON — in production it would be regenerated each time a new price forecast is published.

**File:** `ai_recommendation.json`

| Property | Description | Example |
| --- | --- | --- |
| `generated_at` | When this recommendation was computed | "2026-04-22T14:02:00+02:00" |
| `portfolio_action` | High-level action label shown in the recommendation block | "Coordinated charge" |
| `charge_window_start` | Start of the optimal charge window | "03:00" |
| `charge_window_end` | End of the optimal charge window | "06:00" |
| `charge_price_eur_mwh` | Expected average price during the charge window | 18.6 |
| `discharge_window_start` | Start of the optimal discharge window | "17:00" |
| `discharge_window_end` | End of the optimal discharge window | "20:00" |
| `discharge_price_eur_mwh` | Expected average price during the discharge window | 164.2 |
| `price_spread_multiplier` | Discharge price divided by charge price | 8.8 |
| `confidence_pct` | Model confidence in the recommendation | 85 |
| `explanation` | Natural-language rationale referencing concrete numbers | see example |
| `estimated_capture_eur` | Expected revenue for the full coordinated cycle | 1840 |
| `per_battery_actions[].battery_id` | Asset identifier | "BESS-01" |
| `per_battery_actions[].action` | Charge, Discharge, or Hold | "Charge" |
| `per_battery_actions[].window_start` | Scheduled action time; — for Hold | "03:00" |
| `per_battery_actions[].reason` | Short reason if the asset deviates from portfolio default | "Fault — held offline" |

---

### D-06 · Weather Data

Ambient environmental conditions for each site location. Currently not rendered in the dashboard UI but influences two important backend concerns: (1) battery thermal management — ambient temperature affects safe operating limits; (2) dispatch context — high North Sea wind output directly suppresses overnight EPEX SPOT NL prices.

**File:** `weather_forecast.json` (one entry per site, covering the same 24-hour horizon as D-04)

| Property | Description | Example |
| --- | --- | --- |
| `site_id` | Site identifier (matches site_name in D-01) | "Site A" |
| `location` | City name | "Amsterdam" |
| `timestamp` | Observation or forecast time | "2026-04-22T14:00:00+02:00" |
| `ambient_temp_c` | Ambient air temperature | 11.8 |
| `humidity_pct` | Relative humidity | 74 |
| `wind_speed_ms` | Wind speed at 10 m height | 6.4 |
| `solar_irradiance_wm2` | Global horizontal irradiance | 210 |
| `cloud_cover_pct` | Cloud cover fraction | 60 |
| `condition` | Summary label: clear, partly_cloudy, overcast, rain, storm | "partly_cloudy" |

---

### Dataset Dependency Map

| Dataset | Used by / depends on |
| --- | --- |
| D-01 Battery Master Data | Referenced by D-02, D-03, D-05, D-06 via battery_id / site_id |
| D-02 Real-Time Telemetry | Fleet Overview Table (F-03), Current State Card (F-05), Portfolio Header KPIs (F-02) |
| D-03 Historical Time Series | 24h Replay Card (F-06) |
| D-04 Day-Ahead Market Prices | Market Forecast Chart (F-04) |
| D-05 AI Recommendation | Recommendation Block (F-04) — depends on D-04 (prices) and D-02 (current SoC / mode per asset) |
| D-06 Weather Data | (Future) Thermal context, wind-driven price signals |

---

### Consistency Rules Across Datasets

These cross-dataset constraints must hold in the generated mock data or the dashboard will show contradictory values.

| Rule | Constraint |
| --- | --- |
| SoC continuity | D-03 last interval soc_pct must equal D-02 soc_pct for every asset |
| Mode / power sign | D-02 mode = charging ⇒ power_kw > 0; discharging ⇒ power_kw < 0; idle or fault ⇒ power_kw ≈ 0 |
| Fault assets held | Any asset with D-02 mode = fault must have D-05 per_battery_actions action = Hold |
| Recommendation aligns with prices | D-05 charge_window must correspond to the lowest-price hour(s) in D-04; discharge_window to the highest |
| Portfolio KPIs | D-02 power_kw summed across all assets = Net Power chip; Sum(soc_pct × capacity_kwh / 100) = Available Now chip |
| Per-battery action timing | D-02 next_action and next_action_window must match the corresponding entry in D-05 per_battery_actions |

---

## EPIC 2 · Dashboard Implementation

*PoC build · Synthetic data · Read-only overlay on Withthegrid · Version 1.0 · 22 April 2026*

---

### F-01 · Login Page

#### US-01-01 · Branded Login Screen with Demo Mode Indicator

*As an **operator opening the application for the first time**, I want to see a branded login screen with the product name, tagline, and a clear demo mode notice, so that I immediately understand what product I am using, feel confident it is a professional tool, and do not mistake the PoC for a production system.*

**Description**

When a user navigates to the application URL, the first view they encounter is the login card. It displays the BESS Intelligence logo mark, the product name, and a short positioning tagline. A small, visually subdued footer note explicitly states this is a demo build with no real authentication. A secondary "Forgot password?" link is present for visual completeness but performs no action. The overall visual design should feel polished and consistent with the dashboard that follows.

**Acceptance Criteria**

- **AC-1:** The login screen is the first and only view visible on initial page load.
- **AC-2:** The product logo mark (lightning bolt icon) is rendered in the top-centre of the card.
- **AC-3:** The product name "BESS Intelligence" is displayed adjacent to the logo.
- **AC-4:** The tagline "AI-augmented battery intelligence, on top of your monitoring platform" is visible beneath the logo and name.
- **AC-5:** The card is centred on the viewport both horizontally and vertically on screen sizes ≥ 768 px wide.
- **AC-6:** The layout is not broken on laptop (≥ 1024 px) or tablet (≥ 768 px) viewports.
- **AC-7:** The text "Demo build — no authentication is performed" (or equivalent) is visible below the "Sign in" button.
- **AC-8:** The demo note is styled in a visually subdued way (smaller font, muted colour) so it does not compete with the primary CTA.
- **AC-9:** A "Forgot password?" link is visible below the "Sign in" button.
- **AC-10:** Clicking "Forgot password?" produces no navigation and no error.

#### US-01-02 · Login Credentials and Sign-In Navigation

*As an **operator on the login screen**, I want to see clearly labelled, pre-filled input fields and be able to sign in by clicking the button or pressing Enter, so that I can proceed through the demo flow without friction and without typing credentials manually.*

**Description**

The login card contains two labelled input fields: one for email (type=email) and one for password (type=password). Both fields are pre-filled with valid-looking demo values. The primary call-to-action is the "Sign in" button. Clicking it, or pressing Enter while focus is in either input field, triggers an immediate client-side transition to the dashboard view. No network request is made and no validation is enforced — empty or incorrect values still permit sign-in.

**Acceptance Criteria**

- **AC-1:** An email input field with the label "Email" is present and visible.
- **AC-2:** A password input field with the label "Password" is present and visible, and its value is masked.
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

### F-02 · Portfolio Header & KPI Summary

#### US-02-01 · Total Capacity Display

*As an **operator who has just signed in**, I want to see the total installed capacity of my battery fleet in MWh, so that I immediately understand the scale of the assets under management.*

**Description**

The portfolio header displays a "Total Capacity" chip showing the sum of nameplate energy capacity across all 12 BESS assets. This value is fixed for the PoC (11.2 MWh) and does not change at runtime. It is shown alongside a label and appropriate unit.

**Acceptance Criteria**

- **AC-1:** A "Total Capacity" chip is visible in the portfolio header on dashboard load.
- **AC-2:** The chip displays a numeric value with the unit MWh.
- **AC-3:** The value matches the sum of all asset capacities defined in the fleet data (11.2 MWh).
- **AC-4:** The chip is visible on viewports ≥ 768 px without truncation.

#### US-02-02 · Available Energy Display

*As an **operator planning the next dispatch decision**, I want to see how much energy is immediately available across my fleet, so that I can judge whether the portfolio can respond to the AI recommendation without manual calculation.*

**Description**

The "Available Now" chip shows the aggregated usable energy currently stored across all non-fault assets, derived from each asset's SoC and capacity. It is dynamically computed from the fleet data and updates whenever the underlying values change.

**Acceptance Criteria**

- **AC-1:** An "Available Now" chip is visible in the portfolio header on dashboard load.
- **AC-2:** The chip displays a numeric value with the unit MWh.
- **AC-3:** The value is arithmetically consistent with the sum of (SoC% × capacity) for all non-fault assets (within rounding to two decimal places).
- **AC-4:** The chip value is distinct from the Total Capacity chip value and is lower or equal to it.

#### US-02-03 · Net Power Display

*As an **operator monitoring real-time operations**, I want to see the net real-time power flow of the entire portfolio in kW, so that I can confirm whether the fleet is net charging, net discharging, or balanced at this moment.*

**Description**

The "Net Power" chip shows the algebraic sum of all asset power readings, signed and in kW. A negative value indicates the portfolio is net discharging to the grid; a positive value indicates net charging; zero indicates a balanced or idle fleet.

**Acceptance Criteria**

- **AC-1:** A "Net Power" chip is visible in the portfolio header on dashboard load.
- **AC-2:** The chip displays a signed numeric value with the unit kW.
- **AC-3:** The value matches the algebraic sum of all asset power readings in the fleet data.
- **AC-4:** The chip value is negative when the majority of the fleet is discharging, consistent with the Fleet Overview table.

#### US-02-04 · Live Sync Status Indicator

*As an **operator relying on the dashboard for operational decisions**, I want to see a live status indicator and last-sync timestamp in the header, so that I know the data I am viewing is current and the telemetry feed is active.*

**Description**

A small animated dot and a text string showing the number of online assets and the elapsed time since the last data sync are displayed in the portfolio header below the page title. These reinforce the impression of a live, connected system.

**Acceptance Criteria**

- **AC-1:** A status dot is visible in the page subtitle area of the portfolio header.
- **AC-2:** The dot is rendered in a "live" colour (teal/green) indicating normal operation.
- **AC-3:** The subtitle text includes the number of online assets (e.g. "12 assets online").
- **AC-4:** The subtitle text includes a last-sync timestamp or elapsed time indicator (e.g. "Last sync 12 seconds ago").

---

### F-03 · Fleet Overview Table

#### US-03-01 · View Asset List with Key Metrics

*As an **operator managing a multi-site portfolio**, I want to see all my battery assets listed in a single table with their core operational metrics, so that I can assess the health and status of the whole fleet without opening each asset individually.*

**Description**

The Fleet Overview table renders one row per BESS asset. Each row displays the asset ID, site name and location, current operational state, real-time power, state of charge, state of health, temperature, and the next scheduled action. The table is the primary navigation surface for the dashboard.

**Acceptance Criteria**

- **AC-1:** The table renders on page load with one row per asset (12 rows total across all pages).
- **AC-2:** Each row displays: Asset ID, site name, State badge, Power (kW), SoC (% + bar), SoH (%), Temperature (°C), and Next Action.
- **AC-3:** All fields contain populated, non-empty values for every asset.
- **AC-4:** Asset IDs are displayed in ascending order (BESS-01 through BESS-12).
- **AC-5:** The table has a visible header row with labelled column names.

#### US-03-02 · Identify Operational State at a Glance

*As an **operator scanning the fleet table**, I want to instantly identify each asset's operational state through a colour-coded badge, so that I can distinguish charging, discharging, idle, and fault assets without reading each value.*

**Description**

Each table row carries a State badge that displays the operational mode label and uses a consistent colour scheme: teal for Charging, coral/orange for Discharging, muted purple for Idle, and pink for Fault. A small coloured dot inside the badge reinforces the colour signal. A legend at the bottom of the card maps colours to states.

**Acceptance Criteria**

- **AC-1:** Each row displays exactly one State badge with one of four values: Charging, Discharging, Idle, or Fault.
- **AC-2:** Charging badges are rendered in teal; Discharging in coral; Idle in muted grey/purple; Fault in pink.
- **AC-3:** The asset icon (left of the asset name) uses the same colour as the State badge for that row.
- **AC-4:** A legend at the bottom of the Fleet Overview card maps each colour to its state label.
- **AC-5:** The State badge colour and the sign of the Power value are mutually consistent for every row.

#### US-03-03 · Identify Fault Assets Immediately

*As an **operator responsible for fleet reliability**, I want to have fault-state assets visually distinguished from healthy assets with an animated alert, so that I notice a problem unit immediately without having to scan every row.*

**Description**

Assets in the "fault" operational mode display a pulsing animation on their icon in the table. The Power column shows "offline" instead of a kW value, and the Next Action column shows "Hold" with no scheduled time window, signalling that the unit is not available for dispatch.

**Acceptance Criteria**

- **AC-1:** Fault-state assets render a pulsing/glowing animation on their row icon.
- **AC-2:** The Power cell for a fault asset displays "offline" or equivalent — not a kW value.
- **AC-3:** The Next Action cell for a fault asset displays "Hold" and "—" (no time window).
- **AC-4:** The State badge for a fault asset is pink and labelled "Fault".
- **AC-5:** The animation is continuous and does not stop after a set number of pulses.

#### US-03-04 · Select an Asset to Drill Down

*As an **operator who wants deeper insight into a specific battery**, I want to click a table row to select that asset, so that the Current State and 24-Hour Replay cards update to show data for the asset I selected.*

**Description**

Every row in the fleet table is interactive. Clicking a row marks it as selected (highlighted with a left-side accent border and a background tint) and triggers the detail cards (F-05 and F-06) to re-render with the selected asset's data. Only one row can be selected at a time.

**Acceptance Criteria**

- **AC-1:** Clicking any table row applies a visible "selected" highlight to that row.
- **AC-2:** Only one row is highlighted as selected at any time; clicking a new row deselects the previous one.
- **AC-3:** After clicking a row, the Current State card (F-05) updates to display the selected asset's data.
- **AC-4:** After clicking a row, the 24-Hour Replay card (F-06) regenerates with the selected asset's data.
- **AC-5:** The cursor changes to a pointer when hovering over any table row.
- **AC-6:** A row-hover state (light background tint) is visible on mouse-over before clicking.

#### US-03-05 · Paginate Through the Fleet

*As an **operator with more assets than fit on a single screen**, I want to paginate through the fleet table and control how many rows are shown per page, so that I can browse the full fleet without the table becoming unwieldy.*

**Description**

The fleet table supports pagination with configurable page sizes (5, 10, or 25 rows). Page navigation is provided through numbered page buttons with smart ellipsis, plus previous/next arrows. A "Showing X–Y of Z assets" counter keeps the user oriented. The selected asset remains highlighted across page changes if it is visible.

**Acceptance Criteria**

- **AC-1:** By default, 5 rows are shown per page on initial load.
- **AC-2:** A page-size selector offers options: 5, 10, 25 rows per page.
- **AC-3:** Changing the page size updates the visible rows immediately without a page reload.
- **AC-4:** "Showing X–Y of Z assets" counter updates to reflect the current page and selected page size.
- **AC-5:** Previous and next arrow buttons are present; the previous button is disabled on page 1, the next on the last page.
- **AC-6:** Numbered page buttons are present; the active page button is visually distinct (filled/highlighted).
- **AC-7:** For more than 7 total pages, ellipsis (…) is used to truncate the page list; the first and last page numbers are always shown.
- **AC-8:** Navigating to a page that contains the selected asset preserves the row highlight.

#### US-03-06 · Temperature Warning Highlight

*As an **operator monitoring battery health**, I want to see an amber highlight on temperature values that exceed a safe threshold, so that I am immediately alerted to thermal conditions that may require attention.*

**Description**

The Temperature column displays each asset's current operating temperature in °C. When the value is at or above 28 °C, it renders in amber to signal an elevated condition requiring attention. Values below the threshold render in the standard text colour.

**Acceptance Criteria**

- **AC-1:** The Temperature column is present and displays a value in °C for every non-fault asset.
- **AC-2:** Temperature values ≥ 28 °C are rendered in amber/orange colour.
- **AC-3:** Temperature values < 28 °C are rendered in the default text colour.
- **AC-4:** The threshold boundary (28 °C) is consistently applied — an asset with exactly 28.0 °C triggers the amber style.

#### US-03-07 · Responsive Table on Narrow Viewports

*As an **operator accessing the dashboard on a tablet or small laptop**, I want secondary table columns to hide automatically on narrow screens, so that the table remains readable and usable without horizontal scrolling.*

**Description**

On viewports narrower than 900 px, the SoH and Temperature columns are hidden to preserve readability of the essential columns (Asset, State, Power, SoC, Next Action). The table font size also reduces slightly. The hidden data remains accessible by selecting the asset and viewing the Current State card.

**Acceptance Criteria**

- **AC-1:** On viewports ≥ 900 px wide, all seven columns are visible.
- **AC-2:** On viewports < 900 px wide, the SoH and Temperature columns are hidden.
- **AC-3:** On viewports < 900 px wide, the remaining five columns are fully legible with no overflow or horizontal scroll.
- **AC-4:** The table layout does not break and rows remain selectable on narrow viewports.

---

### F-04 · Market Forecast Card

#### US-04-01 · Day-Ahead Price Forecast Chart

*As an **operator making dispatch decisions**, I want to see a 24-hour day-ahead electricity price chart, so that I understand the price landscape my battery will operate in over the next day.*

**Description**

An SVG area chart renders the day-ahead price curve (€/MWh) over the next 24 hours. The chart includes a time axis with hourly tick labels, a price axis, and horizontal reference grid lines. The area beneath the curve is shaded in a purple gradient to aid visual reading of the price shape.

**Acceptance Criteria**

- **AC-1:** The chart renders a continuous price curve spanning 24 hours.
- **AC-2:** A time axis is present with at least 5 labelled time ticks spanning the 24-hour range.
- **AC-3:** A price axis is present with at least 2 labelled price levels (e.g. 0, 100, 200 €/MWh).
- **AC-4:** Horizontal reference grid lines are present at each labelled price level.
- **AC-5:** The area beneath the price curve is shaded with a gradient fill.
- **AC-6:** The chart renders without visible jank or rendering artefacts on a typical laptop.

#### US-04-02 · Charge and Discharge Zone Overlays

*As an **operator reading the price forecast**, I want to see visually annotated windows on the chart marking when to charge and when to discharge, so that I can relate the AI recommendation to the price curve at a glance without interpreting raw numbers.*

**Description**

Two coloured overlay zones are rendered on the price chart: a hatched teal band over the low-price window (recommended charging period) and a hatched coral band over the high-price window (recommended discharge period). Each zone is labelled with "CHARGE" or "DISCHARGE" in the corresponding accent colour. Data points marking the price minimum (teal dot) and maximum (coral dot) are also shown.

**Acceptance Criteria**

- **AC-1:** A hatched teal zone is overlaid on the chart in the low-price window.
- **AC-2:** A hatched coral zone is overlaid on the chart in the high-price window.
- **AC-3:** The teal zone is labelled "CHARGE" and the coral zone is labelled "DISCHARGE" directly on the chart.
- **AC-4:** A teal dot marks the price minimum on the curve; a coral dot marks the price maximum.
- **AC-5:** The charge zone horizontally aligns with the low-price trough and the discharge zone aligns with the high-price peak of the curve.

#### US-04-03 · AI Portfolio Recommendation Block

*As an **operator who wants to act on the AI's analysis**, I want to see a clear, prominent recommendation block showing the suggested action, time window, expected price, and confidence level, so that I can decide whether to follow the AI's advice without needing to interpret the raw chart myself.*

**Description**

A recommendation block is rendered inside the Market Forecast card, above the chart. It displays four elements in a structured layout: the action verb (e.g. "Coordinated charge"), the target time window and expected price, a confidence percentage badge, and a last-updated timestamp. The block is visually distinguished from the surrounding card by a background fill.

**Acceptance Criteria**

- **AC-1:** The recommendation block is visible inside the Market Forecast card.
- **AC-2:** An action verb (Charge, Discharge, or Hold) is displayed prominently.
- **AC-3:** A time window (e.g. "02:00 – 05:00") is displayed alongside an expected price in €/MWh.
- **AC-4:** A confidence percentage (0–100%) is displayed in a badge or pill.
- **AC-5:** A last-updated timestamp is visible in the card subtitle (e.g. "Updated 14:02").
- **AC-6:** The "AI active" badge is visible in the card header.

#### US-04-04 · AI Explainability Sentence

*As an **operator who needs to justify dispatch decisions to stakeholders**, I want to read a short plain-language explanation of why the AI made its recommendation, so that I can communicate the rationale without having to interpret the underlying model myself.*

**Description**

Below the action/window/confidence row, a single explanatory paragraph states the reasoning behind the recommendation in natural language. It references concrete numbers from the forecast — specifically the price spread, the low and high price values, and the estimated revenue capture — to ground the recommendation in verifiable data.

**Acceptance Criteria**

- **AC-1:** An explanatory sentence or short paragraph is visible below the recommendation action row.
- **AC-2:** The explanation references at least one specific price figure (e.g. "38% drop", "~178 €/MWh").
- **AC-3:** The explanation references an estimated revenue or throughput outcome (e.g. "~€1,620").
- **AC-4:** The explanation is written in natural language (not code, not JSON, not a list of numbers).
- **AC-5:** The text fits within the card width without overflow on viewports ≥ 768 px.

#### US-04-05 · Per-Battery Action Strip

*As an **operator coordinating multiple assets**, I want to see the AI's recommended action broken down per battery in a scrollable strip, so that I can verify that individual asset instructions are appropriate given each unit's state.*

**Description**

Below the recommendation block, a horizontal strip shows one tile per asset. Each tile displays the asset ID, the recommended action (Charge / Discharge / Hold), and the scheduled time window. The strip is horizontally scrollable via prev/next arrow buttons. A counter ("X–Y / 12") tracks the visible slice. Clicking a tile selects that asset across the dashboard.

**Acceptance Criteria**

- **AC-1:** One tile per asset (12 total) is rendered in the per-battery strip.
- **AC-2:** Each tile displays: asset ID, recommended action verb, and time window.
- **AC-3:** Fault-state assets display "Hold" and "—" (no time window) — not a charge or discharge instruction.
- **AC-4:** Prev and next arrow buttons navigate the visible window through the 12 tiles.
- **AC-5:** A counter (e.g. "1–5 / 12") updates as the strip is navigated.
- **AC-6:** Clicking a tile selects that asset and updates the Current State (F-05) and Replay (F-06) cards.
- **AC-7:** The number of visible tiles adjusts gracefully on narrower viewports (fewer tiles shown simultaneously).

---

### F-05 · Current Battery State Card

#### US-05-01 · State of Charge Ring Gauge

*As an **operator assessing a specific battery's readiness**, I want to see the selected battery's state of charge displayed as a visual ring gauge, so that I can intuitively grasp how full the battery is without reading a raw percentage.*

**Description**

An SVG donut ring gauge represents the selected asset's SoC. The ring arc is filled proportionally to the SoC percentage. The numeric value is displayed in the centre of the ring. The ring colour reflects the operational state (purple for idle, teal for charging, coral for discharging).

**Acceptance Criteria**

- **AC-1:** An SVG ring gauge is rendered in the Current State card.
- **AC-2:** The ring arc length is proportional to the SoC percentage (100% SoC = full ring, 0% = empty ring).
- **AC-3:** The numeric SoC value (as a percentage) is displayed in the centre of the ring.
- **AC-4:** The displayed percentage matches the SoC value in the Fleet Overview table row for the same asset.
- **AC-5:** The gauge updates when a different asset is selected in the fleet table.

#### US-05-02 · Operational State and Real-Time Power

*As an **operator verifying that a battery is behaving as expected**, I want to see the selected battery's current operational state and real-time power output, so that I can confirm the asset is in the correct mode and delivering the expected power.*

**Description**

The Current State card displays the operational mode (Charging / Discharging / Idle / Fault) as a colour-coded pill, alongside the real-time power reading in kW. Power is signed: positive for charging, negative for discharging, zero for idle. A directional label (e.g. "kW (discharging)") accompanies the power value.

**Acceptance Criteria**

- **AC-1:** An operational state pill is displayed in the card header, with one of four values: Charging, Discharging, Idle, or Fault.
- **AC-2:** The state pill colour is consistent with the colour scheme used in the Fleet Overview table (teal, coral, muted, pink).
- **AC-3:** The real-time power value is displayed with a unit (kW) and a directional label.
- **AC-4:** Charging assets display a positive kW value; discharging assets display a negative kW value; idle assets display ~0 kW.
- **AC-5:** The state pill and power sign are mutually consistent for every selectable asset.

#### US-05-03 · State of Health and Temperature Metrics

*As an **operator monitoring long-term asset health**, I want to see the selected battery's state of health and current temperature in clearly labelled metric tiles, so that I can identify degradation or thermal risk without navigating away from the main view.*

**Description**

Two metric tiles sit below the SoC gauge: one for State of Health (SoH, %) and one for temperature (°C). The temperature tile applies an amber highlight when the value exceeds a safe threshold, consistent with the fleet table. Both values are sourced from the selected asset's data.

**Acceptance Criteria**

- **AC-1:** A "State of Health" metric tile is present, displaying a percentage value with a label.
- **AC-2:** A "Temperature" metric tile is present, displaying a value in °C with a label.
- **AC-3:** Both values match those shown for the same asset in the Fleet Overview table.
- **AC-4:** The Temperature tile applies an amber visual treatment when the value is ≥ 28 °C.
- **AC-5:** Both metric tiles update when a different asset is selected.

#### US-05-04 · Battery Specifications Row

*As an **operator who needs to know the physical characteristics of the asset I am managing**, I want to see the selected battery's chemistry, power rating, capacity, and duration displayed below the asset name, so that I can confirm I am viewing the correct unit and understand its physical constraints.*

**Description**

Below the selected-asset banner (ID + site + location), a compact row displays four specification fields: battery chemistry badge (LFP), rated power in kW, total energy capacity in kWh, and calculated discharge duration in hours. These values are static per asset and change only when a different asset is selected.

**Acceptance Criteria**

- **AC-1:** A chemistry badge (e.g. "LFP") is visible in the specifications row.
- **AC-2:** The power rating is displayed in kW and labelled "Power rating".
- **AC-3:** The total capacity is displayed in kWh and labelled "Capacity".
- **AC-4:** The discharge duration is displayed in hours and labelled "Duration", calculated as capacity ÷ power rating.
- **AC-5:** All four specification fields update when a different asset is selected in the fleet table.
- **AC-6:** The asset name, site, and location displayed in the banner match the selected row in the fleet table.

#### US-05-05 · Asset Selection Updates the Card

*As an **operator who switches between assets frequently**, I want the Current State card to **refresh instantly** whenever I select a different asset, so that I always see accurate data for the asset I am currently inspecting.*

**Description**

The Current State card is driven by the globally selected asset index. When the user clicks a different row in the fleet table (or a tile in the per-battery strip), all fields in the card — SoC gauge, power, state pill, SoH, temperature, and specs — update to reflect the newly selected asset's values within one rendering frame.

**Acceptance Criteria**

- **AC-1:** Clicking a different row in the fleet table updates all fields in the Current State card without a page reload.
- **AC-2:** Clicking a tile in the per-battery action strip also updates the Current State card.
- **AC-3:** All fields (SoC, power, state pill, SoH, temperature, chemistry, power rating, capacity, duration) update simultaneously — no field retains stale data from the previous selection.
- **AC-4:** The asset name and site in the card banner match the newly selected row.
- **AC-5:** The transition is immediate (no visible loading delay for a data set of 12 assets).

---

### F-06 · Last 24 Hours Replay Card

#### US-06-01 · Composite Power and SoC Chart

*As an **operator auditing a battery's daily behaviour**, I want to see a combined chart showing both the power profile and the state-of-charge curve over the last 24 hours, so that I can understand how the asset's energy level responded to its charging and discharging activity.*

**Description**

The replay chart is an SVG composite: colour-coded vertical bars represent power at each 15-minute interval (teal = charging, coral = discharging, muted = idle), and a purple line-and-area overlay traces the SoC evolution across the same timeline. Both series share the same time axis. 96 data points are plotted (24 hours × 4 per hour).

**Acceptance Criteria**

- **AC-1:** The chart renders 96 power bars covering the 24-hour period.
- **AC-2:** Charging intervals are rendered in teal, discharging in coral, and idle/near-zero in muted purple.
- **AC-3:** A SoC area curve is overlaid on the power bars using the same time axis.
- **AC-4:** A time axis is visible with at least 4 labelled tick marks spanning the 24-hour range.
- **AC-5:** The chart renders without visible lag or artefacts on a typical laptop.
- **AC-6:** Charging and discharging intervals are visually distinguishable from each other and from idle periods.

#### US-06-02 · Animated Replay with Play / Pause

*As an **operator who wants to walk through a battery's day step by step**, I want to play an animated replay of the 24-hour history, so that I can see the sequence of charging and discharging events unfold in an engaging, time-ordered way.*

**Description**

A Play button starts an animation where a playhead (pink vertical line with a dot on the SoC curve) advances from left to right across the chart at a fixed speed (~100 ms per step). As the playhead moves, the current timestamp, SoC value, and power value update in a readout above the chart, and past intervals are highlighted at full opacity while future intervals are dimmed. Pressing Play again pauses the animation.

**Acceptance Criteria**

- **AC-1:** A Play button is visible below the chart.
- **AC-2:** Pressing Play starts the playhead animation from the current position; the playhead advances smoothly from left to right.
- **AC-3:** The timestamp readout above the chart updates at each step to reflect the current playhead position.
- **AC-4:** The SoC and Power values in the readout update at each step to match the data at the playhead position.
- **AC-5:** Power bars to the left of the playhead are rendered at full opacity; bars to the right are dimmed.
- **AC-6:** The Play button toggles to a Pause icon while the animation is running.
- **AC-7:** Pressing Pause halts the animation at the current position; the playhead and readout remain at that position.
- **AC-8:** When the playhead reaches the last data point, the animation stops automatically.

#### US-06-03 · Scrub Bar for Manual Seeking

*As an **operator who wants to inspect a specific moment in the battery's history**, I want to click anywhere on a scrub bar to jump the playhead to that point, so that I can quickly navigate to a time of interest without watching the full replay animation.*

**Description**

A horizontal scrub bar beneath the chart represents the full 24-hour timeline. A fill indicator shows how far through the timeline the playhead has progressed. Clicking any point on the scrub bar moves the playhead to the corresponding time position and updates the chart and readout immediately.

**Acceptance Criteria**

- **AC-1:** A scrub bar is visible below the chart.
- **AC-2:** The scrub bar displays a fill indicator that advances as the playhead progresses through the animation.
- **AC-3:** Clicking the leftmost point of the scrub bar moves the playhead to the start of the timeline (14:00).
- **AC-4:** Clicking the rightmost point moves the playhead to the end of the timeline.
- **AC-5:** Clicking any intermediate point on the scrub bar moves the playhead proportionally to the clicked position.
- **AC-6:** After seeking via the scrub bar, the timestamp, SoC, and Power readouts update immediately to reflect the new playhead position.
- **AC-7:** The play/pause state is preserved after seeking (if paused before the click, it remains paused).

#### US-06-04 · Operational State Pill During Replay

*As an **operator watching the replay**, I want to see the battery's operational state label update dynamically as the playhead moves, so that I always know at a glance whether the battery was charging, discharging, or idle at the moment being shown.*

**Description**

A state pill in the replay card header reflects the operational state corresponding to the current playhead position — Charging (teal) when power is positive, Discharging (coral) when negative, and Idle (muted) when near zero. The pill updates in sync with the playhead animation and manual scrubbing.

**Acceptance Criteria**

- **AC-1:** An operational state pill is visible in the replay card header.
- **AC-2:** The pill displays "Charging" (teal) when the power at the playhead position is positive.
- **AC-3:** The pill displays "Discharging" (coral) when the power at the playhead position is negative.
- **AC-4:** The pill displays "Idle" (muted) when the power at the playhead position is near zero.
- **AC-5:** The pill updates immediately when the playhead moves — whether by animation or by scrubbing.

#### US-06-05 · Daily Energy Summary Chips

*As an **operator reviewing a battery's daily performance**, I want to see a summary of total energy charged, discharged, and net cycle count for the day, so that I can assess utilisation without adding up the raw data myself.*

**Description**

Below the scrub bar, three summary chips display daily energy totals derived from the 24-hour power series: total energy charged (kWh), total energy discharged (kWh), and net equivalent full cycles (×). These values are calculated from the same data that drives the chart and are generated fresh each time an asset is selected.

**Acceptance Criteria**

- **AC-1:** Three summary chips are visible below the scrub bar: Charged (kWh), Discharged (kWh), Net Cycles (×).
- **AC-2:** All three chips display non-zero, non-empty values for every selectable asset.
- **AC-3:** The Charged value equals the sum of positive-power intervals × 0.25 h (15-minute resolution) in kWh.
- **AC-4:** The Discharged value equals the sum of absolute negative-power intervals × 0.25 h in kWh.
- **AC-5:** The Net Cycles value equals (Charged + Discharged) ÷ 2 ÷ asset capacity, rounded to one decimal place.
- **AC-6:** All three values update when a different asset is selected.

#### US-06-06 · Replay Resets on Asset Switch

*As an **operator who switches between assets to compare their daily behaviour**, I want the replay chart and animation to reset and restart automatically when I select a different asset, so that I always see a fresh replay starting from the beginning of the day for whichever asset I am currently inspecting.*

**Description**

When the user selects a new asset via the fleet table or per-battery strip, the replay card regenerates its data series for the new asset, resets the playhead to the start of the timeline, and starts the animation from the beginning. The daily summary chips also recalculate for the new asset.

**Acceptance Criteria**

- **AC-1:** Selecting a different asset in the fleet table regenerates the power bars and SoC curve for that asset.
- **AC-2:** The playhead resets to the leftmost position (start of the timeline) after asset switch.
- **AC-3:** The animation starts playing automatically after the chart regenerates.
- **AC-4:** The timestamp readout resets to the start time (14:00) after asset switch.
- **AC-5:** The daily energy summary chips recalculate and display values for the newly selected asset.
- **AC-6:** The SoC value at the rightmost point of the replay (end of day) is consistent with the current SoC shown in the Current State card (F-05) for the same asset.

---

*BESS Intelligence Layer · PoC mockup · Synthetic data · Read-only overlay on Withthegrid*

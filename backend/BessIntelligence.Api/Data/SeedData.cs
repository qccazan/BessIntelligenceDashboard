using BessIntelligence.Api.Models;

namespace BessIntelligence.Api.Data;

public static class SeedData
{
    // Deterministic RNG for reproducible data
    private static readonly Random Rng = new(42);

    // Amsterdam timezone for UTC-offset timestamps
    private static TimeZoneInfo AmsterdamTz => TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "W. Europe Standard Time" : "Europe/Amsterdam");

    // Base daily price profile (EUR/MWh) — April NL weekday shape
    private static readonly double[] BasePriceProfile =
    [
        40, 30, 20, 15, 18, 25,       // 00–05: overnight trough
        35, 55, 85, 95, 90, 75,       // 06–11: morning ramp + solar dip start
        65, 60, 70, 90, 120, 155,     // 12–17: solar dip → evening ramp
        165, 150, 110, 85, 65, 50     // 18–23: evening peak → wind-down
    ];

    // 2 sites with GPS coordinates
    private static readonly (string SiteId, string City, double Lat, double Lon, bool Coastal)[] Sites =
    [
        ("Site Alpha", "Amsterdam", 52.3676, 4.9041, true),
        ("Site Beta", "Rotterdam", 51.9225, 4.4792, true),
    ];

    // Manufacturers and models distributed across 12 batteries
    private static readonly string[] Manufacturers = ["Tesla", "Tesla", "Tesla", "BYD", "BYD", "BYD", "Samsung SDI", "Samsung SDI", "Samsung SDI", "LG Energy", "LG Energy", "LG Energy"];
    private static readonly string[] BatteryModels = ["Megapack 2XL", "Megapack 2XL", "Megapack 2XL", "Cube Pro C130", "Cube Pro C130", "Cube Pro C130", "SBB-200", "SBB-200", "SBB-200", "RESU Prime", "RESU Prime", "RESU Prime"];

    private const int FaultAssetIndex = 9; // BESS-10 (Site Beta)

    public static void Generate(AppDbContext context)
    {
        if (context.Batteries.Any()) return; // already seeded

        context.ChangeTracker.AutoDetectChangesEnabled = false;

        var config = SeedEngineConfig(context);
        var batteries = SeedBatteries(context);
        var solarInstallations = SeedSolarInstallations(context);
        var prices = SeedMarketPrices(context);
        SeedWeatherForecasts(context, prices);
        SeedSolarProduction(context, solarInstallations, prices);
        var historySocEnds = SeedBatteryHistory(context, batteries, prices);
        var telemetry = SeedBatteryTelemetry(context, batteries, historySocEnds, prices);
        SeedAiRecommendations(context, batteries, telemetry, prices, config);

        context.ChangeTracker.AutoDetectChangesEnabled = true;
    }

    // ── D-07 Engine Config ──────────────────────────────────────────────
    private static EngineConfig SeedEngineConfig(AppDbContext context)
    {
        var config = new EngineConfig
        {
            PriceMaePct = 4.0,
            WindRmseMs = 1.5,
            CloudMaePct = 9.0,
            SigmaSohPct = 0.3,
            Avg30dSpreadMultiplier = 6.1
        };
        context.EngineConfigs.Add(config);
        context.SaveChanges();
        return config;
    }

    // ── D-01 Battery Master (2 sites × 6 batteries) ────────────────────
    private static List<Battery> SeedBatteries(AppDbContext context)
    {
        var batteries = new List<Battery>();
        for (int i = 0; i < 12; i++)
        {
            bool isSiteAlpha = i < 6;
            var site = isSiteAlpha ? Sites[0] : Sites[1];
            int powerRating = isSiteAlpha ? 500 : 400;
            int capacity = isSiteAlpha ? 1000 : 800;
            double gridConnection = isSiteAlpha ? 600.0 : 500.0;

            batteries.Add(new Battery
            {
                Code = $"BESS-{i + 1:D2}",
                SiteName = site.SiteId,
                Location = site.City,
                Country = "NL",
                Latitude = site.Lat + (i % 6) * 0.001, // slight offset per battery
                Longitude = site.Lon + (i % 6) * 0.001,
                Chemistry = "LFP",
                PowerRatingKw = powerRating,
                CapacityKwh = capacity,
                DurationH = 2.0,
                CommissionedDate = new DateTime(2025, 1 + i, 15),
                Manufacturer = Manufacturers[i],
                BatteryModel = BatteryModels[i],
                WiththegridNodeId = $"WTG-{i + 1:D3}",
                GridConnectionKw = gridConnection
            });
        }
        context.Batteries.AddRange(batteries);
        context.SaveChanges();
        return batteries;
    }

    // ── D-08 Solar Installations (1 per site) ───────────────────────────
    private static List<SolarInstallation> SeedSolarInstallations(AppDbContext context)
    {
        var installations = new List<SolarInstallation>
        {
            new()
            {
                SiteId = "Site Alpha",
                Location = "Amsterdam",
                Latitude = 52.3676,
                Longitude = 4.9041,
                CapacityKwp = 350,
                PanelCount = 875, // 350kWp / 400W per panel
                PanelType = "Monocrystalline",
                TiltDeg = 25,
                AzimuthDeg = 180, // due south
                CommissionedDate = new DateTime(2024, 6, 1)
            },
            new()
            {
                SiteId = "Site Beta",
                Location = "Rotterdam",
                Latitude = 51.9225,
                Longitude = 4.4792,
                CapacityKwp = 250,
                PanelCount = 625, // 250kWp / 400W per panel
                PanelType = "Monocrystalline",
                TiltDeg = 30,
                AzimuthDeg = 175, // slightly west of south
                CommissionedDate = new DateTime(2024, 8, 15)
            }
        };
        context.SolarInstallations.AddRange(installations);
        context.SaveChanges();
        return installations;
    }

    // ── D-04 Market Prices (12 months hourly) ───────────────────────────
    private static List<MarketPrice> SeedMarketPrices(AppDbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var tz = AmsterdamTz;
        var localNow = TimeZoneInfo.ConvertTime(now, tz);
        var startHour = new DateTimeOffset(localNow.Year, localNow.Month, localNow.Day, localNow.Hour, 0, 0, localNow.Offset).AddDays(-365);

        var prices = new List<MarketPrice>(8760);
        var generatedAt = now;

        for (int h = 0; h < 8760; h++)
        {
            var hourStart = startHour.AddHours(h);
            int hour = hourStart.Hour;
            int dayOfYear = hourStart.DayOfYear;

            double basePrice = BasePriceProfile[hour];
            double dayFactor = 1.0 + 0.1 * Math.Sin(dayOfYear * 0.0172); // seasonal
            bool isWeekend = hourStart.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            double weekendFactor = isWeekend ? 0.75 : 1.0;
            double noise = 1.0 + (HashNoise(dayOfYear * 24 + hour) * 0.1);

            double price = Math.Round(basePrice * dayFactor * weekendFactor * noise, 2);
            price = Math.Max(5.0, price);

            prices.Add(new MarketPrice
            {
                Market = "EPEX SPOT NL",
                Currency = "EUR",
                GeneratedAt = generatedAt,
                HourStart = hourStart,
                PriceEurMwh = price
            });
        }

        for (int i = 0; i < prices.Count; i += 5000)
        {
            context.MarketPrices.AddRange(prices.Skip(i).Take(5000));
            context.SaveChanges();
        }

        return prices;
    }

    // ── D-06 Weather Forecasts (365 days × 2 sites × 24h) ──────────────
    private static void SeedWeatherForecasts(AppDbContext context, List<MarketPrice> prices)
    {
        // Group prices by day to align weather with price hours
        var pricesByDay = prices
            .GroupBy(p => p.HourStart.Date)
            .OrderBy(g => g.Key)
            .ToList();

        var batch = new List<WeatherForecast>(5000);

        foreach (var dayGroup in pricesByDay)
        {
            var dayPrices = dayGroup.OrderBy(p => p.HourStart).ToList();
            int dayOfYear = dayGroup.Key.DayOfYear;

            // Seasonal factors: summer has more sun, less wind
            double seasonalSolar = 0.5 + 0.5 * Math.Sin((dayOfYear - 80) * 2 * Math.PI / 365); // peak around June
            double seasonalWind = 1.2 - 0.4 * Math.Sin((dayOfYear - 80) * 2 * Math.PI / 365);  // stronger in winter

            foreach (var price in dayPrices)
            {
                int hour = price.HourStart.Hour;
                bool isNight = hour >= 20 || hour < 6;
                bool isDaytimePeak = hour >= 9 && hour <= 15;

                for (int s = 0; s < Sites.Length; s++)
                {
                    var site = Sites[s];

                    // Temperature: seasonal 2-22°C range
                    double baseTemp = 5 + 10 * seasonalSolar; // 5°C winter, 15°C summer baseline
                    double temp = Math.Round(baseTemp + 4.0 * Math.Sin((hour - 4) * Math.PI / 12.0) + HashNoise(s * 100 + hour + dayOfYear) * 2, 1);
                    temp = Math.Clamp(temp, -2.0, 30.0);

                    // Wind: coastal, seasonal variation
                    double baseWind = site.Coastal ? 7.5 : 5.0;
                    double windNight = isNight ? 1.3 : 1.0;
                    double wind = Math.Round(baseWind * windNight * seasonalWind + HashNoise(s * 200 + hour + dayOfYear) * 1.5, 1);
                    wind = Math.Max(1.0, wind);

                    // Solar GHI: seasonal, daytime curve
                    double ghi = 0;
                    if (!isNight && hour >= 6)
                    {
                        double peakGhi = 300 + 350 * seasonalSolar; // 300 winter, 650 summer
                        double solarCurve = Math.Sin((hour - 6) * Math.PI / 14.0);
                        double cloudEffect = 1.0 - (30 + HashNoise(s * 400 + hour + dayOfYear * 3) * 35) / 100.0;
                        cloudEffect = Math.Clamp(cloudEffect, 0.1, 1.0);
                        ghi = Math.Round(peakGhi * Math.Max(0, solarCurve) * cloudEffect + HashNoise(s * 300 + hour + dayOfYear) * 20, 0);
                        ghi = Math.Max(0, ghi);
                    }

                    // Cloud cover
                    double cloud = Math.Round(30 + HashNoise(s * 400 + hour + dayOfYear * 3) * 35, 0);
                    cloud = Math.Clamp(cloud, 0, 100);

                    // Humidity
                    double humidity = Math.Round(65 + HashNoise(s * 500 + hour + dayOfYear) * 20, 0);
                    humidity = Math.Clamp(humidity, 30, 100);

                    string condition = cloud < 20 ? "clear" :
                                       cloud < 50 ? "partly_cloudy" :
                                       cloud < 80 ? "overcast" :
                                       humidity > 85 ? "rain" : "overcast";

                    batch.Add(new WeatherForecast
                    {
                        SiteId = site.SiteId,
                        Location = site.City,
                        HourStart = price.HourStart,
                        AmbientTempC = temp,
                        HumidityPct = humidity,
                        WindSpeedMs = wind,
                        SolarIrradianceWm2 = ghi,
                        CloudCoverPct = cloud,
                        Condition = condition
                    });

                    if (batch.Count >= 5000)
                    {
                        context.WeatherForecasts.AddRange(batch);
                        context.SaveChanges();
                        batch.Clear();
                    }
                }
            }
        }

        if (batch.Count > 0)
        {
            context.WeatherForecasts.AddRange(batch);
            context.SaveChanges();
        }
    }

    // ── D-09 Solar Production (365 days × 2 sites × 96 intervals) ──────
    private static void SeedSolarProduction(AppDbContext context, List<SolarInstallation> installations, List<MarketPrice> prices)
    {
        var now = DateTimeOffset.UtcNow;
        var tz = AmsterdamTz;
        var localNow = TimeZoneInfo.ConvertTime(now, tz);
        int minute = (localNow.Minute / 15) * 15;
        var end = new DateTimeOffset(localNow.Year, localNow.Month, localNow.Day, localNow.Hour, minute, 0, localNow.Offset);
        var start = end.AddDays(-365);

        var batch = new List<SolarProduction>(5000);

        foreach (var installation in installations)
        {
            double capacityKwp = installation.CapacityKwp;
            double tiltRad = installation.TiltDeg * Math.PI / 180.0;
            double lat = installation.Latitude;

            for (var ts = start; ts < end; ts = ts.AddMinutes(15))
            {
                int hour = ts.Hour;
                int dayOfYear = ts.DayOfYear;
                bool isNight = hour >= 20 || hour < 6;

                double productionKw = 0;
                double ghi = 0;
                double panelTemp = 15; // default

                if (!isNight && hour >= 6)
                {
                    // Seasonal solar peak
                    double seasonalSolar = 0.5 + 0.5 * Math.Sin((dayOfYear - 80) * 2 * Math.PI / 365);
                    double peakGhi = 300 + 350 * seasonalSolar;

                    // Solar elevation curve (sine curve over daylight hours)
                    double minuteOfDay = hour * 60 + ts.Minute;
                    double solarCurve = Math.Sin((minuteOfDay - 360) * Math.PI / (14 * 60)); // sunrise ~06:00
                    solarCurve = Math.Max(0, solarCurve);

                    // Cloud effect with daily variation
                    int siteIdx = installation.SiteId == "Site Alpha" ? 0 : 1;
                    double cloudPct = 30 + HashNoise(siteIdx * 400 + hour + dayOfYear * 3) * 35;
                    cloudPct = Math.Clamp(cloudPct, 0, 100);
                    double cloudEffect = 1.0 - cloudPct / 100.0 * 0.8; // clouds reduce output by up to 80%

                    ghi = Math.Round(peakGhi * solarCurve * cloudEffect, 0);
                    ghi = Math.Max(0, ghi);

                    // Panel temperature: ambient + heating from irradiance
                    double baseTemp = 5 + 10 * seasonalSolar;
                    double ambientTemp = baseTemp + 4.0 * Math.Sin((hour - 4) * Math.PI / 12.0);
                    panelTemp = Math.Round(ambientTemp + ghi * 0.03, 1); // panels heat up ~3°C per 100 W/m²

                    // Production: GHI × panel area efficiency × temperature derating
                    // Typical panel efficiency: ~20%, inverter efficiency: ~97%
                    double tempDerating = 1.0 - Math.Max(0, panelTemp - 25) * 0.004; // -0.4%/°C above 25°C
                    tempDerating = Math.Max(0.85, tempDerating);

                    // Convert GHI to production using capacity and standard test conditions (1000 W/m²)
                    productionKw = capacityKwp * (ghi / 1000.0) * tempDerating * 0.97; // 0.97 = inverter efficiency
                    productionKw = Math.Round(Math.Max(0, productionKw), 1);
                }

                double capacityFactor = capacityKwp > 0 ? Math.Round(productionKw / capacityKwp * 100, 1) : 0;

                batch.Add(new SolarProduction
                {
                    SolarInstallationId = installation.Id,
                    Timestamp = ts,
                    ProductionKw = productionKw,
                    IrradianceWm2 = ghi,
                    PanelTempC = Math.Round(panelTemp, 1),
                    CapacityFactorPct = capacityFactor
                });

                if (batch.Count >= 5000)
                {
                    context.SolarProductions.AddRange(batch);
                    context.SaveChanges();
                    batch.Clear();
                }
            }
        }

        if (batch.Count > 0)
        {
            context.SolarProductions.AddRange(batch);
            context.SaveChanges();
        }
    }

    // ── D-03 Battery History (7 days at 15-min) ─────────────────────────
    private static Dictionary<int, double> SeedBatteryHistory(AppDbContext context, List<Battery> batteries, List<MarketPrice> prices)
    {
        var tz = AmsterdamTz;
        var now = DateTimeOffset.UtcNow;
        var localNow = TimeZoneInfo.ConvertTime(now, tz);
        int minute = (localNow.Minute / 15) * 15;
        var end = new DateTimeOffset(localNow.Year, localNow.Month, localNow.Day, localNow.Hour, minute, 0, localNow.Offset);
        var start = end.AddDays(-7);

        var priceLookup = prices
            .Where(p => p.HourStart >= start.AddHours(-1) && p.HourStart <= end)
            .ToDictionary(p => p.HourStart.ToUnixTimeSeconds() / 3600);

        var finalSoc = new Dictionary<int, double>();
        var batch = new List<BatteryHistory>(5000);

        foreach (var battery in batteries)
        {
            double soc = 50.0;
            int capacity = battery.CapacityKwh;
            int power = battery.PowerRatingKw;
            bool isFault = battery.Id == batteries[FaultAssetIndex].Id;

            for (var ts = start; ts < end; ts = ts.AddMinutes(15))
            {
                long hourKey = ts.ToUnixTimeSeconds() / 3600;
                double price = priceLookup.TryGetValue(hourKey, out var mp) ? mp.PriceEurMwh : 60.0;

                double intervalPower;
                if (isFault && ts >= end.AddHours(-2))
                {
                    intervalPower = 0;
                }
                else if (price < 30)
                {
                    intervalPower = power * 0.8;
                }
                else if (price > 120)
                {
                    intervalPower = -power * 0.8;
                }
                else
                {
                    intervalPower = power * 0.05 * (HashNoise((int)(ts.ToUnixTimeSeconds() / 900) + battery.Id * 1000) > 0 ? 1 : -1);
                }

                double socDelta = (intervalPower * 0.25) / capacity * 100.0;
                soc = Math.Clamp(soc + socDelta, 10.0, 95.0);

                if (soc >= 94.0 && intervalPower > 0) intervalPower = 0;
                if (soc <= 11.0 && intervalPower < 0) intervalPower = 0;

                batch.Add(new BatteryHistory
                {
                    BatteryId = battery.Id,
                    Timestamp = ts,
                    PowerKw = Math.Round(intervalPower, 1),
                    SocPct = Math.Round(soc, 1)
                });

                if (batch.Count >= 5000)
                {
                    context.BatteryHistories.AddRange(batch);
                    context.SaveChanges();
                    batch.Clear();
                }
            }

            finalSoc[battery.Id] = Math.Round(soc, 1);
        }

        if (batch.Count > 0)
        {
            context.BatteryHistories.AddRange(batch);
            context.SaveChanges();
        }

        return finalSoc;
    }

    // ── D-02 Battery Telemetry (12 current snapshots) ───────────────────
    private static List<BatteryTelemetry> SeedBatteryTelemetry(
        AppDbContext context, List<Battery> batteries,
        Dictionary<int, double> historySocEnds, List<MarketPrice> prices)
    {
        var now = DateTimeOffset.UtcNow;
        var latestPrices = prices.OrderByDescending(p => p.HourStart).Take(24).OrderBy(p => p.HourStart).ToList();
        var currentPrice = latestPrices.LastOrDefault()?.PriceEurMwh ?? 60;

        var telemetry = new List<BatteryTelemetry>();

        foreach (var battery in batteries)
        {
            bool isFault = battery.Id == batteries[FaultAssetIndex].Id;
            double soc = historySocEnds.TryGetValue(battery.Id, out var endSoc) ? endSoc : 50.0;

            string mode;
            double powerKw;
            string nextAction;
            string nextActionWindow;
            string? faultCode = null;

            if (isFault)
            {
                mode = "fault";
                powerKw = 0;
                nextAction = "Hold";
                nextActionWindow = "\u2014";
                faultCode = "TEMP_OVER_LIMIT";
            }
            else if (currentPrice < 30)
            {
                mode = "charging";
                powerKw = battery.PowerRatingKw * 0.8;
                nextAction = "Discharge";
                nextActionWindow = FindNextWindow(latestPrices, p => p > 120);
            }
            else if (currentPrice > 120)
            {
                mode = "discharging";
                powerKw = -battery.PowerRatingKw * 0.8;
                nextAction = "Charge";
                nextActionWindow = FindNextWindow(latestPrices, p => p < 30);
            }
            else
            {
                mode = "idle";
                powerKw = 0;
                nextAction = currentPrice < 60 ? "Charge" : "Discharge";
                nextActionWindow = currentPrice < 60
                    ? FindNextWindow(latestPrices, p => p < 30)
                    : FindNextWindow(latestPrices, p => p > 120);
            }

            double soh = Math.Round(95.0 + HashNoise(battery.Id * 777) * 4, 1);
            soh = Math.Clamp(soh, 88.0, 99.0);

            double temp = Math.Round(22.0 + HashNoise(battery.Id * 888) * 6, 1);
            double voltage = Math.Round(800 + HashNoise(battery.Id * 999) * 50, 1);
            double current = powerKw != 0 ? Math.Round(powerKw * 1000 / voltage, 1) : 0;

            telemetry.Add(new BatteryTelemetry
            {
                BatteryId = battery.Id,
                Timestamp = now,
                SocPct = soc,
                SohPct = soh,
                PowerKw = Math.Round(powerKw, 1),
                Mode = mode,
                TemperatureC = temp,
                VoltageV = voltage,
                CurrentA = current,
                NextAction = nextAction,
                NextActionWindow = nextActionWindow,
                FaultCode = faultCode
            });
        }

        context.BatteryTelemetries.AddRange(telemetry);
        context.SaveChanges();
        return telemetry;
    }

    // ── D-05 AI Recommendations (365 daily — simplified for history) ────
    private static void SeedAiRecommendations(
        AppDbContext context, List<Battery> batteries,
        List<BatteryTelemetry> telemetry, List<MarketPrice> prices,
        EngineConfig config)
    {
        var pricesByDay = prices
            .GroupBy(p => p.HourStart.Date)
            .OrderBy(g => g.Key)
            .ToList();

        var faultBatteryIds = telemetry.Where(t => t.Mode == "fault").Select(t => t.BatteryId).ToHashSet();

        var recommendations = new List<AiRecommendation>();
        var actions = new List<BatteryAction>();

        foreach (var dayGroup in pricesByDay)
        {
            var dayPrices = dayGroup.OrderBy(p => p.HourStart).ToList();
            if (dayPrices.Count < 20) continue;

            var cheapest = dayPrices.OrderBy(p => p.PriceEurMwh).First();
            var mostExpensive = dayPrices.OrderByDescending(p => p.PriceEurMwh).First();
            double spread = mostExpensive.PriceEurMwh / Math.Max(cheapest.PriceEurMwh, 1.0);

            double confidence = Math.Round(
                0.40 * (100 - config.PriceMaePct * 5) +
                0.25 * (100 - config.WindRmseMs * 8) +
                0.20 * (100 - config.CloudMaePct * 2) +
                0.15 * (100 - config.SigmaSohPct * 10), 0);

            string chargeStart = cheapest.HourStart.ToString("HH:mm");
            string chargeEnd = cheapest.HourStart.AddHours(2).ToString("HH:mm");
            string dischargeStart = mostExpensive.HourStart.ToString("HH:mm");
            string dischargeEnd = mostExpensive.HourStart.AddHours(2).ToString("HH:mm");

            double totalCapacity = batteries.Where(b => !faultBatteryIds.Contains(b.Id)).Sum(b => b.CapacityKwh);
            double captureEur = Math.Round((mostExpensive.PriceEurMwh - cheapest.PriceEurMwh) * totalCapacity / 1000.0, 2);

            var rec = new AiRecommendation
            {
                GeneratedAt = new DateTimeOffset(dayGroup.Key, TimeSpan.Zero),
                PortfolioAction = "Arbitrage",
                ChargeWindowStart = chargeStart,
                ChargeWindowEnd = chargeEnd,
                DischargeWindowStart = dischargeStart,
                DischargeWindowEnd = dischargeEnd,
                ChargePrice = cheapest.PriceEurMwh,
                DischargePrice = mostExpensive.PriceEurMwh,
                PriceSpreadMultiplier = Math.Round(spread, 1),
                Avg30dSpreadMultiplier = config.Avg30dSpreadMultiplier,
                ConfidencePct = confidence,
                Explanation = $"Charge at {chargeStart} ({cheapest.PriceEurMwh:F0} €/MWh), discharge at {dischargeStart} ({mostExpensive.PriceEurMwh:F0} €/MWh). Spread {spread:F1}× vs 30-day avg {config.Avg30dSpreadMultiplier:F1}×.",
                EstimatedCaptureEur = captureEur
            };
            recommendations.Add(rec);

            foreach (var battery in batteries)
            {
                bool isFault = faultBatteryIds.Contains(battery.Id);
                actions.Add(new BatteryAction
                {
                    Recommendation = rec,
                    BatteryId = battery.Id,
                    Action = isFault ? "Hold" : (battery.Id % 3 == 0 ? "Discharge" : "Charge"),
                    WindowStart = isFault ? "\u2014" : chargeStart,
                    WindowEnd = isFault ? "\u2014" : chargeEnd,
                    Reason = isFault ? "Fault \u2014 held offline" : $"Price spread {spread:F1}× favourable"
                });
            }

            if (recommendations.Count % 30 == 0)
            {
                context.AiRecommendations.AddRange(recommendations);
                context.BatteryActions.AddRange(actions);
                context.SaveChanges();
                recommendations.Clear();
                actions.Clear();
            }
        }

        if (recommendations.Count > 0)
        {
            context.AiRecommendations.AddRange(recommendations);
            context.BatteryActions.AddRange(actions);
            context.SaveChanges();
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>Deterministic noise function: returns value between -1 and 1</summary>
    private static double HashNoise(int seed)
    {
        long hash = (long)seed * 2654435761L & 0x7FFFFFFF;
        return (hash % 2000 - 1000) / 1000.0;
    }

    private static string FindNextWindow(List<MarketPrice> prices, Func<double, bool> condition)
    {
        var match = prices.FirstOrDefault(p => condition(p.PriceEurMwh));
        return match != null ? $"{match.HourStart:HH:mm}\u2013{match.HourStart.AddHours(2):HH:mm}" : "TBD";
    }
}

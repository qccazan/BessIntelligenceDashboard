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

    // 12 Dutch cities with GPS coordinates
    private static readonly (string City, double Lat, double Lon, bool Coastal)[] Cities =
    [
        ("Amsterdam", 52.3676, 4.9041, true),
        ("Rotterdam", 51.9225, 4.4792, true),
        ("Utrecht", 52.0907, 5.1214, false),
        ("The Hague", 52.0705, 4.3007, true),
        ("Eindhoven", 51.4416, 5.4697, false),
        ("Tilburg", 51.5555, 5.0913, false),
        ("Groningen", 53.2194, 6.5665, true),
        ("Almere", 52.3508, 5.2647, false),
        ("Breda", 51.5719, 4.7683, false),
        ("Nijmegen", 51.8126, 5.8372, false),
        ("Apeldoorn", 52.2112, 5.9699, false),
        ("Enschede", 52.2215, 6.8937, false),
    ];

    private static readonly string[] Manufacturers = ["Tesla", "Tesla", "Tesla", "BYD", "BYD", "BYD", "Samsung SDI", "Samsung SDI", "Samsung SDI", "LG Energy", "LG Energy", "LG Energy"];
    private static readonly string[] Models = ["Megapack 2XL", "Megapack 2XL", "Megapack 2XL", "Cube Pro C130", "Cube Pro C130", "Cube Pro C130", "SBB-200", "SBB-200", "SBB-200", "RESU Prime", "RESU Prime", "RESU Prime"];
    private static readonly string[] SiteNames = ["Site A", "Site B", "Site C", "Site D", "Site E", "Site F", "Site G", "Site H", "Site I", "Site J", "Site K", "Site L"];

    private const int FaultAssetIndex = 9; // BESS-10

    public static void Generate(AppDbContext context)
    {
        if (context.Batteries.Any()) return; // already seeded

        context.ChangeTracker.AutoDetectChangesEnabled = false;

        var config = SeedEngineConfig(context);
        var batteries = SeedBatteries(context);
        var prices = SeedMarketPrices(context);
        SeedWeatherForecasts(context, batteries, prices);
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

    // ── D-01 Battery Master ─────────────────────────────────────────────
    private static List<Battery> SeedBatteries(AppDbContext context)
    {
        var batteries = new List<Battery>();
        for (int i = 0; i < 12; i++)
        {
            bool isLarge = i < 8; // first 8 are 500kW/1000kWh
            var city = Cities[i];
            batteries.Add(new Battery
            {
                Code = $"BESS-{i + 1:D2}",
                SiteName = SiteNames[i],
                Location = city.City,
                Country = "NL",
                Latitude = city.Lat,
                Longitude = city.Lon,
                Chemistry = "NMC",
                PowerRatingKw = isLarge ? 500 : 400,
                CapacityKwh = isLarge ? 1000 : 800,
                DurationH = 2.0,
                CommissionedDate = new DateTime(2025, 1 + i, 15),
                Manufacturer = Manufacturers[i],
                BatteryModel = Models[i],
                WiththegridNodeId = $"WTG-{i + 1:D3}"
            });
        }
        context.Batteries.AddRange(batteries);
        context.SaveChanges();
        return batteries;
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

            // Base price + small deterministic daily variation
            double basePrice = BasePriceProfile[hour];
            double dayFactor = 1.0 + 0.1 * Math.Sin(dayOfYear * 0.0172); // seasonal
            bool isWeekend = hourStart.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            double weekendFactor = isWeekend ? 0.75 : 1.0;
            double noise = 1.0 + (HashNoise(dayOfYear * 24 + hour) * 0.1); // ±10% deterministic noise

            double price = Math.Round(basePrice * dayFactor * weekendFactor * noise, 2);
            price = Math.Max(5.0, price); // floor at 5 EUR/MWh

            prices.Add(new MarketPrice
            {
                Market = "EPEX SPOT NL",
                Currency = "EUR",
                GeneratedAt = generatedAt,
                HourStart = hourStart,
                PriceEurMwh = price
            });
        }

        // Batch insert
        for (int i = 0; i < prices.Count; i += 5000)
        {
            context.MarketPrices.AddRange(prices.Skip(i).Take(5000));
            context.SaveChanges();
        }

        return prices;
    }

    // ── D-06 Weather Forecasts (24h × 12 sites) ────────────────────────
    private static void SeedWeatherForecasts(AppDbContext context, List<Battery> batteries, List<MarketPrice> prices)
    {
        // Use last 24 hours of prices for alignment
        var last24Prices = prices.OrderByDescending(p => p.HourStart).Take(24).OrderBy(p => p.HourStart).ToList();
        var forecasts = new List<WeatherForecast>(288);

        foreach (var price in last24Prices)
        {
            int hour = price.HourStart.Hour;
            bool isNight = hour >= 20 || hour < 6;
            bool isDaytimePeak = hour >= 9 && hour <= 15;

            for (int s = 0; s < 12; s++)
            {
                var city = Cities[s];
                bool coastal = city.Coastal;

                // Temperature: 8-15°C range for NL April
                double temp = Math.Round(8.0 + 4.0 * Math.Sin((hour - 4) * Math.PI / 12.0) + HashNoise(s * 100 + hour) * 2, 1);
                temp = Math.Clamp(temp, 5.0, 18.0);

                // Wind: coastal 6-9 m/s, inland 3-5 m/s, at least one coastal > 8 overnight
                double baseWind = coastal ? 7.5 : 4.0;
                double windNight = isNight ? 1.3 : 1.0;
                double wind = Math.Round(baseWind * windNight + HashNoise(s * 200 + hour) * 1.5, 1);
                wind = Math.Max(1.0, wind);

                // Solar: 0 at night, peak 400-480 W/m² during 09-15
                double ghi = 0;
                if (!isNight && hour >= 6)
                {
                    double solarBase = isDaytimePeak ? 440 : 200;
                    double solarCurve = Math.Sin((hour - 6) * Math.PI / 14.0);
                    ghi = Math.Round(solarBase * Math.Max(0, solarCurve) + HashNoise(s * 300 + hour) * 30, 0);
                    ghi = Math.Max(0, ghi);
                }

                // Cloud cover and humidity
                double cloud = Math.Round(30 + HashNoise(s * 400 + hour) * 40, 0);
                cloud = Math.Clamp(cloud, 0, 100);
                double humidity = Math.Round(65 + HashNoise(s * 500 + hour) * 20, 0);
                humidity = Math.Clamp(humidity, 30, 100);

                string condition = cloud < 20 ? "clear" :
                                   cloud < 50 ? "partly_cloudy" :
                                   cloud < 80 ? "overcast" :
                                   humidity > 85 ? "rain" : "overcast";

                forecasts.Add(new WeatherForecast
                {
                    SiteId = batteries[s].SiteName,
                    Location = city.City,
                    HourStart = price.HourStart,
                    AmbientTempC = temp,
                    HumidityPct = humidity,
                    WindSpeedMs = wind,
                    SolarIrradianceWm2 = ghi,
                    CloudCoverPct = cloud,
                    Condition = condition
                });
            }
        }

        context.WeatherForecasts.AddRange(forecasts);
        context.SaveChanges();
    }

    // ── D-03 Battery History (7 days at 15-min) ─────────────────────────
    // Returns final SoC per battery for D-02 continuity
    private static Dictionary<int, double> SeedBatteryHistory(AppDbContext context, List<Battery> batteries, List<MarketPrice> prices)
    {
        var tz = AmsterdamTz;
        var now = DateTimeOffset.UtcNow;
        var localNow = TimeZoneInfo.ConvertTime(now, tz);
        // Round down to 15-min boundary
        int minute = (localNow.Minute / 15) * 15;
        var end = new DateTimeOffset(localNow.Year, localNow.Month, localNow.Day, localNow.Hour, minute, 0, localNow.Offset);
        var start = end.AddDays(-7);

        // Build hourly price lookup for the 7-day window
        var priceLookup = prices
            .Where(p => p.HourStart >= start.AddHours(-1) && p.HourStart <= end)
            .ToDictionary(p => p.HourStart.ToUnixTimeSeconds() / 3600);

        var finalSoc = new Dictionary<int, double>();
        var batch = new List<BatteryHistory>(5000);

        foreach (var battery in batteries)
        {
            double soc = 50.0; // start at 50%
            int capacity = battery.CapacityKwh;
            int power = battery.PowerRatingKw;
            bool isFault = battery.Id == batteries[FaultAssetIndex].Id;

            for (var ts = start; ts < end; ts = ts.AddMinutes(15))
            {
                // Find price for this hour
                long hourKey = ts.ToUnixTimeSeconds() / 3600;
                double price = priceLookup.TryGetValue(hourKey, out var mp) ? mp.PriceEurMwh : 60.0;

                double intervalPower;
                if (isFault && ts >= end.AddHours(-2))
                {
                    // Fault in last 2 hours — stuck
                    intervalPower = 0;
                }
                else if (price < 30)
                {
                    intervalPower = power * 0.8; // charging
                }
                else if (price > 120)
                {
                    intervalPower = -power * 0.8; // discharging
                }
                else
                {
                    intervalPower = power * 0.05 * (HashNoise((int)(ts.ToUnixTimeSeconds() / 900) + battery.Id * 1000) > 0 ? 1 : -1);
                }

                // Update SoC
                double socDelta = (intervalPower * 0.25) / capacity * 100.0;
                soc = Math.Clamp(soc + socDelta, 10.0, 95.0);

                // If near limits, reduce power
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
                nextActionWindow = "\u2014"; // em dash
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

    // ── D-05 AI Recommendations (365 daily) ─────────────────────────────
    private static void SeedAiRecommendations(
        AppDbContext context, List<Battery> batteries,
        List<BatteryTelemetry> telemetry, List<MarketPrice> prices,
        EngineConfig config)
    {
        // Group prices by day
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
            if (dayPrices.Count < 20) continue; // skip incomplete days

            var cheapest = dayPrices.OrderBy(p => p.PriceEurMwh).First();
            var mostExpensive = dayPrices.OrderByDescending(p => p.PriceEurMwh).First();
            double spread = mostExpensive.PriceEurMwh / Math.Max(cheapest.PriceEurMwh, 1.0);

            // Confidence: 0.40*80 + 0.25*88 + 0.20*82 + 0.15*97 = 85
            double confidence = Math.Round(
                0.40 * (100 - config.PriceMaePct * 5) +
                0.25 * (100 - config.WindRmseMs * 8) +
                0.20 * (100 - config.CloudMaePct * 2) +
                0.15 * (100 - config.SigmaSohPct * 10), 0);

            string chargeStart = cheapest.HourStart.ToString("HH:mm");
            string chargeEnd = cheapest.HourStart.AddHours(2).ToString("HH:mm");
            string dischargeStart = mostExpensive.HourStart.ToString("HH:mm");
            string dischargeEnd = mostExpensive.HourStart.AddHours(2).ToString("HH:mm");

            // Total fleet capacity for capture estimation
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

            // Batch insert every 30 days
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

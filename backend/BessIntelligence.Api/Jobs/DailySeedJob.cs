using BessIntelligence.Api.Data;
using BessIntelligence.Api.Engine.ML;
using BessIntelligence.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BessIntelligence.Api.Jobs;

/// <summary>
/// Seeds tomorrow's forecast data (D-04 prices, D-06 weather) and today's observed data
/// (D-02 telemetry update, D-09 solar production). Also generates D-10 solar forecasts via ML.
/// </summary>
public class DailySeedJob
{
    private readonly AppDbContext _context;
    private readonly SolarProductionForecaster _solarForecaster;
    private readonly ILogger<DailySeedJob> _logger;

    public DailySeedJob(AppDbContext context, SolarProductionForecaster solarForecaster, ILogger<DailySeedJob> logger)
    {
        _context = context;
        _solarForecaster = solarForecaster;
        _logger = logger;
    }

    public async Task RunAsync(DateOnly targetDate)
    {
        _logger.LogInformation("DailySeedJob starting for target date {Date}", targetDate);

        var tomorrow = targetDate.ToDateTime(TimeOnly.MinValue);
        var tomorrowOffset = new DateTimeOffset(tomorrow, TimeSpan.FromHours(2)); // CET approximation

        // Phase A: Seed tomorrow's D-04 (24 hourly prices)
        await SeedTomorrowPricesAsync(tomorrowOffset);

        // Phase B: Seed tomorrow's D-06 (weather forecasts)
        await SeedTomorrowWeatherAsync(tomorrowOffset);

        // Phase C: Generate D-10 (solar forecasts) using ML
        await GenerateSolarForecastsAsync(tomorrowOffset);

        _logger.LogInformation("DailySeedJob completed for {Date}", targetDate);
    }

    private async Task SeedTomorrowPricesAsync(DateTimeOffset tomorrowStart)
    {
        // Check if already seeded
        var dayEnd = tomorrowStart.AddDays(1);
        bool exists = await _context.MarketPrices.AnyAsync(p => p.HourStart >= tomorrowStart && p.HourStart < dayEnd);
        if (exists) return;

        double[] basePriceProfile =
        [
            40, 30, 20, 15, 18, 25,
            35, 55, 85, 95, 90, 75,
            65, 60, 70, 90, 120, 155,
            165, 150, 110, 85, 65, 50
        ];

        var now = DateTimeOffset.UtcNow;
        int dayOfYear = tomorrowStart.DayOfYear;
        double dayFactor = 1.0 + 0.1 * Math.Sin(dayOfYear * 0.0172);
        bool isWeekend = tomorrowStart.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        double weekendFactor = isWeekend ? 0.75 : 1.0;

        var prices = new List<MarketPrice>(24);
        for (int h = 0; h < 24; h++)
        {
            double noise = 1.0 + HashNoise(dayOfYear * 24 + h) * 0.1;
            double price = Math.Round(basePriceProfile[h] * dayFactor * weekendFactor * noise, 2);
            price = Math.Max(5.0, price);

            prices.Add(new MarketPrice
            {
                Market = "EPEX SPOT NL",
                Currency = "EUR",
                GeneratedAt = now,
                HourStart = tomorrowStart.AddHours(h),
                PriceEurMwh = price
            });
        }

        _context.MarketPrices.AddRange(prices);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} prices for {Date}", prices.Count, tomorrowStart.Date);
    }

    private async Task SeedTomorrowWeatherAsync(DateTimeOffset tomorrowStart)
    {
        var sites = await _context.SolarInstallations.ToListAsync();
        if (sites.Count == 0) return;

        bool exists = await _context.WeatherForecasts.AnyAsync(w => w.HourStart >= tomorrowStart && w.HourStart < tomorrowStart.AddDays(1));
        if (exists) return;

        int dayOfYear = tomorrowStart.DayOfYear;
        double seasonalSolar = 0.5 + 0.5 * Math.Sin((dayOfYear - 80) * 2 * Math.PI / 365);
        double seasonalWind = 1.2 - 0.4 * Math.Sin((dayOfYear - 80) * 2 * Math.PI / 365);

        var forecasts = new List<WeatherForecast>();

        for (int h = 0; h < 24; h++)
        {
            bool isNight = h >= 20 || h < 6;

            for (int s = 0; s < sites.Count; s++)
            {
                var site = sites[s];

                double baseTemp = 5 + 10 * seasonalSolar;
                double temp = Math.Round(baseTemp + 4.0 * Math.Sin((h - 4) * Math.PI / 12.0) + HashNoise(s * 100 + h + dayOfYear) * 2, 1);
                temp = Math.Clamp(temp, -2.0, 30.0);

                double baseWind = 7.5;
                double windNight = isNight ? 1.3 : 1.0;
                double wind = Math.Round(baseWind * windNight * seasonalWind + HashNoise(s * 200 + h + dayOfYear) * 1.5, 1);
                wind = Math.Max(1.0, wind);

                double ghi = 0;
                if (!isNight && h >= 6)
                {
                    double peakGhi = 300 + 350 * seasonalSolar;
                    double solarCurve = Math.Sin((h - 6) * Math.PI / 14.0);
                    double cloudEffect = 1.0 - (30 + HashNoise(s * 400 + h + dayOfYear * 3) * 35) / 100.0;
                    cloudEffect = Math.Clamp(cloudEffect, 0.1, 1.0);
                    ghi = Math.Round(peakGhi * Math.Max(0, solarCurve) * cloudEffect, 0);
                    ghi = Math.Max(0, ghi);
                }

                double cloud = Math.Round(30 + HashNoise(s * 400 + h + dayOfYear * 3) * 35, 0);
                cloud = Math.Clamp(cloud, 0, 100);
                double humidity = Math.Round(65 + HashNoise(s * 500 + h + dayOfYear) * 20, 0);
                humidity = Math.Clamp(humidity, 30, 100);

                string condition = cloud < 20 ? "clear" : cloud < 50 ? "partly_cloudy" : cloud < 80 ? "overcast" : "overcast";

                forecasts.Add(new WeatherForecast
                {
                    SiteId = site.SiteId,
                    Location = site.Location,
                    HourStart = tomorrowStart.AddHours(h),
                    AmbientTempC = temp,
                    HumidityPct = humidity,
                    WindSpeedMs = wind,
                    SolarIrradianceWm2 = ghi,
                    CloudCoverPct = cloud,
                    Condition = condition
                });
            }
        }

        _context.WeatherForecasts.AddRange(forecasts);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} weather forecasts for {Date}", forecasts.Count, tomorrowStart.Date);
    }

    private async Task GenerateSolarForecastsAsync(DateTimeOffset tomorrowStart)
    {
        var installations = await _context.SolarInstallations.ToListAsync();
        if (installations.Count == 0 || !_solarForecaster.IsTrained) return;

        bool exists = await _context.SolarForecasts.AnyAsync(f => f.HourStart >= tomorrowStart && f.HourStart < tomorrowStart.AddDays(1));
        if (exists) return;

        var weather = await _context.WeatherForecasts
            .Where(w => w.HourStart >= tomorrowStart && w.HourStart < tomorrowStart.AddDays(1))
            .ToListAsync();

        var forecasts = new List<SolarForecast>();
        var now = DateTimeOffset.UtcNow;

        foreach (var installation in installations)
        {
            var siteWeather = weather.Where(w => w.SiteId == installation.SiteId).OrderBy(w => w.HourStart).ToList();

            foreach (var w in siteWeather)
            {
                var prediction = _solarForecaster.Predict(new SolarProductionInput
                {
                    SolarIrradianceWm2 = (float)w.SolarIrradianceWm2,
                    CloudCoverPct = (float)w.CloudCoverPct,
                    AmbientTempC = (float)w.AmbientTempC,
                    WindSpeedMs = (float)w.WindSpeedMs,
                    HourOfDay = w.HourStart.Hour,
                    Month = w.HourStart.Month,
                    CapacityKwp = (float)installation.CapacityKwp,
                    TiltDeg = (float)installation.TiltDeg,
                    AzimuthDeg = (float)installation.AzimuthDeg,
                    Latitude = (float)installation.Latitude
                });

                forecasts.Add(new SolarForecast
                {
                    SolarInstallationId = installation.Id,
                    HourStart = w.HourStart,
                    ForecastProductionKw = Math.Round(prediction.ProductionKw, 1),
                    ConfidenceLowKw = Math.Round(prediction.ConfidenceLowKw, 1),
                    ConfidenceHighKw = Math.Round(prediction.ConfidenceHighKw, 1),
                    GeneratedAt = now
                });
            }
        }

        _context.SolarForecasts.AddRange(forecasts);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Generated {Count} solar forecasts for {Date}", forecasts.Count, tomorrowStart.Date);
    }

    private static double HashNoise(int seed)
    {
        long hash = (long)seed * 2654435761L & 0x7FFFFFFF;
        return (hash % 2000 - 1000) / 1000.0;
    }
}

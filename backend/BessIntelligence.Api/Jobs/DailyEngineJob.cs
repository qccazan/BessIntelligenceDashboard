using BessIntelligence.Api.Data;
using BessIntelligence.Api.Engine;
using BessIntelligence.Api.Engine.ML;
using BessIntelligence.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BessIntelligence.Api.Jobs;

/// <summary>
/// Runs at startup if no recommendation exists for tomorrow.
/// Trains ML models on historical data, then runs the dispatch engine.
/// Also triggered on-demand via POST /api/engine/run.
/// </summary>
public class DailyEngineJob
{
    private readonly AppDbContext _context;
    private readonly SolarProductionForecaster _solarForecaster;
    private readonly DegradationPredictor _degradationPredictor;
    private readonly DailySeedJob _seedJob;
    private readonly ILogger<DailyEngineJob> _logger;

    public DailyEngineJob(
        AppDbContext context,
        SolarProductionForecaster solarForecaster,
        DegradationPredictor degradationPredictor,
        DailySeedJob seedJob,
        ILogger<DailyEngineJob> logger)
    {
        _context = context;
        _solarForecaster = solarForecaster;
        _degradationPredictor = degradationPredictor;
        _seedJob = seedJob;
        _logger = logger;
    }

    public async Task<EngineRunStatus> RunAsync(DateOnly? targetDate = null, bool forceRegenerate = false)
    {
        var tomorrow = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        // Check if already generated (unless forcing regeneration)
        var existingStatus = await _context.EngineRunStatuses
            .FirstOrDefaultAsync(s => s.Date == tomorrow);

        if (existingStatus != null && existingStatus.Status == "Completed" && !forceRegenerate)
        {
            _logger.LogInformation("Recommendation already exists for {Date}, skipping.", tomorrow);
            return existingStatus;
        }

        // Create or update status
        var status = existingStatus ?? new EngineRunStatus { Date = tomorrow };
        status.Status = "Running";
        status.StartedAt = DateTimeOffset.UtcNow;
        status.Error = null;

        if (existingStatus == null)
            _context.EngineRunStatuses.Add(status);

        await _context.SaveChangesAsync();

        try
        {
            // If regenerating, clear existing recommendation for this date
            if (forceRegenerate)
            {
                var tomorrowStart = new DateTimeOffset(tomorrow.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(2));
                var tomorrowEnd = tomorrowStart.AddDays(1);

                var existingRec = await _context.AiRecommendations
                    .Include(r => r.BatteryActions)
                    .FirstOrDefaultAsync(r => r.GeneratedAt >= tomorrowStart && r.GeneratedAt < tomorrowEnd);

                if (existingRec != null)
                {
                    _context.BatteryActions.RemoveRange(existingRec.BatteryActions);
                    _context.AiRecommendations.Remove(existingRec);
                    await _context.SaveChangesAsync();
                }

                // Also clear forecasts to regenerate
                var existingForecasts = await _context.SolarForecasts
                    .Where(f => f.HourStart >= tomorrowStart && f.HourStart < tomorrowEnd)
                    .ToListAsync();
                if (existingForecasts.Count > 0)
                {
                    _context.SolarForecasts.RemoveRange(existingForecasts);
                    await _context.SaveChangesAsync();
                }
            }

            // Step 1: Train ML models on historical data
            _logger.LogInformation("Training ML models...");
            await TrainModelsAsync();

            // Step 2: Seed tomorrow's forecast data
            _logger.LogInformation("Seeding tomorrow's data for {Date}...", tomorrow);
            await _seedJob.RunAsync(tomorrow);

            // Step 3: Gather all inputs
            _logger.LogInformation("Running dispatch engine for {Date}...", tomorrow);
            var input = await GatherEngineInputAsync(tomorrow);

            // Step 4: Run the dispatch engine
            var engine = new DispatchEngine(_degradationPredictor);
            var recommendation = engine.Run(input, tomorrow);

            // Step 5: Save recommendation
            _context.AiRecommendations.Add(recommendation);
            await _context.SaveChangesAsync();

            status.Status = "Completed";
            status.CompletedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Engine run completed. Recommendation saved for {Date}. Capture: €{Capture:F0}",
                tomorrow, recommendation.EstimatedCaptureEur);

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Engine run failed for {Date}", tomorrow);
            status.Status = "Failed";
            status.CompletedAt = DateTimeOffset.UtcNow;
            status.Error = ex.Message;
            await _context.SaveChangesAsync();
            return status;
        }
    }

    private async Task TrainModelsAsync()
    {
        // Train SolarProductionForecaster on D-09 + D-06 + D-08
        if (!_solarForecaster.IsTrained)
        {
            var installations = await _context.SolarInstallations.ToListAsync();
            var productions = await _context.SolarProductions.ToListAsync();
            var weather = await _context.WeatherForecasts.ToListAsync();

            var weatherLookup = weather
                .GroupBy(w => (w.SiteId, w.HourStart.Date, w.HourStart.Hour))
                .ToDictionary(g => g.Key, g => g.First());

            var trainingData = new List<SolarProductionInput>();
            foreach (var prod in productions)
            {
                var inst = installations.FirstOrDefault(i => i.Id == prod.SolarInstallationId);
                if (inst == null) continue;

                var key = (inst.SiteId, prod.Timestamp.Date, prod.Timestamp.Hour);
                if (!weatherLookup.TryGetValue(key, out var w)) continue;

                trainingData.Add(new SolarProductionInput
                {
                    SolarIrradianceWm2 = (float)w.SolarIrradianceWm2,
                    CloudCoverPct = (float)w.CloudCoverPct,
                    AmbientTempC = (float)w.AmbientTempC,
                    WindSpeedMs = (float)w.WindSpeedMs,
                    HourOfDay = prod.Timestamp.Hour,
                    Month = prod.Timestamp.Month,
                    CapacityKwp = (float)inst.CapacityKwp,
                    TiltDeg = (float)inst.TiltDeg,
                    AzimuthDeg = (float)inst.AzimuthDeg,
                    Latitude = (float)inst.Latitude,
                    ProductionKw = (float)prod.ProductionKw
                });
            }

            if (trainingData.Count > 100)
            {
                _solarForecaster.Train(trainingData);
                _logger.LogInformation("SolarProductionForecaster trained on {Count} samples", trainingData.Count);
            }
        }

        // Train DegradationPredictor on D-03b trajectories
        var batteries = await _context.Batteries.ToListAsync();
        if (!_degradationPredictor.IsTrained)
        {
            var trainingData = new List<DegradationInput>();
            foreach (var battery in batteries)
            {
                var history = await _context.BatteryHistories
                    .Where(h => h.BatteryId == battery.Id)
                    .OrderBy(h => h.Timestamp)
                    .ToListAsync();

                var days = history.Chunk(96).ToList();
                foreach (var day in days)
                {
                    if (day.Length < 90) continue;

                    double charged = day.Where(h => h.PowerKw > 0).Sum(h => h.PowerKw * 0.25);
                    double dodPct = charged / battery.CapacityKwh * 100;
                    double cRate = (double)battery.PowerRatingKw / battery.CapacityKwh;
                    double timeAbove80 = (double)day.Count(h => h.SocPct > 80) / day.Length;

                    // Label: use deterministic formula as ground truth for training
                    double cost = DegradationPredictor.CalculateFallback(new DegradationInput
                    {
                        DodPct = (float)dodPct,
                        CRate = (float)cRate,
                        AmbientTempC = 12,
                        SohPct = 95,
                        TimeAbove80Pct = (float)timeAbove80,
                        CapacityKwh = battery.CapacityKwh
                    });

                    trainingData.Add(new DegradationInput
                    {
                        DodPct = (float)dodPct,
                        CRate = (float)cRate,
                        AmbientTempC = 12,
                        SohPct = 95,
                        TimeAbove80Pct = (float)timeAbove80,
                        CapacityKwh = battery.CapacityKwh,
                        CycleCostEur = (float)cost
                    });
                }
            }

            if (trainingData.Count > 50)
            {
                _degradationPredictor.Train(trainingData);
                _logger.LogInformation("DegradationPredictor trained on {Count} samples", trainingData.Count);
            }
        }
    }

    private async Task<DispatchEngineInput> GatherEngineInputAsync(DateOnly targetDate)
    {
        var tomorrowStart = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(2));
        var tomorrowEnd = tomorrowStart.AddDays(1);

        var batteries = await _context.Batteries.ToListAsync();
        var telemetry = await _context.BatteryTelemetries.ToListAsync();

        var now = DateTimeOffset.UtcNow;
        var history24h = await _context.BatteryHistories
            .Where(h => h.Timestamp >= now.AddDays(-1))
            .ToListAsync();

        var history7d = await _context.BatteryHistories
            .Where(h => h.Timestamp >= now.AddDays(-7))
            .ToListAsync();

        var tomorrowPrices = await _context.MarketPrices
            .Where(p => p.HourStart >= tomorrowStart && p.HourStart < tomorrowEnd)
            .OrderBy(p => p.HourStart)
            .ToListAsync();

        var tomorrowWeather = await _context.WeatherForecasts
            .Where(w => w.HourStart >= tomorrowStart && w.HourStart < tomorrowEnd)
            .ToListAsync();

        var solarForecasts = await _context.SolarForecasts
            .Where(f => f.HourStart >= tomorrowStart && f.HourStart < tomorrowEnd)
            .ToListAsync();

        var solarInstallations = await _context.SolarInstallations.ToListAsync();

        var config = await _context.EngineConfigs.FirstAsync();

        return new DispatchEngineInput
        {
            Batteries = batteries,
            TodayTelemetry = telemetry,
            TodayHistory24h = history24h,
            TodayHistory7d = history7d,
            TomorrowPrices = tomorrowPrices,
            TomorrowWeather = tomorrowWeather,
            SolarForecasts = solarForecasts,
            SolarInstallations = solarInstallations,
            EngineConfig = config
        };
    }
}

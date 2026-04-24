using BessIntelligence.Api.Models;

namespace BessIntelligence.Api.Engine.Steps;

/// <summary>
/// Step 6: Combine price forecast uncertainty, wind forecast error, solar forecast confidence,
/// and fleet SoH variance into a single confidence percentage.
/// Uses D-07 engine config constants.
/// </summary>
public class ConfidenceScorer
{
    public ConfidenceResult Score(
        EngineConfig config,
        string dispatchScenario,
        List<BatteryAssessment> assessments,
        List<SolarForecast> solarForecasts)
    {
        // Price score: prices are static/seeded, so high confidence (minor residual uncertainty)
        double priceScore = 95;

        // Wind forecast score: max(0, 100 - RMSE_ms × 8)
        double windScore = Math.Max(0, 100 - config.WindRmseMs * 8);

        // Solar forecast score: depends on whether solar duck window is active
        double solarScore;
        if (dispatchScenario is "Wind+Solar" or "Solar only")
        {
            solarScore = Math.Max(0, 100 - config.CloudMaePct * 2);

            // Factor in ML confidence interval width if available
            if (solarForecasts.Count > 0)
            {
                var daytimeForecasts = solarForecasts.Where(f => f.HourStart.Hour >= 9 && f.HourStart.Hour <= 15).ToList();
                if (daytimeForecasts.Count > 0)
                {
                    double avgWidth = daytimeForecasts.Average(f =>
                        f.ForecastProductionKw > 0
                            ? (f.ConfidenceHighKw - f.ConfidenceLowKw) / f.ForecastProductionKw * 100
                            : 50);
                    // Narrower interval = higher confidence
                    double intervalScore = Math.Max(0, 100 - avgWidth * 1.5);
                    solarScore = (solarScore + intervalScore) / 2; // blend formula + ML confidence
                }
            }
        }
        else
        {
            solarScore = 50; // neutral when no solar window active
        }

        // Fleet SoH uniformity: max(0, 100 - σ_SoH × 10)
        double fleetSohScore;
        var eligibleSoh = assessments
            .Where(a => !a.IsFault && a.SohPct >= 75)
            .Select(a => a.SohPct)
            .ToList();

        if (eligibleSoh.Count > 1)
        {
            double mean = eligibleSoh.Average();
            double variance = eligibleSoh.Average(s => (s - mean) * (s - mean));
            double sigma = Math.Sqrt(variance);
            fleetSohScore = Math.Max(0, 100 - sigma * 10);
        }
        else
        {
            fleetSohScore = Math.Max(0, 100 - config.SigmaSohPct * 10);
        }

        // Weighted sum
        double confidence = Math.Round(
            0.40 * priceScore +
            0.25 * windScore +
            0.20 * solarScore +
            0.15 * fleetSohScore, 0);

        confidence = Math.Clamp(confidence, 0, 100);

        return new ConfidenceResult
        {
            PriceScore = priceScore,
            WindScore = windScore,
            SolarScore = solarScore,
            FleetSohScore = fleetSohScore,
            ConfidencePct = confidence
        };
    }
}

public class ConfidenceResult
{
    public double PriceScore { get; set; }
    public double WindScore { get; set; }
    public double SolarScore { get; set; }
    public double FleetSohScore { get; set; }
    public double ConfidencePct { get; set; }
}

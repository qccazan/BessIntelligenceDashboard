using BessIntelligence.Api.Engine.ML;

namespace BessIntelligence.Api.Engine.Steps;

/// <summary>
/// Step 3: Calculate degradation cost per asset per cycle using ML model.
/// Falls back to deterministic formula if ML model is not trained.
/// Accounts for DoD, C-rate, temperature, calendar aging, SoH.
/// </summary>
public class DegradationCostCalculator
{
    private readonly DegradationPredictor _predictor;

    public DegradationCostCalculator(DegradationPredictor predictor)
    {
        _predictor = predictor;
    }

    public double CalculateCycleCost(BatteryAssessment assessment, double chargeEnergyKwh, int windowHours)
    {
        double dodPct = chargeEnergyKwh / assessment.Battery.CapacityKwh * 100;
        double cRate = (double)assessment.Battery.PowerRatingKw / assessment.Battery.CapacityKwh;

        var input = new DegradationInput
        {
            DodPct = (float)dodPct,
            CRate = (float)cRate,
            AmbientTempC = (float)assessment.TemperatureC,
            SohPct = (float)assessment.SohPct,
            TimeAbove80Pct = (float)assessment.TimeAbove80Pct,
            CapacityKwh = assessment.Battery.CapacityKwh
        };

        return _predictor.PredictCycleCost(input);
    }
}

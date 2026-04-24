using Microsoft.ML;
using Microsoft.ML.Data;

namespace BessIntelligence.Api.Engine.ML;

/// <summary>
/// ML.NET FastTree regression that predicts battery cycle degradation cost from operating conditions.
/// Learns from historical SoH trajectories in D-03b.
/// Replaces hardcoded formula (DoD/100)^1.3 with a learned non-linear model.
/// </summary>
public class DegradationPredictor
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private PredictionEngine<DegradationInput, DegradationPrediction>? _predictionEngine;

    public bool IsTrained => _model != null;

    public DegradationPredictor()
    {
        _mlContext = new MLContext(seed: 42);
    }

    public void Train(IEnumerable<DegradationInput> trainingData)
    {
        var data = trainingData.ToList();
        if (data.Count < 50) return; // need enough training samples

        var dataView = _mlContext.Data.LoadFromEnumerable(data);

        var pipeline = _mlContext.Transforms.Concatenate("Features",
                nameof(DegradationInput.DodPct),
                nameof(DegradationInput.CRate),
                nameof(DegradationInput.AmbientTempC),
                nameof(DegradationInput.SohPct),
                nameof(DegradationInput.TimeAbove80Pct),
                nameof(DegradationInput.CapacityKwh))
            .Append(_mlContext.Regression.Trainers.FastTree(
                labelColumnName: nameof(DegradationInput.CycleCostEur),
                featureColumnName: "Features",
                numberOfLeaves: 15,
                numberOfTrees: 80,
                minimumExampleCountPerLeaf: 5,
                learningRate: 0.1));

        _model = pipeline.Fit(dataView);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<DegradationInput, DegradationPrediction>(_model);
    }

    public double PredictCycleCost(DegradationInput input)
    {
        if (_predictionEngine == null)
            return CalculateFallback(input);

        var prediction = _predictionEngine.Predict(input);
        return Math.Max(0, prediction.CycleCostEur);
    }

    /// <summary>Deterministic fallback when model is not trained.</summary>
    public static double CalculateFallback(DegradationInput input)
    {
        const double BaseCycleCostPerKwh = 0.030; // 120 EUR/kWh replacement / 4000 cycles

        double dodFactor = Math.Pow(input.DodPct / 100.0, 1.3);
        double cRateFactor = 1.0 + Math.Max(0, (input.CRate - 0.5)) * 0.30;

        double tempFactor = input.AmbientTempC switch
        {
            < 5 => 1.25,
            > 35 => 1.20,
            _ => 1.00
        };

        double calendarFactor = input.TimeAbove80Pct switch
        {
            < 0.10f => 1.00,
            <= 0.25f => 1.08,
            _ => 1.15
        };

        double sohMultiplier = 100.0 / Math.Max(1, input.SohPct);

        return BaseCycleCostPerKwh * input.CapacityKwh * dodFactor * cRateFactor * tempFactor * calendarFactor * sohMultiplier;
    }
}

public class DegradationInput
{
    [LoadColumn(0)] public float DodPct { get; set; }
    [LoadColumn(1)] public float CRate { get; set; }
    [LoadColumn(2)] public float AmbientTempC { get; set; }
    [LoadColumn(3)] public float SohPct { get; set; }
    [LoadColumn(4)] public float TimeAbove80Pct { get; set; }
    [LoadColumn(5)] public float CapacityKwh { get; set; }
    [LoadColumn(6)] public float CycleCostEur { get; set; }
}

public class DegradationPrediction
{
    [ColumnName("Score")]
    public float CycleCostEur { get; set; }
}

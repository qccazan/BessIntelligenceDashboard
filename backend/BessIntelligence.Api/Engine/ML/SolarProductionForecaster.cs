using Microsoft.ML;
using Microsoft.ML.Data;

namespace BessIntelligence.Api.Engine.ML;

/// <summary>
/// ML.NET FastTree regression model that predicts solar panel production from weather conditions.
/// Trained on historical D-09 (production) + D-06 (weather) + D-08 (panel specs) data.
/// Produces hourly forecasts with confidence intervals for D-10.
/// </summary>
public class SolarProductionForecaster
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private PredictionEngine<SolarProductionInput, SolarProductionPrediction>? _predictionEngine;

    public bool IsTrained => _model != null;

    public SolarProductionForecaster()
    {
        _mlContext = new MLContext(seed: 42);
    }

    public void Train(IEnumerable<SolarProductionInput> trainingData)
    {
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = _mlContext.Transforms.Concatenate("Features",
                nameof(SolarProductionInput.SolarIrradianceWm2),
                nameof(SolarProductionInput.CloudCoverPct),
                nameof(SolarProductionInput.AmbientTempC),
                nameof(SolarProductionInput.WindSpeedMs),
                nameof(SolarProductionInput.HourOfDay),
                nameof(SolarProductionInput.Month),
                nameof(SolarProductionInput.CapacityKwp),
                nameof(SolarProductionInput.TiltDeg),
                nameof(SolarProductionInput.AzimuthDeg),
                nameof(SolarProductionInput.Latitude))
            .Append(_mlContext.Regression.Trainers.FastTree(
                labelColumnName: nameof(SolarProductionInput.ProductionKw),
                featureColumnName: "Features",
                numberOfLeaves: 30,
                numberOfTrees: 150,
                minimumExampleCountPerLeaf: 5,
                learningRate: 0.1));

        _model = pipeline.Fit(dataView);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<SolarProductionInput, SolarProductionPrediction>(_model);
    }

    public SolarProductionPrediction Predict(SolarProductionInput input)
    {
        if (_predictionEngine == null)
            return new SolarProductionPrediction { ProductionKw = 0, ConfidenceLowKw = 0, ConfidenceHighKw = 0 };

        var prediction = _predictionEngine.Predict(input);
        float point = Math.Max(0, prediction.ProductionKw);

        // Confidence interval: ±15% for clear conditions, ±30% for cloudy
        float uncertaintyFactor = input.CloudCoverPct > 60 ? 0.30f : 0.15f;
        prediction.ProductionKw = point;
        prediction.ConfidenceLowKw = Math.Max(0, point * (1 - uncertaintyFactor));
        prediction.ConfidenceHighKw = point * (1 + uncertaintyFactor);

        return prediction;
    }
}

public class SolarProductionInput
{
    [LoadColumn(0)] public float SolarIrradianceWm2 { get; set; }
    [LoadColumn(1)] public float CloudCoverPct { get; set; }
    [LoadColumn(2)] public float AmbientTempC { get; set; }
    [LoadColumn(3)] public float WindSpeedMs { get; set; }
    [LoadColumn(4)] public float HourOfDay { get; set; }
    [LoadColumn(5)] public float Month { get; set; }
    [LoadColumn(6)] public float CapacityKwp { get; set; }
    [LoadColumn(7)] public float TiltDeg { get; set; }
    [LoadColumn(8)] public float AzimuthDeg { get; set; }
    [LoadColumn(9)] public float Latitude { get; set; }
    [LoadColumn(10)] public float ProductionKw { get; set; }
}

public class SolarProductionPrediction
{
    [ColumnName("Score")]
    public float ProductionKw { get; set; }
    public float ConfidenceLowKw { get; set; }
    public float ConfidenceHighKw { get; set; }
}

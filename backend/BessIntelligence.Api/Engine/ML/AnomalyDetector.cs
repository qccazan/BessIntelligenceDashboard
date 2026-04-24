using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;

namespace BessIntelligence.Api.Engine.ML;

/// <summary>
/// ML.NET SSA spike detection on per-battery SoC time-series from D-03b.
/// Detects abnormal SoC patterns (unexpected drops, erratic charging, degradation spikes).
/// Output: anomaly score per battery (0 = normal, higher = more anomalous).
/// </summary>
public class AnomalyDetector
{
    private readonly MLContext _mlContext;
    private readonly Dictionary<int, ITransformer> _models = new();

    public AnomalyDetector()
    {
        _mlContext = new MLContext(seed: 42);
    }

    public void Train(int batteryId, IEnumerable<SocTimePoint> socSeries)
    {
        var data = socSeries.ToList();
        if (data.Count < 96) return; // need at least 1 day of data

        var dataView = _mlContext.Data.LoadFromEnumerable(data);

        // SSA spike detection: 96 intervals/day, detect spikes that deviate from daily pattern
        var pipeline = _mlContext.Transforms.DetectSpikeBySsa(
            outputColumnName: "Prediction",
            inputColumnName: nameof(SocTimePoint.SocPct),
            confidence: 95.0,
            pvalueHistoryLength: 48,     // look back ~12 hours
            trainingWindowSize: 96 * 3,  // 3 days of training window
            seasonalityWindowSize: 96);  // daily seasonality (96 × 15min = 24h)

        _models[batteryId] = pipeline.Fit(dataView);
    }

    public double GetAnomalyScore(int batteryId, IEnumerable<SocTimePoint> recentSeries)
    {
        if (!_models.TryGetValue(batteryId, out var model))
            return 0; // no model trained, assume normal

        var data = recentSeries.ToList();
        if (data.Count == 0) return 0;

        var dataView = _mlContext.Data.LoadFromEnumerable(data);
        var transformedData = model.Transform(dataView);

        var predictions = _mlContext.Data
            .CreateEnumerable<SocAnomalyPrediction>(transformedData, reuseRowObject: false)
            .ToList();

        if (predictions.Count == 0) return 0;

        // Anomaly score = fraction of points flagged as spikes in the last 24 hours
        var last24h = predictions.TakeLast(96).ToList();
        int spikeCount = last24h.Count(p => p.Prediction != null && p.Prediction.Length >= 3 && p.Prediction[0] == 1);

        return (double)spikeCount / last24h.Count;
    }
}

public class SocTimePoint
{
    public float SocPct { get; set; }
}

public class SocAnomalyPrediction
{
    [VectorType(3)]
    public double[]? Prediction { get; set; }
}

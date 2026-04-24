using BessIntelligence.Api.Engine.ML;
using BessIntelligence.Api.Engine.Steps;
using BessIntelligence.Api.Models;

namespace BessIntelligence.Api.Engine;

/// <summary>
/// Orchestrates the 7-step dispatch engine pipeline.
/// Inputs: today's observed data (D-02, D-03a/b) + tomorrow's forecasts (D-04, D-06, D-10).
/// Output: D-05 AI Recommendation with per-battery actions.
/// </summary>
public class DispatchEngine
{
    private readonly PriceSignalProcessor _priceSignalProcessor;
    private readonly BatteryStateAssessor _batteryStateAssessor;
    private readonly DegradationCostCalculator _degradationCostCalculator;
    private readonly WindowOptimiser _windowOptimiser;
    private readonly BatteryAssigner _batteryAssigner;
    private readonly ConfidenceScorer _confidenceScorer;
    private readonly OutputAssembler _outputAssembler;

    public DispatchEngine(
        AnomalyDetector anomalyDetector,
        DegradationPredictor degradationPredictor)
    {
        _priceSignalProcessor = new PriceSignalProcessor();
        _batteryStateAssessor = new BatteryStateAssessor(anomalyDetector);
        _degradationCostCalculator = new DegradationCostCalculator(degradationPredictor);
        _windowOptimiser = new WindowOptimiser(_degradationCostCalculator);
        _batteryAssigner = new BatteryAssigner();
        _confidenceScorer = new ConfidenceScorer();
        _outputAssembler = new OutputAssembler();
    }

    public AiRecommendation Run(DispatchEngineInput input, DateOnly targetDate)
    {
        // Step 1: Price signal processing (window enumeration from static prices)
        var priceSignal = _priceSignalProcessor.Process(
            input.TomorrowPrices,
            input.TomorrowWeather,
            input.SolarForecasts);

        // Step 2: Battery state assessment (safety bounds + ML anomaly detection)
        var batteryState = _batteryStateAssessor.Assess(
            input.Batteries,
            input.TodayTelemetry,
            input.TodayHistory24h,
            input.TodayHistory7d,
            input.SolarForecasts,
            input.SolarInstallations);

        // Step 3 is embedded in Step 4 (degradation calc per window pair per asset)

        // Step 4: Window optimisation (grid-constrained, solar-aware)
        var optimisation = _windowOptimiser.Optimise(
            priceSignal.ValidWindowPairs,
            batteryState.Assessments,
            priceSignal.CorrectedPrices);

        // Step 5: Per-battery assignment (eligibility + staggering)
        var assignments = _batteryAssigner.Assign(
            batteryState.Assessments,
            optimisation);

        // Step 6: Confidence scoring
        var confidence = _confidenceScorer.Score(
            input.EngineConfig,
            priceSignal.DispatchScenario,
            batteryState.Assessments,
            input.SolarForecasts);

        // Step 7: Output assembly (D-05 + explanation)
        var recommendation = _outputAssembler.Assemble(
            optimisation,
            assignments,
            confidence,
            priceSignal,
            input.EngineConfig.Avg30dSpreadMultiplier,
            targetDate);

        return recommendation;
    }
}

public class DispatchEngineInput
{
    public List<Battery> Batteries { get; set; } = [];
    public List<BatteryTelemetry> TodayTelemetry { get; set; } = [];
    public List<BatteryHistory> TodayHistory24h { get; set; } = [];
    public List<BatteryHistory> TodayHistory7d { get; set; } = [];
    public List<MarketPrice> TomorrowPrices { get; set; } = [];
    public List<WeatherForecast> TomorrowWeather { get; set; } = [];
    public List<SolarForecast> SolarForecasts { get; set; } = [];
    public List<SolarInstallation> SolarInstallations { get; set; } = [];
    public EngineConfig EngineConfig { get; set; } = null!;
}

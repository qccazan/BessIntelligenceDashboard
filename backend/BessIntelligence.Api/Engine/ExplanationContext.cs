namespace BessIntelligence.Api.Engine;

public record ExplanationContext(
    double ChargePrice,
    double DischargePrice,
    string ChargeStart,
    string ChargeEnd,
    string DischargeStart,
    string DischargeEnd,
    double SpreadMultiplier,
    double Avg30dSpread,
    string DispatchScenario,
    double AvgWindMs,
    double SolarCoveragePct,
    double EstimatedCapture,
    double ConfidencePct,
    bool IsHold,
    string? HoldReason);

namespace BessIntelligence.Api.DTOs;

public record RecommendationDto(
    int Id,
    DateTimeOffset GeneratedAt,
    string PortfolioAction,
    string ChargeWindowStart,
    string ChargeWindowEnd,
    string DischargeWindowStart,
    string DischargeWindowEnd,
    double ChargePrice,
    double DischargePrice,
    double PriceSpreadMultiplier,
    double Avg30dSpreadMultiplier,
    double ConfidencePct,
    string Explanation,
    double EstimatedCaptureEur,
    List<BatteryActionDto> BatteryActions
);

public record BatteryActionDto(
    int Id,
    int BatteryId,
    string BatteryCode,
    string Action,
    string WindowStart,
    string WindowEnd,
    string Reason
);

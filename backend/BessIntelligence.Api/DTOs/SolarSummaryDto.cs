namespace BessIntelligence.Api.DTOs;

public record SolarSummaryDto(
    double TotalCapacityMwp,
    int TotalPanelCount,
    double YesterdayProductionMwh
);

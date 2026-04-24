namespace BessIntelligence.Api.DTOs;

public record SolarForecastDto(
    int Id,
    string SiteId,
    DateTimeOffset HourStart,
    double ForecastProductionKw,
    double ConfidenceLowKw,
    double ConfidenceHighKw,
    DateTimeOffset GeneratedAt
);

public record SolarProductionDto(
    long Id,
    string SiteId,
    DateTimeOffset Timestamp,
    double ProductionKw,
    double IrradianceWm2,
    double PanelTempC,
    double CapacityFactorPct
);

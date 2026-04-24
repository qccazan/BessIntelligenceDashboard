namespace BessIntelligence.Api.DTOs;

public record FleetAssetDto(
    int Id,
    string Code,
    string SiteName,
    string Location,
    string Mode,
    double PowerKw,
    double SocPct,
    double SohPct,
    double TemperatureC,
    string NextAction,
    string NextActionWindow,
    string? FaultCode,
    string Chemistry,
    int PowerRatingKw,
    int CapacityKwh,
    double DurationH
);

public record FleetSummaryDto(
    double TotalCapacityMwh,
    double AvailableNowMwh,
    double NetPowerMwh,
    int AssetCount,
    List<FleetAssetDto> Assets
);

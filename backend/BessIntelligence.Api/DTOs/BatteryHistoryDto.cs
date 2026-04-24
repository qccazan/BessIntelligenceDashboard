namespace BessIntelligence.Api.DTOs;

public record BatteryHistoryDto(
    DateTimeOffset Timestamp,
    double PowerKw,
    double SocPct
);

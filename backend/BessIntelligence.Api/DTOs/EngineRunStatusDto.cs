namespace BessIntelligence.Api.DTOs;

public record EngineRunStatusDto(
    int Id,
    DateOnly Date,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? Error
);

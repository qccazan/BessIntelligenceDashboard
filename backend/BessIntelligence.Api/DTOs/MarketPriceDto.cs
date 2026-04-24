namespace BessIntelligence.Api.DTOs;

public record MarketPriceDto(
    int Id,
    DateTimeOffset HourStart,
    double PriceEurMwh,
    string Market,
    string Currency
);

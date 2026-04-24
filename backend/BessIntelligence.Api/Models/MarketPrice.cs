namespace BessIntelligence.Api.Models;

public class MarketPrice
{
    public int Id { get; set; }
    public string Market { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public DateTimeOffset HourStart { get; set; }
    public double PriceEurMwh { get; set; }
}

namespace BessIntelligence.Api.Models;

public class AiRecommendation
{
    public int Id { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public string PortfolioAction { get; set; } = string.Empty;
    public string ChargeWindowStart { get; set; } = string.Empty;
    public string ChargeWindowEnd { get; set; } = string.Empty;
    public string DischargeWindowStart { get; set; } = string.Empty;
    public string DischargeWindowEnd { get; set; } = string.Empty;
    public double ChargePrice { get; set; }
    public double DischargePrice { get; set; }
    public double PriceSpreadMultiplier { get; set; }
    public double Avg30dSpreadMultiplier { get; set; }
    public double ConfidencePct { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public double EstimatedCaptureEur { get; set; }

    public ICollection<BatteryAction> BatteryActions { get; set; } = new List<BatteryAction>();
}

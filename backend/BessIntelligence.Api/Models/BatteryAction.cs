namespace BessIntelligence.Api.Models;

public class BatteryAction
{
    public int Id { get; set; }
    public int RecommendationId { get; set; }
    public int BatteryId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string WindowStart { get; set; } = string.Empty;
    public string WindowEnd { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;

    public AiRecommendation Recommendation { get; set; } = null!;
    public Battery Battery { get; set; } = null!;
}

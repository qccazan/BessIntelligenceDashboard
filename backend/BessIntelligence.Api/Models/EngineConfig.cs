namespace BessIntelligence.Api.Models;

public class EngineConfig
{
    public int Id { get; set; }
    public double PriceMaePct { get; set; }
    public double WindRmseMs { get; set; }
    public double CloudMaePct { get; set; }
    public double SigmaSohPct { get; set; }
    public double Avg30dSpreadMultiplier { get; set; }
}

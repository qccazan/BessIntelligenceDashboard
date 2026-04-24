namespace BessIntelligence.Api.Models;

public class SolarForecast
{
    public int Id { get; set; }
    public int SolarInstallationId { get; set; }
    public DateTimeOffset HourStart { get; set; }
    public double ForecastProductionKw { get; set; }
    public double ConfidenceLowKw { get; set; }
    public double ConfidenceHighKw { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }

    public SolarInstallation SolarInstallation { get; set; } = null!;
}

namespace BessIntelligence.Api.Models;

public class WeatherForecast
{
    public int Id { get; set; }
    public string SiteId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTimeOffset HourStart { get; set; }
    public double AmbientTempC { get; set; }
    public double HumidityPct { get; set; }
    public double WindSpeedMs { get; set; }
    public double SolarIrradianceWm2 { get; set; }
    public double CloudCoverPct { get; set; }
    public string Condition { get; set; } = string.Empty;
}

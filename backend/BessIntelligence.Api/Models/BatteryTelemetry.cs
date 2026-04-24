namespace BessIntelligence.Api.Models;

public class BatteryTelemetry
{
    public int Id { get; set; }
    public int BatteryId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public double SocPct { get; set; }
    public double SohPct { get; set; }
    public double PowerKw { get; set; }
    public string Mode { get; set; } = string.Empty;
    public double TemperatureC { get; set; }
    public double VoltageV { get; set; }
    public double CurrentA { get; set; }
    public string NextAction { get; set; } = string.Empty;
    public string NextActionWindow { get; set; } = string.Empty;
    public string? FaultCode { get; set; }

    public Battery Battery { get; set; } = null!;
}

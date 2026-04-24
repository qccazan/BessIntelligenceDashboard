namespace BessIntelligence.Api.Models;

public class BatteryHistory
{
    public long Id { get; set; }
    public int BatteryId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public double PowerKw { get; set; }
    public double SocPct { get; set; }

    public Battery Battery { get; set; } = null!;
}

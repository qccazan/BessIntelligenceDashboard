namespace BessIntelligence.Api.Models;

public class SolarProduction
{
    public long Id { get; set; }
    public int SolarInstallationId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public double ProductionKw { get; set; }
    public double IrradianceWm2 { get; set; }
    public double PanelTempC { get; set; }
    public double CapacityFactorPct { get; set; }

    public SolarInstallation SolarInstallation { get; set; } = null!;
}

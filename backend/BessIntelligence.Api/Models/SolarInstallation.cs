namespace BessIntelligence.Api.Models;

public class SolarInstallation
{
    public int Id { get; set; }
    public string SiteId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double CapacityKwp { get; set; }
    public int PanelCount { get; set; }
    public string PanelType { get; set; } = string.Empty;
    public double TiltDeg { get; set; }
    public double AzimuthDeg { get; set; }
    public DateTime CommissionedDate { get; set; }
}

namespace BessIntelligence.Api.Models;

public class Battery
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Chemistry { get; set; } = string.Empty;
    public int PowerRatingKw { get; set; }
    public int CapacityKwh { get; set; }
    public double DurationH { get; set; }
    public DateTime CommissionedDate { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string BatteryModel { get; set; } = string.Empty;
    public string WiththegridNodeId { get; set; } = string.Empty;
}

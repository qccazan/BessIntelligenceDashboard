using BessIntelligence.Api.Engine.ML;
using BessIntelligence.Api.Models;

namespace BessIntelligence.Api.Engine.Steps;

/// <summary>
/// Step 2: Read today's D-02, enforce LFP safety bounds, compute available/dispatchable energy,
/// run anomaly detection, classify dispatch scenario, compute per-site grid headroom.
/// </summary>
public class BatteryStateAssessor
{
    private const double ChargeCeiling = 90.0;
    private const double DischargeFloor = 15.0;
    private const double EmergencyHold = 10.0;
    private const double Efficiency = 0.95;

    private readonly AnomalyDetector _anomalyDetector;

    public BatteryStateAssessor(AnomalyDetector anomalyDetector)
    {
        _anomalyDetector = anomalyDetector;
    }

    public BatteryStateResult Assess(
        List<Battery> batteries,
        List<BatteryTelemetry> telemetry,
        List<BatteryHistory> history24h,
        List<BatteryHistory> history7d,
        List<SolarForecast> solarForecasts,
        List<SolarInstallation> solarInstallations)
    {
        var assessments = new List<BatteryAssessment>();

        foreach (var battery in batteries)
        {
            var tel = telemetry.FirstOrDefault(t => t.BatteryId == battery.Id);
            if (tel == null) continue;

            double soc = tel.SocPct;
            double soh = tel.SohPct;
            double temp = tel.TemperatureC;
            string mode = tel.Mode;

            // Available and dispatchable energy
            double availableToChargeKwh = (ChargeCeiling - soc) / 100.0 * battery.CapacityKwh;
            double dispatchableKwh = (soc - DischargeFloor) / 100.0 * battery.CapacityKwh;
            availableToChargeKwh = Math.Max(0, availableToChargeKwh);
            dispatchableKwh = Math.Max(0, dispatchableKwh);

            // Emergency hold check
            bool isEmergencyHold = soc < EmergencyHold;

            // 7-day average DoD from D-03b
            var batteryHistory7d = history7d.Where(h => h.BatteryId == battery.Id).OrderBy(h => h.Timestamp).ToList();
            double avgDod7d = ComputeAverageDoD(batteryHistory7d, battery.CapacityKwh);

            // Time above 80% SoC in last 24h from D-03a
            var batteryHistory24h = history24h.Where(h => h.BatteryId == battery.Id).ToList();
            double timeAbove80Pct = batteryHistory24h.Count > 0
                ? (double)batteryHistory24h.Count(h => h.SocPct > 80) / batteryHistory24h.Count
                : 0;

            // ML anomaly score
            var socSeries = batteryHistory7d.Select(h => new SocTimePoint { SocPct = (float)h.SocPct });
            double anomalyScore = _anomalyDetector.GetAnomalyScore(battery.Id, socSeries);

            // Per-site grid headroom: grid_connection - solar_forecast per hour
            var siteInstallation = solarInstallations.FirstOrDefault(s => s.SiteId == battery.SiteName);
            var siteSolarForecasts = siteInstallation != null
                ? solarForecasts.Where(f => f.SolarInstallationId == siteInstallation.Id).ToList()
                : new List<SolarForecast>();

            assessments.Add(new BatteryAssessment
            {
                Battery = battery,
                Telemetry = tel,
                SocPct = soc,
                SohPct = soh,
                TemperatureC = temp,
                Mode = mode,
                AvailableToChargeKwh = availableToChargeKwh,
                DispatchableKwh = dispatchableKwh,
                IsEmergencyHold = isEmergencyHold,
                IsFault = mode == "fault",
                AvgDod7d = avgDod7d,
                TimeAbove80Pct = timeAbove80Pct,
                AnomalyScore = anomalyScore,
                GridConnectionKw = battery.GridConnectionKw,
                SiteSolarForecasts = siteSolarForecasts
            });
        }

        return new BatteryStateResult { Assessments = assessments };
    }

    private static double ComputeAverageDoD(List<BatteryHistory> history, int capacityKwh)
    {
        if (history.Count < 96) return 40; // default assumption

        // Split into daily chunks of 96 intervals
        var days = history.Chunk(96).ToList();
        double totalDoD = 0;
        int dayCount = 0;

        foreach (var day in days)
        {
            if (day.Length < 90) continue; // skip incomplete days

            // DoD = total charged energy / capacity × 100
            double charged = day.Where(h => h.PowerKw > 0).Sum(h => h.PowerKw * 0.25); // kWh
            double dod = charged / capacityKwh * 100;
            totalDoD += dod;
            dayCount++;
        }

        return dayCount > 0 ? totalDoD / dayCount : 40;
    }
}

public class BatteryStateResult
{
    public List<BatteryAssessment> Assessments { get; set; } = [];
}

public class BatteryAssessment
{
    public Battery Battery { get; set; } = null!;
    public BatteryTelemetry Telemetry { get; set; } = null!;
    public double SocPct { get; set; }
    public double SohPct { get; set; }
    public double TemperatureC { get; set; }
    public string Mode { get; set; } = string.Empty;
    public double AvailableToChargeKwh { get; set; }
    public double DispatchableKwh { get; set; }
    public bool IsEmergencyHold { get; set; }
    public bool IsFault { get; set; }
    public double AvgDod7d { get; set; }
    public double TimeAbove80Pct { get; set; }
    public double AnomalyScore { get; set; }
    public double GridConnectionKw { get; set; }
    public List<SolarForecast> SiteSolarForecasts { get; set; } = [];
}

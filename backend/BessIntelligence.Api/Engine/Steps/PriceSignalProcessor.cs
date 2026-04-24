using BessIntelligence.Api.Models;

namespace BessIntelligence.Api.Engine.Steps;

/// <summary>
/// Step 1: Load tomorrow's D-04 prices (static/seeded),
/// enumerate candidate 1–4h charge/discharge windows, filter by 40€ spread,
/// detect solar duck window.
/// </summary>
public class PriceSignalProcessor
{

    public PriceSignalResult Process(
        List<MarketPrice> tomorrowPrices,
        List<WeatherForecast> tomorrowWeather,
        List<SolarForecast> solarForecasts)
    {
        if (tomorrowPrices.Count == 0)
            return PriceSignalResult.Empty;

        var orderedPrices = tomorrowPrices.OrderBy(p => p.HourStart).ToList();

        // Build hourly price list with weather context (prices are static/seeded)
        var hourlyPrices = new List<HourlyPrice>();
        foreach (var price in orderedPrices)
        {
            int hour = price.HourStart.Hour;

            // Average weather across all sites for this hour
            var hourWeather = tomorrowWeather.Where(w => w.HourStart.Hour == hour).ToList();
            float avgGhi = hourWeather.Count > 0 ? (float)hourWeather.Average(w => w.SolarIrradianceWm2) : 0;
            float avgWind = hourWeather.Count > 0 ? (float)hourWeather.Average(w => w.WindSpeedMs) : 0;

            // Total solar production forecast for this hour
            float totalSolarKw = solarForecasts
                .Where(f => f.HourStart.Hour == hour)
                .Sum(f => (float)f.ForecastProductionKw);

            hourlyPrices.Add(new HourlyPrice
            {
                Hour = hour,
                HourStart = price.HourStart,
                RawPriceEurMwh = price.PriceEurMwh,
                AdjustedPriceEurMwh = price.PriceEurMwh, // prices are static, no correction
                AvgGhi = avgGhi,
                AvgWindSpeedMs = avgWind,
                TotalSolarProductionKw = totalSolarKw
            });
        }

        // Enumerate candidate windows (1–4 hours)
        var chargeWindows = new List<CandidateWindow>();
        var dischargeWindows = new List<CandidateWindow>();

        for (int length = 1; length <= 4; length++)
        {
            for (int start = 0; start <= hourlyPrices.Count - length; start++)
            {
                var windowPrices = hourlyPrices.Skip(start).Take(length).ToList();
                double avgPrice = windowPrices.Average(p => p.AdjustedPriceEurMwh);
                double avgSolar = windowPrices.Average(p => p.TotalSolarProductionKw);

                var window = new CandidateWindow
                {
                    StartHour = windowPrices[0].Hour,
                    EndHour = windowPrices[^1].Hour + 1,
                    HourStart = windowPrices[0].HourStart,
                    HourEnd = windowPrices[^1].HourStart.AddHours(1),
                    LengthHours = length,
                    AvgPriceEurMwh = avgPrice,
                    AvgSolarProductionKw = avgSolar,
                    Source = "market"
                };

                chargeWindows.Add(window);
                dischargeWindows.Add(window);
            }
        }

        // Filter valid window pairs: discharge must start ≥ 1h after charge ends, spread ≥ 40 EUR/MWh
        var validPairs = new List<WindowPair>();
        foreach (var charge in chargeWindows)
        {
            foreach (var discharge in dischargeWindows)
            {
                if (discharge.StartHour < charge.EndHour + 1) continue; // rest constraint
                double spread = discharge.AvgPriceEurMwh - charge.AvgPriceEurMwh;
                if (spread < 40) continue; // minimum spread filter

                validPairs.Add(new WindowPair
                {
                    ChargeWindow = charge,
                    DischargeWindow = discharge,
                    GrossSpreadEurMwh = spread
                });
            }
        }

        // Detect solar duck window (midday charge opportunity on sunny days)
        double medianPrice = hourlyPrices.OrderBy(p => p.AdjustedPriceEurMwh)
            .Skip(hourlyPrices.Count / 2).First().AdjustedPriceEurMwh;

        var middayHours = hourlyPrices.Where(p => p.Hour >= 9 && p.Hour <= 15).ToList();
        double avgMiddayGhi = middayHours.Count > 0 ? middayHours.Average(p => p.AvgGhi) : 0;
        bool solarDuckEligible = avgMiddayGhi > 350;

        CandidateWindow? solarDuckWindow = null;
        if (solarDuckEligible)
        {
            var duckHours = middayHours
                .Where(p => p.AdjustedPriceEurMwh < medianPrice)
                .OrderBy(p => p.AdjustedPriceEurMwh)
                .Take(3)
                .OrderBy(p => p.Hour)
                .ToList();

            if (duckHours.Count >= 2)
            {
                double duckAvgPrice = duckHours.Average(p => p.AdjustedPriceEurMwh);
                double duckAvgSolar = duckHours.Average(p => p.TotalSolarProductionKw);

                // Check if spread vs evening discharge clears 40 EUR/MWh
                var eveningPeak = hourlyPrices.Where(p => p.Hour >= 17 && p.Hour <= 20).ToList();
                double eveningAvgPrice = eveningPeak.Count > 0 ? eveningPeak.Average(p => p.AdjustedPriceEurMwh) : 0;

                if (eveningAvgPrice - duckAvgPrice >= 40)
                {
                    solarDuckWindow = new CandidateWindow
                    {
                        StartHour = duckHours[0].Hour,
                        EndHour = duckHours[^1].Hour + 1,
                        HourStart = duckHours[0].HourStart,
                        HourEnd = duckHours[^1].HourStart.AddHours(1),
                        LengthHours = duckHours.Count,
                        AvgPriceEurMwh = duckAvgPrice,
                        AvgSolarProductionKw = duckAvgSolar,
                        Source = "solar_duck"
                    };

                    // Add duck window pairs with evening discharge windows
                    foreach (var discharge in dischargeWindows.Where(d => d.StartHour >= solarDuckWindow.EndHour + 1))
                    {
                        double spread = discharge.AvgPriceEurMwh - duckAvgPrice;
                        if (spread >= 40)
                        {
                            validPairs.Add(new WindowPair
                            {
                                ChargeWindow = solarDuckWindow,
                                DischargeWindow = discharge,
                                GrossSpreadEurMwh = spread
                            });
                        }
                    }
                }
            }
        }

        // Classify dispatch scenario
        double avgPortfolioWind = hourlyPrices.Average(p => p.AvgWindSpeedMs);
        string dispatchScenario;
        if (avgPortfolioWind > 6 && avgMiddayGhi > 350)
            dispatchScenario = "Wind+Solar";
        else if (avgPortfolioWind > 6)
            dispatchScenario = "Wind only";
        else if (avgMiddayGhi > 350)
            dispatchScenario = "Solar only";
        else
            dispatchScenario = "Flat/Hold";

        return new PriceSignalResult
        {
            CorrectedPrices = hourlyPrices,
            ValidWindowPairs = validPairs.OrderByDescending(p => p.GrossSpreadEurMwh).ToList(),
            SolarDuckWindow = solarDuckWindow,
            DispatchScenario = dispatchScenario,
            AvgPortfolioWindMs = avgPortfolioWind,
            AvgMiddayGhi = avgMiddayGhi,
            MedianPriceEurMwh = medianPrice
        };
    }
}

// ── Result types ────────────────────────────────────────────────────────

public class PriceSignalResult
{
    public List<HourlyPrice> CorrectedPrices { get; set; } = [];
    public List<WindowPair> ValidWindowPairs { get; set; } = [];
    public CandidateWindow? SolarDuckWindow { get; set; }
    public string DispatchScenario { get; set; } = "Flat/Hold";
    public double AvgPortfolioWindMs { get; set; }
    public double AvgMiddayGhi { get; set; }
    public double MedianPriceEurMwh { get; set; }

    public static PriceSignalResult Empty => new();
}

public class HourlyPrice
{
    public int Hour { get; set; }
    public DateTimeOffset HourStart { get; set; }
    public double RawPriceEurMwh { get; set; }
    public double AdjustedPriceEurMwh { get; set; }
    public float AvgGhi { get; set; }
    public float AvgWindSpeedMs { get; set; }
    public float TotalSolarProductionKw { get; set; }
}

public class CandidateWindow
{
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public DateTimeOffset HourStart { get; set; }
    public DateTimeOffset HourEnd { get; set; }
    public int LengthHours { get; set; }
    public double AvgPriceEurMwh { get; set; }
    public double AvgSolarProductionKw { get; set; }
    public string Source { get; set; } = "market";
}

public class WindowPair
{
    public CandidateWindow ChargeWindow { get; set; } = null!;
    public CandidateWindow DischargeWindow { get; set; } = null!;
    public double GrossSpreadEurMwh { get; set; }
}

using BessIntelligence.Api.Models;

namespace BessIntelligence.Api.Engine.Steps;

/// <summary>
/// Step 7: Assemble the complete D-05 AI Recommendation object with all fields populated,
/// including natural-language explanation from template.
/// </summary>
public class OutputAssembler
{
    public AiRecommendation Assemble(
        WindowOptimisationResult optimisation,
        List<BatteryAssignment> assignments,
        ConfidenceResult confidence,
        PriceSignalResult priceSignal,
        double avg30dSpreadMultiplier)
    {
        var now = DateTimeOffset.UtcNow;

        // Derive portfolio action from majority assignment
        var actionCounts = assignments
            .GroupBy(a => a.Action)
            .OrderByDescending(g => g.Count())
            .ToList();
        string portfolioAction = optimisation.IsHold
            ? "Hold"
            : DerivePortfolioAction(actionCounts);

        string chargeStart = "\u2014";
        string chargeEnd = "\u2014";
        string dischargeStart = "\u2014";
        string dischargeEnd = "\u2014";
        double chargePrice = 0;
        double dischargePrice = 0;
        double spreadMultiplier = 0;

        if (optimisation.OptimalPair != null)
        {
            var pair = optimisation.OptimalPair;
            chargeStart = pair.ChargeWindow.HourStart.ToString("HH:mm");
            chargeEnd = pair.ChargeWindow.HourEnd.ToString("HH:mm");
            dischargeStart = pair.DischargeWindow.HourStart.ToString("HH:mm");
            dischargeEnd = pair.DischargeWindow.HourEnd.ToString("HH:mm");
            chargePrice = Math.Round(pair.ChargeWindow.AvgPriceEurMwh, 1);
            dischargePrice = Math.Round(pair.DischargeWindow.AvgPriceEurMwh, 1);
            spreadMultiplier = chargePrice > 0 ? Math.Round(dischargePrice / chargePrice, 1) : 0;
        }

        // Estimated capture
        double estimatedCapture = optimisation.AssetScores
            .Where(s => s.NetBenefitEur > 0)
            .Sum(s => s.NetBenefitEur);

        // Solar coverage stats
        double totalSolarChargeKwh = optimisation.AssetScores.Sum(s => s.SolarChargeKwh);
        double totalChargeKwh = optimisation.AssetScores.Sum(s => s.ChargeEnergyKwh);
        double solarCoveragePct = totalChargeKwh > 0 ? totalSolarChargeKwh / totalChargeKwh * 100 : 0;

        // Generate explanation
        string explanation = GenerateExplanation(
            chargePrice, dischargePrice, chargeStart, chargeEnd, dischargeStart, dischargeEnd,
            spreadMultiplier, avg30dSpreadMultiplier,
            priceSignal.DispatchScenario, priceSignal.AvgPortfolioWindMs,
            solarCoveragePct, estimatedCapture,
            optimisation.IsHold, optimisation.HoldReason);

        var recommendation = new AiRecommendation
        {
            GeneratedAt = now,
            PortfolioAction = portfolioAction,
            ChargeWindowStart = chargeStart,
            ChargeWindowEnd = chargeEnd,
            DischargeWindowStart = dischargeStart,
            DischargeWindowEnd = dischargeEnd,
            ChargePrice = chargePrice,
            DischargePrice = dischargePrice,
            PriceSpreadMultiplier = spreadMultiplier,
            Avg30dSpreadMultiplier = avg30dSpreadMultiplier,
            ConfidencePct = confidence.ConfidencePct,
            Explanation = explanation,
            EstimatedCaptureEur = Math.Round(estimatedCapture, 2)
        };

        // Build battery actions
        foreach (var assignment in assignments)
        {
            recommendation.BatteryActions.Add(new BatteryAction
            {
                BatteryId = assignment.Battery.Id,
                Action = assignment.Action,
                WindowStart = assignment.WindowStart?.ToString("HH:mm") ?? "\u2014",
                WindowEnd = assignment.WindowEnd?.ToString("HH:mm") ?? "\u2014",
                Reason = assignment.Reason ?? $"Price spread {spreadMultiplier:F1}\u00d7 favourable"
            });
        }

        return recommendation;
    }

    private static string DerivePortfolioAction(List<IGrouping<string, BatteryAssignment>> actionCounts)
    {
        var top = actionCounts.FirstOrDefault();
        if (top == null) return "Hold";

        return top.Key switch
        {
            "Charge" => "Coordinated charge",
            "Discharge" => "Coordinated discharge",
            _ => "Hold"
        };
    }

    private static string GenerateExplanation(
        double chargePrice, double dischargePrice,
        string chargeStart, string chargeEnd, string dischargeStart, string dischargeEnd,
        double spreadMultiplier, double avg30dSpread,
        string dispatchScenario, double avgWind,
        double solarCoveragePct, double estimatedCapture,
        bool isHold, string? holdReason)
    {
        if (isHold)
        {
            return $"Hold recommended. {holdReason ?? "Insufficient price spread for profitable dispatch."} " +
                   $"The fleet is held to protect battery longevity. " +
                   $"Confidence in this assessment remains at the reported level.";
        }

        string windContext = avgWind switch
        {
            > 7 => "high wind output across the North Sea",
            >= 5 => "sustained overnight wind generation",
            _ => "lower overnight demand"
        };

        string spreadComparison = spreadMultiplier switch
        {
            _ when spreadMultiplier > avg30dSpread * 1.3 => "well above",
            _ when spreadMultiplier > avg30dSpread * 1.0 => "above",
            _ => "in line with"
        };

        string solarContext = "";
        if (dispatchScenario is "Wind+Solar" or "Solar only")
        {
            solarContext = $" Solar panels expected to cover {solarCoveragePct:F0}% of charging demand, reducing grid import costs.";
        }

        return $"EPEX SPOT NL overnight prices fall to ~{chargePrice:F0} EUR/MWh between {chargeStart} " +
               $"and {chargeEnd} \u2014 the cheapest window of the next 24 hours, driven by {windContext} " +
               $"\u2014 before recovering to ~{dischargePrice:F0} EUR/MWh during the evening demand ramp at " +
               $"{dischargeStart}\u2013{dischargeEnd}. The {spreadMultiplier:F1}\u00d7 spread is {spreadComparison} " +
               $"the 30-day average of {avg30dSpread:F1}\u00d7.{solarContext} " +
               $"Estimated capture: ~EUR {estimatedCapture:F0} for the coordinated cycle.";
    }
}

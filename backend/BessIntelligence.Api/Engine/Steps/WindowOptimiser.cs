namespace BessIntelligence.Api.Engine.Steps;

/// <summary>
/// Step 4: Score all valid window pairs by net benefit minus degradation cost.
/// Accounts for solar production reducing charge cost.
/// Enforces grid connection constraints: battery+solar cannot exceed grid capacity.
/// Applies 200€ minimum profitability gate.
/// </summary>
public class WindowOptimiser
{
    private const double MinPortfolioNetBenefit = 200.0;
    private const double Efficiency = 0.95;

    private readonly DegradationCostCalculator _degradationCalculator;

    public WindowOptimiser(DegradationCostCalculator degradationCalculator)
    {
        _degradationCalculator = degradationCalculator;
    }

    public WindowOptimisationResult Optimise(
        List<WindowPair> validPairs,
        List<BatteryAssessment> assessments,
        List<HourlyPrice> correctedPrices)
    {
        if (validPairs.Count == 0 || assessments.Count == 0)
            return WindowOptimisationResult.Hold("No valid window pairs found.");

        var eligibleAssets = assessments
            .Where(a => !a.IsFault && !a.IsEmergencyHold && a.SohPct >= 75)
            .ToList();

        if (eligibleAssets.Count == 0)
            return WindowOptimisationResult.Hold("No eligible assets available.");

        WindowPair? bestPair = null;
        double bestScore = double.MinValue;
        List<AssetWindowScore>? bestAssetScores = null;

        foreach (var pair in validPairs)
        {
            var assetScores = new List<AssetWindowScore>();

            foreach (var asset in eligibleAssets)
            {
                int chargeHours = pair.ChargeWindow.LengthHours;
                int dischargeHours = pair.DischargeWindow.LengthHours;

                // Charge energy bounded by available capacity and power rating × window × efficiency
                double chargeEnergyKwh = Math.Min(
                    asset.AvailableToChargeKwh,
                    asset.Battery.PowerRatingKw * chargeHours * Efficiency);

                // Discharge energy bounded by dispatchable capacity and power rating × window × efficiency
                double dischargeEnergyKwh = Math.Min(
                    asset.DispatchableKwh,
                    asset.Battery.PowerRatingKw * dischargeHours * Efficiency);

                // Grid-constrained discharge: account for solar production at site
                double avgSolarDuringDischarge = 0;
                foreach (var sf in asset.SiteSolarForecasts)
                {
                    if (sf.HourStart.Hour >= pair.DischargeWindow.StartHour &&
                        sf.HourStart.Hour < pair.DischargeWindow.EndHour)
                    {
                        avgSolarDuringDischarge += sf.ForecastProductionKw;
                    }
                }
                if (dischargeHours > 0)
                    avgSolarDuringDischarge /= dischargeHours;

                // Site has N batteries sharing the grid connection — use per-battery share
                int siteBatteryCount = eligibleAssets.Count(a => a.Battery.SiteName == asset.Battery.SiteName);
                double perBatteryGridKw = asset.GridConnectionKw; // per-battery grid connection
                double maxDischargeKw = Math.Max(0, perBatteryGridKw - avgSolarDuringDischarge / Math.Max(1, siteBatteryCount));
                double gridConstrainedDischargeKwh = maxDischargeKw * dischargeHours * Efficiency;
                dischargeEnergyKwh = Math.Min(dischargeEnergyKwh, gridConstrainedDischargeKwh);

                // Charge cost: solar reduces what we buy from grid
                double avgSolarDuringCharge = 0;
                foreach (var sf in asset.SiteSolarForecasts)
                {
                    if (sf.HourStart.Hour >= pair.ChargeWindow.StartHour &&
                        sf.HourStart.Hour < pair.ChargeWindow.EndHour)
                    {
                        avgSolarDuringCharge += sf.ForecastProductionKw;
                    }
                }
                if (chargeHours > 0)
                    avgSolarDuringCharge /= chargeHours;

                double solarChargeKwh = avgSolarDuringCharge / Math.Max(1, siteBatteryCount) * chargeHours;
                double gridChargeKwh = Math.Max(0, chargeEnergyKwh - solarChargeKwh);

                // Revenue calculation
                double chargeCostEur = gridChargeKwh * pair.ChargeWindow.AvgPriceEurMwh / 1000.0;
                double dischargeRevenueEur = dischargeEnergyKwh * pair.DischargeWindow.AvgPriceEurMwh / 1000.0;
                double grossRevenueEur = dischargeRevenueEur - chargeCostEur;

                // Degradation cost
                double cycleCostEur = _degradationCalculator.CalculateCycleCost(asset, chargeEnergyKwh, chargeHours);

                double netBenefitEur = grossRevenueEur - cycleCostEur;

                assetScores.Add(new AssetWindowScore
                {
                    Assessment = asset,
                    ChargeEnergyKwh = chargeEnergyKwh,
                    DischargeEnergyKwh = dischargeEnergyKwh,
                    SolarChargeKwh = solarChargeKwh,
                    GridChargeKwh = gridChargeKwh,
                    GrossRevenueEur = grossRevenueEur,
                    CycleCostEur = cycleCostEur,
                    NetBenefitEur = netBenefitEur
                });
            }

            double portfolioNetBenefit = assetScores.Sum(s => s.NetBenefitEur);
            double portfolioCycleCost = assetScores.Sum(s => s.CycleCostEur);
            double score = portfolioCycleCost > 0 ? portfolioNetBenefit / portfolioCycleCost : 0;

            // Tie-breaking: prefer shorter charge window when scores within 5%
            if (bestPair != null && bestScore > 0)
            {
                double scoreDiff = Math.Abs(score - bestScore) / bestScore;
                if (scoreDiff < 0.05 && pair.ChargeWindow.LengthHours < bestPair.ChargeWindow.LengthHours)
                {
                    bestPair = pair;
                    bestScore = score;
                    bestAssetScores = assetScores;
                    continue;
                }
            }

            if (score > bestScore)
            {
                bestPair = pair;
                bestScore = score;
                bestAssetScores = assetScores;
            }
        }

        if (bestPair == null || bestAssetScores == null)
            return WindowOptimisationResult.Hold("No profitable window pair found.");

        double totalNetBenefit = bestAssetScores.Sum(s => s.NetBenefitEur);

        if (totalNetBenefit < MinPortfolioNetBenefit)
        {
            return new WindowOptimisationResult
            {
                IsHold = true,
                HoldReason = $"Portfolio net benefit €{totalNetBenefit:F0} below minimum threshold of €{MinPortfolioNetBenefit:F0}.",
                OptimalPair = bestPair,
                AssetScores = bestAssetScores,
                PortfolioNetBenefitEur = totalNetBenefit,
                PortfolioScore = bestScore
            };
        }

        return new WindowOptimisationResult
        {
            IsHold = false,
            OptimalPair = bestPair,
            AssetScores = bestAssetScores,
            PortfolioNetBenefitEur = totalNetBenefit,
            PortfolioScore = bestScore
        };
    }
}

public class WindowOptimisationResult
{
    public bool IsHold { get; set; }
    public string? HoldReason { get; set; }
    public WindowPair? OptimalPair { get; set; }
    public List<AssetWindowScore> AssetScores { get; set; } = [];
    public double PortfolioNetBenefitEur { get; set; }
    public double PortfolioScore { get; set; }

    public static WindowOptimisationResult Hold(string reason) => new()
    {
        IsHold = true,
        HoldReason = reason
    };
}

public class AssetWindowScore
{
    public BatteryAssessment Assessment { get; set; } = null!;
    public double ChargeEnergyKwh { get; set; }
    public double DischargeEnergyKwh { get; set; }
    public double SolarChargeKwh { get; set; }
    public double GridChargeKwh { get; set; }
    public double GrossRevenueEur { get; set; }
    public double CycleCostEur { get; set; }
    public double NetBenefitEur { get; set; }
}

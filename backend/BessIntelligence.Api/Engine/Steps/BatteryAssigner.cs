using BessIntelligence.Api.Models;

namespace BessIntelligence.Api.Engine.Steps;

/// <summary>
/// Step 5: Assign Charge/Discharge/Hold to each battery based on eligibility rules.
/// Stagger start times in groups of 3, offset by 15 minutes, to reduce grid coincidence.
/// </summary>
public class BatteryAssigner
{
    public List<BatteryAssignment> Assign(
        List<BatteryAssessment> allAssessments,
        WindowOptimisationResult optimisation)
    {
        var assignments = new List<BatteryAssignment>();

        foreach (var assessment in allAssessments)
        {
            // Priority-ordered eligibility rules
            if (assessment.IsFault)
            {
                assignments.Add(MakeHold(assessment, "Fault \u2014 held offline"));
                continue;
            }

            if (assessment.SohPct < 75)
            {
                assignments.Add(MakeHold(assessment, "SoH below dispatch threshold"));
                continue;
            }

            if (assessment.IsEmergencyHold)
            {
                assignments.Add(MakeHold(assessment, "Emergency hold \u2014 SoC critically low"));
                continue;
            }

            if (assessment.AnomalyScore > 0.5)
            {
                assignments.Add(MakeHold(assessment, $"Anomalous behavior detected (score: {assessment.AnomalyScore:F2})"));
                continue;
            }

            if (optimisation.IsHold)
            {
                assignments.Add(MakeHold(assessment, optimisation.HoldReason ?? "Low spread \u2014 hold recommended"));
                continue;
            }

            // Find this asset's score
            var assetScore = optimisation.AssetScores
                .FirstOrDefault(s => s.Assessment.Battery.Id == assessment.Battery.Id);

            if (assetScore == null || assetScore.NetBenefitEur < 0)
            {
                assignments.Add(MakeHold(assessment, "Marginal asset \u2014 held to protect longevity"));
                continue;
            }

            // High SoC: dispatch at peak instead of charging
            if (assessment.SocPct > 85)
            {
                assignments.Add(new BatteryAssignment
                {
                    Battery = assessment.Battery,
                    Action = "Discharge",
                    Reason = "High SoC \u2014 dispatch at peak",
                    NeedsStaggering = true,
                    IsChargeAction = false
                });
                continue;
            }

            // Low SoC: charge with delayed start
            if (assessment.SocPct < 25)
            {
                assignments.Add(new BatteryAssignment
                {
                    Battery = assessment.Battery,
                    Action = "Charge",
                    Reason = "Low SoC \u2014 delayed start",
                    NeedsStaggering = true,
                    IsChargeAction = true,
                    DelayMinutes = 30
                });
                continue;
            }

            // Normal: charge at optimal window
            assignments.Add(new BatteryAssignment
            {
                Battery = assessment.Battery,
                Action = "Charge",
                Reason = null,
                NeedsStaggering = true,
                IsChargeAction = true
            });
        }

        // Stagger start times for active assignments
        if (optimisation.OptimalPair != null)
        {
            StaggerStartTimes(assignments, optimisation);
        }

        return assignments;
    }

    private static void StaggerStartTimes(List<BatteryAssignment> assignments, WindowOptimisationResult optimisation)
    {
        var pair = optimisation.OptimalPair!;

        // Sort charge assignments by available energy descending
        var chargeAssignments = assignments
            .Where(a => a.IsChargeAction && a.NeedsStaggering)
            .OrderByDescending(a =>
            {
                var score = optimisation.AssetScores.FirstOrDefault(s => s.Assessment.Battery.Id == a.Battery.Id);
                return score?.ChargeEnergyKwh ?? 0;
            })
            .ToList();

        for (int i = 0; i < chargeAssignments.Count; i++)
        {
            var a = chargeAssignments[i];
            int groupOffset = (i / 3) * 15; // groups of 3, 15-min offset
            int totalDelay = groupOffset + a.DelayMinutes;

            a.WindowStart = pair.ChargeWindow.HourStart.AddMinutes(totalDelay);
            a.WindowEnd = pair.ChargeWindow.HourEnd;
        }

        // Sort discharge assignments similarly
        var dischargeAssignments = assignments
            .Where(a => a.Action == "Discharge" && a.NeedsStaggering)
            .OrderByDescending(a =>
            {
                var score = optimisation.AssetScores.FirstOrDefault(s => s.Assessment.Battery.Id == a.Battery.Id);
                return score?.DischargeEnergyKwh ?? 0;
            })
            .ToList();

        for (int i = 0; i < dischargeAssignments.Count; i++)
        {
            var a = dischargeAssignments[i];
            int groupOffset = (i / 3) * 15;

            a.WindowStart = pair.DischargeWindow.HourStart.AddMinutes(groupOffset);
            a.WindowEnd = pair.DischargeWindow.HourEnd;
        }

        // Hold assignments: no windows
        foreach (var a in assignments.Where(a => a.Action == "Hold"))
        {
            a.WindowStart = null;
            a.WindowEnd = null;
        }
    }

    private static BatteryAssignment MakeHold(BatteryAssessment assessment, string reason)
    {
        return new BatteryAssignment
        {
            Battery = assessment.Battery,
            Action = "Hold",
            Reason = reason,
            NeedsStaggering = false,
            WindowStart = null,
            WindowEnd = null
        };
    }
}

public class BatteryAssignment
{
    public Battery Battery { get; set; } = null!;
    public string Action { get; set; } = "Hold";
    public string? Reason { get; set; }
    public DateTimeOffset? WindowStart { get; set; }
    public DateTimeOffset? WindowEnd { get; set; }
    public bool NeedsStaggering { get; set; }
    public bool IsChargeAction { get; set; }
    public int DelayMinutes { get; set; }
}

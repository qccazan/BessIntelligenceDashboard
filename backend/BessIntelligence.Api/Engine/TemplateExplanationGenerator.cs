namespace BessIntelligence.Api.Engine;

public class TemplateExplanationGenerator : IExplanationGenerator
{
    public Task<string> GenerateAsync(ExplanationContext ctx)
    {
        if (ctx.IsHold)
        {
            var explanation = $"Hold recommended. {ctx.HoldReason ?? "Insufficient price spread for profitable dispatch."} " +
                              $"The fleet is held to protect battery longevity. " +
                              $"Confidence in this assessment remains at the reported level.";
            return Task.FromResult(explanation);
        }

        string windContext = ctx.AvgWindMs switch
        {
            > 7 => "high wind output across the North Sea",
            >= 5 => "sustained overnight wind generation",
            _ => "lower overnight demand"
        };

        string spreadComparison = ctx.SpreadMultiplier switch
        {
            _ when ctx.SpreadMultiplier > ctx.Avg30dSpread * 1.3 => "well above",
            _ when ctx.SpreadMultiplier > ctx.Avg30dSpread * 1.0 => "above",
            _ => "in line with"
        };

        string solarContext = "";
        if (ctx.DispatchScenario is "Wind+Solar" or "Solar only")
        {
            solarContext = $" Solar panels expected to cover {ctx.SolarCoveragePct:F0}% of charging demand, reducing grid import costs.";
        }

        var result = $"EPEX SPOT NL overnight prices fall to ~{ctx.ChargePrice:F0} EUR/MWh between {ctx.ChargeStart} " +
                     $"and {ctx.ChargeEnd} \u2014 the cheapest window of the next 24 hours, driven by {windContext} " +
                     $"\u2014 before recovering to ~{ctx.DischargePrice:F0} EUR/MWh during the evening demand ramp at " +
                     $"{ctx.DischargeStart}\u2013{ctx.DischargeEnd}. The {ctx.SpreadMultiplier:F1}\u00d7 spread is {spreadComparison} " +
                     $"the 30-day average of {ctx.Avg30dSpread:F1}\u00d7.{solarContext} " +
                     $"Estimated capture: ~EUR {ctx.EstimatedCapture:F0} for the coordinated cycle.";

        return Task.FromResult(result);
    }
}

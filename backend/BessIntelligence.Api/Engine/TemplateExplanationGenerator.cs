namespace BessIntelligence.Api.Engine;

public class TemplateExplanationGenerator : IExplanationGenerator
{
    public Task<string> GenerateAsync(ExplanationContext ctx)
    {
        if (ctx.IsHold)
        {
            var explanation = $"No action recommended right now. " +
                              $"{ctx.HoldReason ?? "The difference between buying and selling prices is too small to make a profit."} " +
                              $"Batteries stay on standby to avoid unnecessary wear.";
            return Task.FromResult(explanation);
        }

        string windReason = ctx.AvgWindMs switch
        {
            > 7 => "strong North Sea winds overnight",
            >= 5 => "steady wind energy production overnight",
            _ => "low electricity usage overnight"
        };

        string spreadComparison = ctx.SpreadMultiplier switch
        {
            _ when ctx.SpreadMultiplier > ctx.Avg30dSpread * 1.3 => $"nearly double",
            _ when ctx.SpreadMultiplier > ctx.Avg30dSpread * 1.0 => $"above",
            _ => "roughly in line with"
        };

        string solarContext = ctx.SolarCoveragePct > 0
            ? $" Solar covers {ctx.SolarCoveragePct:F0}% of the charging energy, cutting grid costs."
            : " Solar covers 0% at that hour, so all cheap power comes from the grid.";

        var result = $"Electricity is cheapest between {ctx.ChargeStart}\u2013{ctx.ChargeEnd} " +
                     $"(\u20AC{ctx.ChargePrice:F0}/MWh) due to {windReason}, " +
                     $"then jumps to \u20AC{ctx.DischargePrice:F0}/MWh during the evening rush at " +
                     $"{ctx.DischargeStart}\u2013{ctx.DischargeEnd} when usage spikes. " +
                     $"That\u2019s a {ctx.SpreadMultiplier:F1}\u00d7 price gap \u2014 " +
                     $"{spreadComparison} the 30-day average of {ctx.Avg30dSpread:F1}\u00d7.{solarContext} " +
                     $"By charging batteries at night and selling that energy back in the evening, " +
                     $"the system earns an estimated \u20AC{ctx.EstimatedCapture:F0} in one cycle.";

        return Task.FromResult(result);
    }
}

namespace BessIntelligence.Api.Engine;

public interface IExplanationGenerator
{
    Task<string> GenerateAsync(ExplanationContext context);
}

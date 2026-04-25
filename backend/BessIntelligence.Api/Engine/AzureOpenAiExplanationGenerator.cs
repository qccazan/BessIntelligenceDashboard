using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace BessIntelligence.Api.Engine;

public class AzureOpenAiExplanationGenerator : IExplanationGenerator
{
    private const string SystemPrompt =
        """
        You are a BESS (Battery Energy Storage System) energy trading analyst writing daily dispatch recommendations for portfolio operators in the Netherlands.

        Given structured dispatch engine data, write a concise 2–3 sentence explanation.
        Rules:
        - Be precise with numbers (prices in EUR/MWh, times in HH:mm, capture in EUR).
        - Mention the charge and discharge windows, the price spread, and estimated capture.
        - If solar coverage is significant (>5%), mention it.
        - If it's a Hold recommendation, explain why briefly.
        - Use a professional, analytical tone. No markdown, no bullet points — plain prose.
        - Do not invent data. Only use the values provided.
        """;

    private readonly ChatClient _chatClient;
    private readonly TemplateExplanationGenerator _fallback = new();
    private readonly ILogger<AzureOpenAiExplanationGenerator> _logger;

    public AzureOpenAiExplanationGenerator(
        AzureOpenAIClient openAiClient,
        string deploymentName,
        ILogger<AzureOpenAiExplanationGenerator> logger)
    {
        _chatClient = openAiClient.GetChatClient(deploymentName);
        _logger = logger;
    }

    public async Task<string> GenerateAsync(ExplanationContext ctx)
    {
        try
        {
            var userMessage = BuildUserMessage(ctx);

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 256,
                Temperature = 0.3f
            };

            var response = await _chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(SystemPrompt),
                    new UserChatMessage(userMessage)
                ],
                options);

            var content = response.Value.Content[0].Text?.Trim();

            if (!string.IsNullOrWhiteSpace(content))
            {
                _logger.LogInformation("Azure OpenAI explanation generated successfully.");
                return content;
            }

            _logger.LogWarning("Azure OpenAI returned empty content, falling back to template.");
            return await _fallback.GenerateAsync(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure OpenAI call failed, falling back to template.");
            return await _fallback.GenerateAsync(ctx);
        }
    }

    private static string BuildUserMessage(ExplanationContext ctx)
    {
        if (ctx.IsHold)
        {
            return $$"""
                Dispatch decision: HOLD
                Reason: {{ctx.HoldReason ?? "Insufficient price spread"}}
                Confidence: {{ctx.ConfidencePct:F0}}%
                """;
        }

        return $$"""
            Market: EPEX SPOT NL
            Charge window: {{ctx.ChargeStart}}–{{ctx.ChargeEnd}} at ~{{ctx.ChargePrice:F0}} EUR/MWh
            Discharge window: {{ctx.DischargeStart}}–{{ctx.DischargeEnd}} at ~{{ctx.DischargePrice:F0}} EUR/MWh
            Price spread: {{ctx.SpreadMultiplier:F1}}× (30-day avg: {{ctx.Avg30dSpread:F1}}×)
            Dispatch scenario: {{ctx.DispatchScenario}}
            Avg wind speed: {{ctx.AvgWindMs:F1}} m/s
            Solar coverage of charge: {{ctx.SolarCoveragePct:F0}}%
            Estimated capture: €{{ctx.EstimatedCapture:F0}}
            Confidence: {{ctx.ConfidencePct:F0}}%
            """;
    }
}

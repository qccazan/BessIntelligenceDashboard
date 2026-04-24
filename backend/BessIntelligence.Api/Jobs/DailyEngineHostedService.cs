using BessIntelligence.Api.Data;

namespace BessIntelligence.Api.Jobs;

/// <summary>
/// Runs the dispatch engine daily at 00:05 UTC to generate tomorrow's recommendation.
/// </summary>
public class DailyEngineHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyEngineHostedService> _logger;

    public DailyEngineHostedService(IServiceScopeFactory scopeFactory, ILogger<DailyEngineHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddMinutes(5); // 00:05 UTC tomorrow
            var delay = nextRun - now;

            _logger.LogInformation("Next engine run scheduled at {NextRun} (in {Delay})", nextRun, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var engineJob = scope.ServiceProvider.GetRequiredService<DailyEngineJob>();
                var status = await engineJob.RunAsync();
                _logger.LogInformation("Daily engine run completed: {Status} for {Date}", status.Status, status.Date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Daily engine run failed.");
            }
        }
    }
}

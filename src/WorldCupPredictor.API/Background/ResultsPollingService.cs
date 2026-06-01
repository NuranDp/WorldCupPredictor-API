using WorldCupPredictor.API.Services;

namespace WorldCupPredictor.API.Background;

/// <summary>
/// Background service that polls ESPN (free, no API key) for World Cup results
/// every N minutes. Configurable via ApiFootball:PollIntervalMinutes in appsettings.
/// </summary>
public class ResultsPollingService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<ResultsPollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = config.GetValue<int>("ApiFootball:PollIntervalMinutes", 30);
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        logger.LogInformation(
            "ResultsPollingService started (ESPN) — polling every {Interval} minutes", intervalMinutes);

        // Initial delay so the app fully starts up before first poll
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<EspnSoccerService>();
                var count = await service.SyncResultsAsync();
                if (count > 0)
                    logger.LogInformation("ESPN polling: updated {Count} match results", count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ResultsPollingService error during sync");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}

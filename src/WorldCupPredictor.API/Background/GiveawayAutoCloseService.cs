using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.Models;

namespace WorldCupPredictor.API.Background;

public class GiveawayAutoCloseService(
    IServiceScopeFactory scopeFactory,
    ILogger<GiveawayAutoCloseService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GiveawayAutoCloseService started — checking every 2 minutes.");

        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var halfTimeThreshold = DateTime.UtcNow.AddMinutes(-45);

                var toClose = await db.Giveaways
                    .Include(g => g.Match)
                    .Where(g => g.Status == GiveawayStatus.Open
                             && g.Match.MatchDate.HasValue
                             && g.Match.MatchDate.Value <= halfTimeThreshold)
                    .ToListAsync(stoppingToken);

                if (toClose.Count > 0)
                {
                    foreach (var giveaway in toClose)
                        giveaway.Status = GiveawayStatus.Closed;

                    await db.SaveChangesAsync(stoppingToken);

                    logger.LogInformation(
                        "Auto-closed {Count} giveaway(s) at half-time.", toClose.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GiveawayAutoCloseService error.");
            }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }
}

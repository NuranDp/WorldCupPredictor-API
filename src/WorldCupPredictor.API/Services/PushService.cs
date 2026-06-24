using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebPush;
using WorldCupPredictor.API.Data;
using ModelPushSubscription = WorldCupPredictor.API.Models.PushSubscription;

namespace WorldCupPredictor.API.Services;

public class PushService(AppDbContext db, IConfiguration config, ILogger<PushService> logger) : IPushService
{
    public async Task SubscribeAsync(int userId, string endpoint, string p256dh, string auth)
    {
        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

        if (existing is not null)
        {
            existing.P256dh = p256dh;
            existing.Auth = auth;
        }
        else
        {
            db.PushSubscriptions.Add(new ModelPushSubscription
            {
                UserId = userId,
                Endpoint = endpoint,
                P256dh = p256dh,
                Auth = auth,
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task UnsubscribeAsync(int userId, string endpoint)
    {
        var sub = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

        if (sub is not null)
        {
            db.PushSubscriptions.Remove(sub);
            await db.SaveChangesAsync();
        }
    }

    public async Task SendGiveawayNotificationAsync(int giveawayId)
    {
        var giveaway = await db.Giveaways
            .Include(g => g.Match).ThenInclude(m => m.HomeTeam)
            .Include(g => g.Match).ThenInclude(m => m.AwayTeam)
            .FirstOrDefaultAsync(g => g.Id == giveawayId);

        if (giveaway is null) return;

        var subscriptions = await db.PushSubscriptions.ToListAsync();
        if (subscriptions.Count == 0) return;

        var publicKey = config["Vapid:PublicKey"]!;
        var privateKey = config["Vapid:PrivateKey"]!;
        var subject = config["Email:FromAddress"] is { } addr ? $"mailto:{addr}" : "mailto:support@predictthechampion.com";

        var homeTeam = giveaway.Match?.HomeTeam?.Name ?? "Home";
        var awayTeam = giveaway.Match?.AwayTeam?.Name ?? "Away";

        var appUrl = config["Email:AppUrl"] ?? "https://predictthechampion.com";
        var payload = JsonSerializer.Serialize(new
        {
            notification = new
            {
                title = "New Giveaway — Win a Prize!",
                body = $"{homeTeam} vs {awayTeam} — predict the score & win: {giveaway.Prize}",
                icon = "/icons/icon-192.png",
                badge = "/icons/icon-72.png",
                data = new { url = $"{appUrl}/giveaway" },
            },
        });

        var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
        var client = new WebPushClient();

        var tasks = subscriptions.Select(async sub =>
        {
            try
            {
                var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSub, payload, vapidDetails);
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone
                                           || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                db.PushSubscriptions.Remove(sub);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Push failed for subscription {Id}", sub.Id);
            }
        });

        await Task.WhenAll(tasks);
        await db.SaveChangesAsync();
    }
}

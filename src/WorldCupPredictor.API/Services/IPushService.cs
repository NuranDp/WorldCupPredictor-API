namespace WorldCupPredictor.API.Services;

public interface IPushService
{
    Task SubscribeAsync(int userId, string endpoint, string p256dh, string auth);
    Task UnsubscribeAsync(int userId, string endpoint);
    Task SendGiveawayNotificationAsync(int giveawayId);
}

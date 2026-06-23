using WorldCupPredictor.API.Models;

namespace WorldCupPredictor.API.Services;

public interface IEmailService
{
    Task<int> SendGiveawayNotificationAsync(Giveaway giveaway);
}

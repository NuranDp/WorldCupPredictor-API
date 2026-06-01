using WorldCupPredictor.API.Models;

namespace WorldCupPredictor.API.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    int? GetUserIdFromExpiredToken(string token);
}

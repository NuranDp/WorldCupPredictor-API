using WorldCupPredictor.API.DTOs;

namespace WorldCupPredictor.API.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(string name, string email, string password, string? phoneNumber);
    Task<AuthResponse> LoginAsync(string email, string password);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task<AuthResponse> GoogleLoginAsync(string idToken);
}

using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.DTOs;
using WorldCupPredictor.API.Models;

namespace WorldCupPredictor.API.Services;

public class AuthService(AppDbContext db, ITokenService tokenService, IConfiguration config) : IAuthService
{
    // In-memory refresh token store; swap for a DB table in a later phase
    private static readonly Dictionary<string, (int UserId, DateTime Expiry)> RefreshTokens = new();

    public async Task<AuthResponse> RegisterAsync(string name, string email, string password, string? phoneNumber)
    {
        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("An account with that email already exists.");

        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            PhoneNumber = phoneNumber,
            CreatedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> GoogleLoginAsync(string idToken)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [config["Google:ClientId"]!],
        };

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch
        {
            throw new UnauthorizedAccessException("Invalid Google token.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject || u.Email == payload.Email);

        if (user is null)
        {
            user = new User
            {
                Name = payload.Name ?? payload.Email,
                Email = payload.Email,
                GoogleId = payload.Subject,
                AvatarUrl = payload.Picture,
                CreatedAt = DateTime.UtcNow,
            };
            db.Users.Add(user);
        }
        else
        {
            // link existing email-password account to Google
            user.GoogleId ??= payload.Subject;
            user.AvatarUrl ??= payload.Picture;
        }

        await db.SaveChangesAsync();
        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        if (!RefreshTokens.TryGetValue(refreshToken, out var entry) || entry.Expiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        RefreshTokens.Remove(refreshToken);

        var user = await db.Users.FindAsync(entry.UserId)
            ?? throw new UnauthorizedAccessException("User not found.");

        return BuildAuthResponse(user);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var access = tokenService.GenerateAccessToken(user);
        var refresh = tokenService.GenerateRefreshToken();

        RefreshTokens[refresh] = (user.Id, DateTime.UtcNow.AddDays(30));

        return new AuthResponse(
            access,
            refresh,
            new UserDto(user.Id, user.Name, user.Email, user.AvatarUrl, user.IsAdmin)
        );
    }
}

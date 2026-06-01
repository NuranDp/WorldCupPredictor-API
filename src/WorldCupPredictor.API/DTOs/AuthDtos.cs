namespace WorldCupPredictor.API.DTOs;

public record RegisterRequest(string Name, string Email, string Password, string? PhoneNumber);

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User
);

public record RefreshTokenRequest(string RefreshToken);

public record GoogleLoginRequest(string Credential);

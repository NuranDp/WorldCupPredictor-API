namespace WorldCupPredictor.API.DTOs;

public record UserDto(
    int Id,
    string Name,
    string Email,
    string? AvatarUrl,
    bool IsAdmin
);

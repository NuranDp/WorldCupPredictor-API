using WorldCupPredictor.API.DTOs;

namespace WorldCupPredictor.API.Services;

public interface IBracketService
{
    Task<BracketDto?> GetBracketAsync(int userId);
    Task<BracketDto?> GetBracketByIdAsync(int bracketId);
    Task<BracketDto?> GetBracketByTokenAsync(string shareToken);
    Task<BracketDto> SaveBracketAsync(int userId, BracketSubmitRequest request);
}

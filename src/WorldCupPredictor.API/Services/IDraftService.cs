using WorldCupPredictor.API.DTOs;

namespace WorldCupPredictor.API.Services;

public interface IDraftService
{
    Task<List<BracketDraftMeta>> ListDraftsAsync(int userId);
    Task<BracketDraftFull?> GetDraftAsync(int userId, int draftId);
    Task<BracketDraftMeta> SaveDraftAsync(int userId, SaveDraftRequest request);
    Task DeleteDraftAsync(int userId, int draftId);
    /// <summary>Copy the draft's picks into the live Bracket (final submission).</summary>
    Task<BracketDto> SubmitDraftAsync(int userId, int draftId);
}

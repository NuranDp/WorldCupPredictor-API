using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.DTOs;
using WorldCupPredictor.API.Models;

namespace WorldCupPredictor.API.Services;

public class DraftService(AppDbContext db, IBracketService bracketService) : IDraftService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const int MaxDraftsPerTier = 3;

    public async Task<List<BracketDraftMeta>> ListDraftsAsync(int userId)
    {
        return await db.BracketDrafts
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d => new BracketDraftMeta(d.Id, d.Name, d.Tier.ToString(), d.CreatedAt, d.UpdatedAt, d.IsSubmittedFinal))
            .ToListAsync();
    }

    public async Task<BracketDraftFull?> GetDraftAsync(int userId, int draftId)
    {
        var draft = await db.BracketDrafts
            .FirstOrDefaultAsync(d => d.Id == draftId && d.UserId == userId);
        if (draft is null) return null;
        return MapToFull(draft);
    }

    public async Task<BracketDraftMeta> SaveDraftAsync(int userId, SaveDraftRequest request)
    {
        BracketDraft draft;

        if (request.Id.HasValue)
        {
            // Update existing
            draft = await db.BracketDrafts
                .FirstOrDefaultAsync(d => d.Id == request.Id.Value && d.UserId == userId)
                ?? throw new KeyNotFoundException("Draft not found.");
        }
        else
        {
            // Create new — enforce max 3 per tier
            if (!Enum.TryParse<BracketTier>(request.Tier, out var tier))
                throw new ArgumentException("Invalid tier.");

            var count = await db.BracketDrafts
                .CountAsync(d => d.UserId == userId && d.Tier == tier);
            if (count >= MaxDraftsPerTier)
                throw new InvalidOperationException($"You already have {MaxDraftsPerTier} drafts for {tier}. Delete one before creating a new draft.");

            draft = new BracketDraft { UserId = userId, CreatedAt = DateTime.UtcNow };
            db.BracketDrafts.Add(draft);
        }

        if (!Enum.TryParse<BracketTier>(request.Tier, out var parsedTier))
            throw new ArgumentException("Invalid tier.");

        draft.Name = request.Name.Trim();
        draft.Tier = parsedTier;
        draft.UpdatedAt = DateTime.UtcNow;
        draft.PicksJson = JsonSerializer.Serialize(new
        {
            groupPicks    = request.GroupPicks,
            knockoutPicks = request.KnockoutPicks,
            best3rdPicks  = request.Best3rdPicks,
        }, Json);

        await db.SaveChangesAsync();
        return new BracketDraftMeta(draft.Id, draft.Name, draft.Tier.ToString(), draft.CreatedAt, draft.UpdatedAt, draft.IsSubmittedFinal);
    }

    public async Task DeleteDraftAsync(int userId, int draftId)
    {
        var draft = await db.BracketDrafts
            .FirstOrDefaultAsync(d => d.Id == draftId && d.UserId == userId)
            ?? throw new KeyNotFoundException("Draft not found.");

        db.BracketDrafts.Remove(draft);
        await db.SaveChangesAsync();
    }

    public async Task<BracketDto> SubmitDraftAsync(int userId, int draftId)
    {
        var draft = await db.BracketDrafts
            .FirstOrDefaultAsync(d => d.Id == draftId && d.UserId == userId)
            ?? throw new KeyNotFoundException("Draft not found.");

        // Clear the flag from any previously submitted draft for this user
        var previouslySubmitted = await db.BracketDrafts
            .Where(d => d.UserId == userId && d.IsSubmittedFinal && d.Id != draftId)
            .ToListAsync();
        foreach (var prev in previouslySubmitted)
            prev.IsSubmittedFinal = false;

        // Mark this draft as the submitted one
        draft.IsSubmittedFinal = true;
        await db.SaveChangesAsync();

        var full = MapToFull(draft);
        var submitRequest = new BracketSubmitRequest(
            full.Tier,
            full.GroupPicks,
            full.KnockoutPicks,
            full.Best3rdPicks
        );

        return await bracketService.SaveBracketAsync(userId, submitRequest);
    }

    private static BracketDraftFull MapToFull(BracketDraft draft)
    {
        var picks = JsonSerializer.Deserialize<DraftPicksShape>(draft.PicksJson, Json)
                    ?? new DraftPicksShape([], [], []);
        return new BracketDraftFull(
            draft.Id, draft.Name, draft.Tier.ToString(),
            draft.CreatedAt, draft.UpdatedAt,
            picks.GroupPicks, picks.KnockoutPicks, picks.Best3rdPicks
        );
    }

    private record DraftPicksShape(
        List<GroupPickSubmitDto> GroupPicks,
        List<KnockoutPickSubmitDto> KnockoutPicks,
        List<Best3rdPickSubmitDto> Best3rdPicks
    );
}

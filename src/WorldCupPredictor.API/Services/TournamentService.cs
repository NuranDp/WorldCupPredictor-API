using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.DTOs;

namespace WorldCupPredictor.API.Services;

public class TournamentService(AppDbContext db) : ITournamentService
{
    public async Task<TournamentConfigDto?> GetConfigAsync()
    {
        var config = await db.TournamentConfigs.FirstOrDefaultAsync(c => c.IsActive);
        if (config is null) return null;

        return new TournamentConfigDto(
            config.StartDate,
            config.BracketLockDate,
            config.IsActive,
            config.Season,
            DateTime.UtcNow >= config.BracketLockDate
        );
    }

    public async Task<List<GroupWithTeamsDto>> GetGroupsWithTeamsAsync()
    {
        var groups = await db.TournamentGroups
            .Include(g => g.Teams)
            .OrderBy(g => g.Name)
            .ToListAsync();

        return groups.Select(g => new GroupWithTeamsDto(
            g.Id,
            g.Name,
            g.Teams
              .OrderBy(t => t.Seeding)
              .Select(t => new TeamDto(t.Id, t.Name, t.FlagUrl, t.FifaCode, t.Seeding, t.FifaRanking))
              .ToList(),
            g.ActualFirstTeamId,
            g.ActualSecondTeamId
        )).ToList();
    }

    public async Task<List<MatchSlotDto>> GetKnockoutSlotsAsync()
    {
        var matches = await db.Matches
            .Where(m => m.SlotNumber != null)
            .OrderBy(m => m.SlotNumber)
            .ToListAsync();

        return matches.Select(m => new MatchSlotDto(
            m.Id,
            m.SlotNumber!.Value,
            m.Round.ToString()
        )).ToList();
    }

    public async Task<List<PlayerDto>> GetPlayersForTeamAsync(int teamId)
    {
        return await db.Players
            .Where(p => p.TeamId == teamId)
            .OrderBy(p => p.Position).ThenBy(p => p.ShirtNumber)
            .Select(p => new PlayerDto(p.Id, p.Name, p.Position, p.ShirtNumber))
            .ToListAsync();
    }
}

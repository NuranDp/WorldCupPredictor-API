using WorldCupPredictor.API.DTOs;

namespace WorldCupPredictor.API.Services;

public interface ITournamentService
{
    Task<TournamentConfigDto?> GetConfigAsync();
    Task<List<GroupWithTeamsDto>> GetGroupsWithTeamsAsync();
    Task<List<MatchSlotDto>> GetKnockoutSlotsAsync();
    Task<List<PlayerDto>> GetPlayersForTeamAsync(int teamId);
}

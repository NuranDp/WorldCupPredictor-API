using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.Services;

namespace WorldCupPredictor.API.Controllers;

[ApiController]
[Route("api")]
public class TournamentController(ITournamentService tournamentService, AppDbContext db) : ControllerBase
{
    [HttpGet("tournament/config")]
    public async Task<IActionResult> GetConfig()
    {
        var config = await tournamentService.GetConfigAsync();
        return config is null ? NotFound() : Ok(config);
    }

    [HttpGet("teams")]
    public async Task<IActionResult> GetTeams()
    {
        var groups = await tournamentService.GetGroupsWithTeamsAsync();
        return Ok(groups);
    }

    [HttpGet("matches")]
    public async Task<IActionResult> GetMatches()
    {
        var slots = await tournamentService.GetKnockoutSlotsAsync();
        return Ok(slots);
    }

    [HttpGet("teams/{teamId}/players")]
    [Authorize]
    public async Task<IActionResult> GetPlayers(int teamId)
    {
        var players = await tournamentService.GetPlayersForTeamAsync(teamId);
        return Ok(players);
    }

    [HttpGet("tournament/actual-best3rd")]
    public async Task<IActionResult> GetActualBest3rd()
    {
        var teamIds = await db.ActualBest3rdQualifiers
            .Select(q => q.TeamId)
            .ToListAsync();
        return Ok(new { teamIds });
    }
}

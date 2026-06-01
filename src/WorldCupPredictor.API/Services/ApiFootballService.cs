using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.Models;

namespace WorldCupPredictor.API.Services;

public class ApiFootballOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://v3.football.api-sports.io";
    public int LeagueId { get; set; } = 1;
    public int Season { get; set; } = 2026;
    public int PollIntervalMinutes { get; set; } = 30;
}

public class ApiFootballService(
    IHttpClientFactory httpFactory,
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<ApiFootballService> logger)
{
    private readonly ApiFootballOptions _opts = config
        .GetSection("ApiFootball")
        .Get<ApiFootballOptions>() ?? new();

    /// <summary>
    /// Fetches finished fixtures from API-Football and updates Match results in the DB.
    /// Returns number of matches updated.
    /// </summary>
    public async Task<int> SyncResultsAsync()
    {
        if (string.IsNullOrWhiteSpace(_opts.ApiKey) || _opts.ApiKey == "YOUR_API_FOOTBALL_KEY")
        {
            logger.LogWarning("ApiFootball ApiKey not configured — skipping sync.");
            return 0;
        }

        var client = httpFactory.CreateClient("ApiFootball");
        var url = $"/fixtures?league={_opts.LeagueId}&season={_opts.Season}&status=FT";

        HttpResponseMessage response;
        try { response = await client.GetAsync(url); }
        catch (Exception ex)
        {
            logger.LogError(ex, "ApiFootball HTTP request failed");
            return 0;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("ApiFootball returned {Status}", response.StatusCode);
            return 0;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var fixtures = doc.RootElement.GetProperty("response");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        int updated = 0;

        foreach (var fixture in fixtures.EnumerateArray())
        {
            try { updated += await ProcessFixtureAsync(db, fixture); }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing fixture");
            }
        }

        if (updated > 0)
        {
            await db.SaveChangesAsync();
            // Trigger score recalculation
            var scorer = scope.ServiceProvider.GetRequiredService<ScoringService>();
            await scorer.RecalculateAllAsync();
            logger.LogInformation("ApiFootball sync: {Count} matches updated, scores recalculated", updated);
        }

        return updated;
    }

    private static async Task<int> ProcessFixtureAsync(AppDbContext db, JsonElement fixture)
    {
        var fixtureObj = fixture.GetProperty("fixture");
        var externalId = fixtureObj.GetProperty("id").GetInt32().ToString();

        var match = await db.Matches
            .FirstOrDefaultAsync(m => m.ExternalApiId == externalId);

        if (match is null) return 0;
        if (match.Status == MatchStatus.Completed) return 0; // already processed

        var goals = fixture.GetProperty("goals");
        var homeScore = goals.GetProperty("home").GetInt32();
        var awayScore = goals.GetProperty("away").GetInt32();

        match.HomeScore = homeScore;
        match.AwayScore = awayScore;
        match.Status = MatchStatus.Completed;

        // Determine winner (handle penalties/extra-time via "winner" field)
        var teams = fixture.GetProperty("teams");
        var homeWinner = teams.GetProperty("home").GetProperty("winner");
        if (!homeWinner.ValueKind.Equals(JsonValueKind.Null))
        {
            if (homeWinner.GetBoolean())
                match.WinnerTeamId = match.HomeTeamId;
            else if (!homeWinner.GetBoolean())
                match.WinnerTeamId = match.AwayTeamId;
        }
        else if (homeScore > awayScore)
            match.WinnerTeamId = match.HomeTeamId;
        else if (awayScore > homeScore)
            match.WinnerTeamId = match.AwayTeamId;

        return 1;
    }
}

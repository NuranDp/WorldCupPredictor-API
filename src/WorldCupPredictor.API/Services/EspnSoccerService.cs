using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.Models;

namespace WorldCupPredictor.API.Services;

/// <summary>
/// Fetches World Cup match results from ESPN's public (no-key) API.
/// Endpoint: https://site.api.espn.com/apis/site/v2/sports/soccer/fifa.world/scoreboard?dates=YYYYMMDD
/// Teams are matched to our DB via FIFA code (team.abbreviation).
/// </summary>
public class EspnSoccerService(
    IHttpClientFactory httpFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<EspnSoccerService> logger)
{
    private const string BaseUrl = "https://site.api.espn.com/apis/site/v2/sports/soccer/fifa.world";

    /// <summary>
    /// Polls ESPN for completed matches over the last N days and updates our DB.
    /// Returns number of matches updated.
    /// </summary>
    public async Task<int> SyncResultsAsync(int lookbackDays = 3)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Build FIFA code → Team ID map
        var fifaCodeMap = await db.Teams
            .ToDictionaryAsync(t => t.FifaCode.ToUpper(), t => t.Id);

        int totalUpdated = 0;

        // Poll each day from lookbackDays ago to today
        for (int i = lookbackDays; i >= 0; i--)
        {
            var date = DateTime.UtcNow.AddDays(-i).ToString("yyyyMMdd");
            int updated = await SyncDateAsync(db, date, fifaCodeMap);
            totalUpdated += updated;
        }

        if (totalUpdated > 0)
        {
            await db.SaveChangesAsync();
            var scorer = scope.ServiceProvider.GetRequiredService<ScoringService>();
            await scorer.RecalculateAllAsync();
            logger.LogInformation("ESPN sync: {Count} matches updated, scores recalculated", totalUpdated);
        }

        return totalUpdated;
    }

    private async Task<int> SyncDateAsync(
        AppDbContext db,
        string date,
        Dictionary<string, int> fifaCodeMap)
    {
        var client = httpFactory.CreateClient("Espn");

        HttpResponseMessage response;
        try { response = await client.GetAsync($"{BaseUrl}/scoreboard?dates={date}"); }
        catch (Exception ex)
        {
            logger.LogError(ex, "ESPN request failed for date {Date}", date);
            return 0;
        }

        if (!response.IsSuccessStatusCode) return 0;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("events", out var events)) return 0;

        int updated = 0;

        foreach (var evt in events.EnumerateArray())
        {
            try { updated += await ProcessEventAsync(db, evt, fifaCodeMap); }
            catch (Exception ex) { logger.LogError(ex, "Error processing ESPN event"); }
        }

        return updated;
    }

    private static async Task<int> ProcessEventAsync(
        AppDbContext db,
        JsonElement evt,
        Dictionary<string, int> fifaCodeMap)
    {
        // Only process completed matches
        if (!evt.TryGetProperty("status", out var status)) return 0;
        var statusType = status.GetProperty("type");
        var completed = statusType.GetProperty("completed").GetBoolean();
        if (!completed) return 0;

        // Get competitors
        var competitions = evt.GetProperty("competitions");
        var comp = competitions[0];
        var competitors = comp.GetProperty("competitors");

        string? homeFifa = null, awayFifa = null;
        int homeScore = 0, awayScore = 0;
        bool homeWinner = false;

        foreach (var c in competitors.EnumerateArray())
        {
            var side = c.GetProperty("homeAway").GetString();
            var abbr = c.GetProperty("team").GetProperty("abbreviation").GetString()?.ToUpper();
            var score = int.TryParse(c.GetProperty("score").GetString(), out var s) ? s : 0;
            var winner = c.GetProperty("winner").GetBoolean();

            if (side == "home") { homeFifa = abbr; homeScore = score; homeWinner = winner; }
            else                { awayFifa = abbr; awayScore = score; }
        }

        if (homeFifa is null || awayFifa is null) return 0;
        if (!fifaCodeMap.TryGetValue(homeFifa, out var homeTeamId)) return 0;
        if (!fifaCodeMap.TryGetValue(awayFifa, out var awayTeamId)) return 0;

        // Find match in DB by home + away team
        var match = await db.Matches.FirstOrDefaultAsync(m =>
            m.HomeTeamId == homeTeamId && m.AwayTeamId == awayTeamId);

        if (match is null) return 0;
        if (match.Status == MatchStatus.Completed) return 0; // already done

        match.HomeScore = homeScore;
        match.AwayScore = awayScore;
        match.Status = MatchStatus.Completed;
        match.WinnerTeamId = homeWinner ? homeTeamId : awayTeamId;

        return 1;
    }
}

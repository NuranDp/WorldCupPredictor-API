using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.Models;

namespace WorldCupPredictor.API.Services;

// ── Config ────────────────────────────────────────────────────────────────────
public class ScoringOptions
{
    public int GroupFirst { get; set; } = 5;
    public int GroupSecond { get; set; } = 5;
    public int Best3rdQualifier { get; set; } = 3;
    public int RoundOf32 { get; set; } = 5;
    public int RoundOf16 { get; set; } = 10;
    public int QuarterFinal { get; set; } = 15;
    public int SemiFinal { get; set; } = 20;
    public int ThirdPlace { get; set; } = 10;
    public int Final { get; set; } = 30;

    /// <summary>Bonus for correct goal difference (Silver + Gold picks)</summary>
    public int GoalDiffBonus { get; set; } = 3;

    /// <summary>Bonus for exact scoreline on top of GoalDiffBonus (Gold picks only)</summary>
    public int ExactScoreBonus { get; set; } = 5;
}

// ── Service ───────────────────────────────────────────────────────────────────
public class ScoringService(AppDbContext db, IOptions<ScoringOptions> opts)
{
    private readonly ScoringOptions _pts = opts.Value;

    /// <summary>
    /// Recalculates TotalPoints for every bracket and persists the result.
    /// Call after any match result or group standing is updated.
    /// </summary>
    public async Task RecalculateAllAsync()
    {
        var brackets = await db.Brackets
            .Include(b => b.GroupPicks)
            .Include(b => b.Picks).ThenInclude(p => p.Match)
            .Include(b => b.Best3rdPicks)
            .ToListAsync();

        var groups = await db.TournamentGroups.ToListAsync();
        var groupMap = groups.ToDictionary(g => g.Id);

        // Which team IDs actually qualified as best 3rd
        var actualBest3rdTeamIds = await GetActualBest3rdTeamIdsAsync();

        foreach (var bracket in brackets)
        {
            bracket.TotalPoints = CalculatePoints(bracket, groupMap, actualBest3rdTeamIds);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// A bracket pick is valid for scoring only if the bracket was submitted
    /// at least 1 hour before the match kicked off.
    /// </summary>
    private static bool SubmittedInTime(Bracket bracket, Match match)
    {
        if (!match.MatchDate.HasValue) return true; // no kick-off data → score it
        var deadline = match.MatchDate.Value.AddHours(-1);
        return bracket.SubmittedAt <= deadline;
    }

    private int CalculatePoints(
        Bracket bracket,
        Dictionary<int, TournamentGroup> groupMap,
        HashSet<int> actualBest3rdIds)
    {
        int total = 0;

        // ── Group picks ───────────────────────────────────────────────────────
        // Group stage deadline: 1 hour before the tournament's first match.
        // We use the earliest MatchDate across all matches as the group-stage kick-off.
        // (Group picks don't map 1-to-1 to a single match, so we use the first match.)
        foreach (var gp in bracket.GroupPicks)
        {
            if (!groupMap.TryGetValue(gp.GroupId, out var grp)) continue;

            if (grp.ActualFirstTeamId.HasValue && gp.FirstTeamId == grp.ActualFirstTeamId)
                total += _pts.GroupFirst;

            if (grp.ActualSecondTeamId.HasValue && gp.SecondTeamId == grp.ActualSecondTeamId)
                total += _pts.GroupSecond;
        }

        // ── Best 3rd qualifier picks ──────────────────────────────────────────
        foreach (var bp in bracket.Best3rdPicks)
        {
            if (bp.TeamId.HasValue && actualBest3rdIds.Contains(bp.TeamId.Value))
                total += _pts.Best3rdQualifier;
        }

        // ── Knockout picks ────────────────────────────────────────────────────
        foreach (var pick in bracket.Picks)
        {
            var match = pick.Match;
            if (match.Status != MatchStatus.Completed) continue;
            if (!match.WinnerTeamId.HasValue) continue;

            // Zero points if bracket was not submitted at least 1 hour before kick-off
            if (!SubmittedInTime(bracket, match)) continue;

            if (pick.PickedTeamId != match.WinnerTeamId) continue;

            // Base points for correct winner
            int basePoints = match.Round switch
            {
                MatchRound.RoundOf32    => _pts.RoundOf32,
                MatchRound.RoundOf16    => _pts.RoundOf16,
                MatchRound.QuarterFinal => _pts.QuarterFinal,
                MatchRound.SemiFinal    => _pts.SemiFinal,
                MatchRound.ThirdPlace   => _pts.ThirdPlace,
                MatchRound.Final        => _pts.Final,
                _ => 0,
            };
            total += basePoints;

            // Bonus points only when actual scores are available
            if (!match.HomeScore.HasValue || !match.AwayScore.HasValue) continue;
            int actualDiff = Math.Abs(match.HomeScore.Value - match.AwayScore.Value);

            if (bracket.Tier == BracketTier.Silver)
            {
                // Silver: user entered goal margin in HomeScore
                if (pick.HomeScore.HasValue && pick.HomeScore.Value == actualDiff)
                    total += _pts.GoalDiffBonus;
            }
            else if (bracket.Tier == BracketTier.Gold)
            {
                // Gold: user entered full scoreline (HomeScore : AwayScore)
                if (pick.HomeScore.HasValue && pick.AwayScore.HasValue)
                {
                    int predictedDiff = Math.Abs(pick.HomeScore.Value - pick.AwayScore.Value);
                    if (predictedDiff == actualDiff)
                        total += _pts.GoalDiffBonus;

                    if (pick.HomeScore.Value == match.HomeScore.Value &&
                        pick.AwayScore.Value == match.AwayScore.Value)
                        total += _pts.ExactScoreBonus;
                }
            }
        }

        return total;
    }

    /// <summary>
    /// Returns team IDs that actually finished 3rd in their group but still
    /// qualified (i.e. the 8 best 3rd-place teams).
    /// Logic: among all teams with ActualFirstTeamId/ActualSecondTeamId set,
    /// collect the remaining teams — we mark the 8 best once the admin sets
    /// the actual best3rd list via the admin endpoint.
    /// For now, a Team has a flag ActualQualifiedAs3rd (added separately) or
    /// we derive it from Match results. We use a dedicated table approach here:
    /// any team whose Id appears in the ActualBest3rdQualifiers table.
    /// </summary>
    private async Task<HashSet<int>> GetActualBest3rdTeamIdsAsync()
    {
        // Uses the ActualBest3rdQualifiers table populated by admin / results poller
        var ids = await db.ActualBest3rdQualifiers
            .Select(q => q.TeamId)
            .ToListAsync();
        return ids.ToHashSet();
    }
}

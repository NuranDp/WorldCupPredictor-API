namespace WorldCupPredictor.API.DTOs;

public record GroupPickDto(
    int GroupId,
    string GroupName,
    int? FirstTeamId,
    int? SecondTeamId
);

public record KnockoutPickDto(
    int MatchId,
    int SlotNumber,
    string Round,
    int? PickedTeamId,
    int? HomeScore,
    int? AwayScore,
    List<int> LineupPlayerIds,
    DateTime? KickOffTime
);

public record Best3rdPickDto(int Rank, int? TeamId);

public record BracketDto(
    int Id,
    string ShareToken,
    bool IsLocked,
    int TotalPoints,
    DateTime SubmittedAt,
    string Tier,
    List<GroupPickDto> GroupPicks,
    List<KnockoutPickDto> KnockoutPicks,
    List<Best3rdPickDto> Best3rdPicks
);

// ── Leaderboard ───────────────────────────────────────────────────────────────

public record LeaderboardEntryDto(
    int Rank,
    int UserId,
    string Name,
    string? AvatarUrl,
    int TotalPoints,
    DateTime? SubmittedAt
);

// ── Requests ──────────────────────────────────────────────────────────────────

public record GroupPickSubmitDto(int GroupId, int? FirstTeamId, int? SecondTeamId);
public record KnockoutPickSubmitDto(int MatchId, int? PickedTeamId, int? HomeScore, int? AwayScore, List<int>? LineupPlayerIds);
public record Best3rdPickSubmitDto(int Rank, int? TeamId);

public record BracketSubmitRequest(
    string? Tier,
    List<GroupPickSubmitDto> GroupPicks,
    List<KnockoutPickSubmitDto> KnockoutPicks,
    List<Best3rdPickSubmitDto> Best3rdPicks
);

public record BracketSetTierRequest(string Tier);

// ── Drafts ────────────────────────────────────────────────────────────────────

public record BracketDraftMeta(
    int Id,
    string Name,
    string Tier,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsSubmittedFinal
);

public record BracketDraftFull(
    int Id,
    string Name,
    string Tier,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<GroupPickSubmitDto> GroupPicks,
    List<KnockoutPickSubmitDto> KnockoutPicks,
    List<Best3rdPickSubmitDto> Best3rdPicks
);

public record SaveDraftRequest(
    int? Id,          // null = create new, int = update existing
    string Name,
    string Tier,
    List<GroupPickSubmitDto> GroupPicks,
    List<KnockoutPickSubmitDto> KnockoutPicks,
    List<Best3rdPickSubmitDto> Best3rdPicks
);

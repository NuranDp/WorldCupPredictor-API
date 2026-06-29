namespace WorldCupPredictor.API.DTOs;

public record TournamentConfigDto(
    DateTime StartDate,
    DateTime BracketLockDate,
    bool IsActive,
    string Season,
    bool IsBracketLocked
);

public record TeamDto(
    int Id,
    string Name,
    string FlagUrl,
    string FifaCode,
    int Seeding,
    int FifaRanking
);

public record GroupWithTeamsDto(
    int Id,
    string Name,
    List<TeamDto> Teams,
    int? ActualFirstTeamId,
    int? ActualSecondTeamId
);

public record MatchSlotDto(
    int Id,
    int SlotNumber,
    string Round
);

public record PlayerDto(int Id, string Name, string Position, int ShirtNumber);

namespace QuizBattle.Domain.Features.Games
{

    public sealed record GameResult(
        Guid GameRoomId,
        GameType GameType,
        int TotalRounds,
        int? WinnerUserId,
        IReadOnlyList<FinalStanding> FinalStandings,
        DateTime? StartedAt,
        DateTime EndedAt);

    public sealed record FinalStanding(
        int Position,
        int UserId,
        string DisplayName,
        string? PhotoUrl,
        int TotalScore,
        string ColorHex);
}

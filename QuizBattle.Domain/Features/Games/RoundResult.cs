namespace QuizBattle.Domain.Features.Games
{

    public sealed record RoundResult(
        int RoundNumber,
        int QuestionId,
        string CorrectOption,
        string CorrectAnswerText,
        IReadOnlyList<PlayerRoundResult> PlayerResults,
        IReadOnlyList<PlayerScore> CurrentStandings);

    public sealed record PlayerRoundResult(
        int UserId,
        string DisplayName,
        string? AnswerGiven,
        TimeSpan? ResponseTime,
        int PointsAwarded,
        bool IsCorrect);

    public sealed record PlayerScore(
        int UserId,
        string DisplayName,
        int TotalScore);
}

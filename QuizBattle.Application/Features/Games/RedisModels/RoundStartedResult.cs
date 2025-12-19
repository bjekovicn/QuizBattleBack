namespace QuizBattle.Application.Features.Games.RedisModels
{
    public sealed record RoundStartedResult(
        GameQuestionDto Question,
        int CurrentRound,
        int TotalRounds,
        long RoundEndsAt);
}

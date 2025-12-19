using QuizBattle.Application.Features.Games.RedisModels;

namespace QuizBattle.Infrastructure.Features.Games.Redis.Scripting.Responses
{
    internal sealed class StartNextRoundResponse : LuaScriptResponse
    {
        public GameQuestionDto? Question { get; init; }
        public long RoundEndsAt { get; init; }
        public int CurrentRound { get; init; }
        public int TotalRounds { get; init; }
    }
}
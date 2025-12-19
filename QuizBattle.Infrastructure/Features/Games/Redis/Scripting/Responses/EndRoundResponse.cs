using QuizBattle.Application.Features.Games;

namespace QuizBattle.Infrastructure.Features.Games.Redis.Scripting.Responses
{
    internal sealed class EndRoundResponse : LuaScriptResponse
    {
        public RoundResultDto? Result { get; init; }
    }
}
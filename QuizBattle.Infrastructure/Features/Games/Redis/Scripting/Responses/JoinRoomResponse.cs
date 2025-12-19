using QuizBattle.Application.Features.Games.RedisModels;

namespace QuizBattle.Infrastructure.Features.Games.Redis.Scripting.Responses
{
    internal sealed class JoinRoomResponse : LuaScriptResponse
    {
        public GamePlayerDto? Player { get; init; }
        public GameRoomDto? Room { get; init; }
    }
}
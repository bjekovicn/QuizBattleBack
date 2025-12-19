using QuizBattle.Application.Features.Games.RedisModels;

namespace QuizBattle.Infrastructure.Features.Games.Redis.Scripting.Responses
{
    internal sealed class CreateRoomResponse : LuaScriptResponse
    {
        public GameRoomDto? Room { get; init; }
    }
}


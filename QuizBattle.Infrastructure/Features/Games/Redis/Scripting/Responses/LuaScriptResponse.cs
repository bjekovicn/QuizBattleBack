using System.Text.Json;

namespace QuizBattle.Infrastructure.Features.Games.Redis.Scripting.Responses
{

    public abstract class LuaScriptResponse
    {
        public bool Success { get; init; }
        public string? Error { get; init; }

        public static JsonSerializerOptions JsonOptions { get; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

}
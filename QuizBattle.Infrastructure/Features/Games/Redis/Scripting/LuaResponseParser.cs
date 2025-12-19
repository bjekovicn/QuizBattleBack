using System.Text.Json;
using QuizBattle.Infrastructure.Features.Games.Redis.Scripting.Responses;

namespace QuizBattle.Infrastructure.Features.Games.Redis.Scripting
{
    internal static class LuaResponseParser
    {
        public static T Parse<T>(string json) where T : LuaScriptResponse
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException($"Empty response from Lua script when parsing type {typeof(T).Name}");
            }

            try
            {
                var response = JsonSerializer.Deserialize<T>(json, LuaScriptResponse.JsonOptions);
                return response ?? throw new InvalidOperationException($"Failed to deserialize Lua script response into type {typeof(T).Name}");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Invalid JSON from Lua script while parsing type {typeof(T).Name}: {json}",
                    ex
                );
            }
        }
    }

}

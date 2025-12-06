using System.Text.Json;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Games;

namespace QuizBattle.Infrastructure.Features.Games.Redis;

/// <summary>
/// Base response wrapper for all Lua scripts.
/// All scripts return JSON in format: {success: true, ...data} or {success: false, error: "ERROR_CODE"}
/// </summary>
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

internal sealed class CreateRoomResponse : LuaScriptResponse
{
    public GameRoomDto? Room { get; init; }
}

internal sealed class JoinRoomResponse : LuaScriptResponse
{
    public GamePlayerDto? Player { get; init; }
    public GameRoomDto? Room { get; init; }
}

internal sealed class SubmitAnswerResponse : LuaScriptResponse
{
    public SubmitAnswerResultDto? Result { get; init; }
}

internal sealed class SubmitAnswerResultDto
{
    public bool Accepted { get; init; }
    public bool AllPlayersAnswered { get; init; }
    public int PlayersAnsweredCount { get; init; }
    public int TotalPlayersCount { get; init; }
}

internal sealed class EndRoundResponse : LuaScriptResponse
{
    public RoundResultDto? Result { get; init; }
}

internal sealed class StartNextRoundResponse : LuaScriptResponse
{
    public GameQuestionDto? Question { get; init; }
    public long RoundEndsAt { get; init; }
    public int CurrentRound { get; init; }
    public int TotalRounds { get; init; }
}

internal sealed class SetPlayerReadyResponse : LuaScriptResponse
{
    public bool AllPlayersReady { get; init; }
}

internal sealed class JoinMatchmakingResponse : LuaScriptResponse
{
    public bool Matched { get; init; }
    public List<MatchmakingPlayerDto>? Players { get; init; }
    public int QueuePosition { get; init; }
}

internal sealed class MatchmakingPlayerDto
{
    public int UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? PhotoUrl { get; init; }
    public long JoinedAt { get; init; }
}

/// <summary>
/// Helper class to parse Lua script responses consistently.
/// </summary>
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

using QuizBattle.Application.Features.Games.Repositories;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;
using QuizBattle.Infrastructure.Features.Games.Redis.Scripting;
using QuizBattle.Infrastructure.Features.Games.Redis.Scripting.Responses;
using StackExchange.Redis;

namespace QuizBattle.Infrastructure.Features.Games.Redis.Repositories;

internal sealed class RedisMatchmakingRepository : IMatchmakingRepository
{
    private readonly IDatabase _redis;
    private readonly LuaScriptExecutor _scriptCaller;
    private readonly TimeSpan _queueTtl = TimeSpan.FromMinutes(5);

    public RedisMatchmakingRepository(
        IConnectionMultiplexer connectionMultiplexer,
        LuaScriptExecutor scriptCaller)
    {
        _redis = connectionMultiplexer.GetDatabase();
        _scriptCaller = scriptCaller;
    }

    private static string QueueKey(GameType gameType, string language) =>
        $"matchmaking:{(int)gameType}:{language}";

    private static string PlayerInfoKey(int userId) =>
        $"matchmaking:player:{userId}";

    private static int GetRequiredPlayers(GameType gameType) => gameType switch
    {
        GameType.RandomDuel => 2,
        GameType.FriendDuel => 2,
        GameType.RandomBattle => 3,
        GameType.FriendBattle => 3,
        _ => 2
    };

    public async Task<Result<MatchmakingResult>> JoinQueueAsync(
        int userId,
        string displayName,
        string? photoUrl,
        GameType gameType,
        string languageCode,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var requiredPlayers = GetRequiredPlayers(gameType);

        var result = await _scriptCaller.EvalAsync(
            "join_matchmaking",
            new RedisKey[] { QueueKey(gameType, languageCode), PlayerInfoKey(userId) },
            new RedisValue[]
            {
                userId.ToString(),
                displayName,
                photoUrl ?? "",
                requiredPlayers,
                now,
                (int)_queueTtl.TotalSeconds
            });

        var response = LuaResponseParser.Parse<JoinMatchmakingResponse>(result.ToString());

        if (!response.Success)
        {
            return Result.Failure<MatchmakingResult>(
                new Error("Matchmaking.Failed", response.Error ?? "Failed to join queue"));
        }

        if (response.Matched && response.Players != null)
        {
            var matchedPlayers = response.Players
                .Select(p => new MatchedPlayerDto(
                    p.UserId,
                    p.DisplayName ?? "Player",
                    string.IsNullOrEmpty(p.PhotoUrl) ? null : p.PhotoUrl))
                .ToList();

            return Result.Success(new MatchmakingResult(
                true,
                null, // Room will be created by the caller
                matchedPlayers));
        }

        return Result.Success(new MatchmakingResult(false, null, null));
    }

    public async Task<Result> LeaveQueueAsync(
        int userId,
        GameType gameType,
        string languageCode,
        CancellationToken ct = default)
    {
        await _redis.SortedSetRemoveAsync(QueueKey(gameType, languageCode), userId.ToString());
        await _redis.KeyDeleteAsync(PlayerInfoKey(userId));

        return Result.Success();
    }

    public async Task<bool> IsInQueueAsync(int userId, CancellationToken ct = default)
    {
        return await _redis.KeyExistsAsync(PlayerInfoKey(userId));
    }
}
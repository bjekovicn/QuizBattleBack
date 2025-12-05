using System.Text.Json;
using QuizBattle.Application.Features.Games;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;
using StackExchange.Redis;

namespace QuizBattle.Infrastructure.Features.Games.Redis
{

    internal sealed class RedisMatchmakingRepository : IMatchmakingRepository
    {
        private readonly IDatabase _redis;
        private readonly TimeSpan _queueTtl = TimeSpan.FromMinutes(5);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public RedisMatchmakingRepository(IConnectionMultiplexer connectionMultiplexer)
        {
            _redis = connectionMultiplexer.GetDatabase();
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

            var result = await _redis.ScriptEvaluateAsync(
                LuaScripts.JoinMatchmaking,
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

            var resultStr = result.ToString();

            if (string.IsNullOrEmpty(resultStr))
            {
                return Result.Failure<MatchmakingResult>(
                    new Error("Matchmaking.Failed", "Failed to join queue."));
            }

            var data = JsonSerializer.Deserialize<JsonElement>(resultStr, JsonOptions);

            if (data.TryGetProperty("matched", out var matched) && matched.GetBoolean())
            {
                var playersJson = data.GetProperty("players").GetRawText();
                var players = JsonSerializer.Deserialize<List<MatchedPlayerInfo>>(playersJson, JsonOptions)!;

                return Result.Success(new MatchmakingResult(
                    true,
                    null, // Room will be created by the caller
                    players.Select(p => new MatchedPlayerDto(
                        p.UserId,
                        p.DisplayName ?? "Player",
                        string.IsNullOrEmpty(p.PhotoUrl) ? null : p.PhotoUrl)).ToList()));
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

        private sealed class MatchedPlayerInfo
        {
            public int UserId { get; set; }
            public string? DisplayName { get; set; }
            public string? PhotoUrl { get; set; }
        }
    }
}

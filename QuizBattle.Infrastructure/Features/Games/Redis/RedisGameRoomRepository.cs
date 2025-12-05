using System.Text.Json;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Games;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;
using StackExchange.Redis;

namespace QuizBattle.Infrastructure.Features.Games.Redis
{

    internal sealed class RedisGameRoomRepository : IGameRoomRepository
    {
        private readonly IDatabase _redis;
        private readonly TimeSpan _roomTtl = TimeSpan.FromHours(2);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public RedisGameRoomRepository(IConnectionMultiplexer connectionMultiplexer)
        {
            _redis = connectionMultiplexer.GetDatabase();
        }

        private static string RoomKey(string roomId) => $"game:room:{roomId}";
        private static string RoomKey(GameRoomId roomId) => RoomKey(roomId.Value.ToString());
        private static string PlayerKey(int userId) => $"game:player:{userId}";
        private const string ActiveRoomsKey = "game:active_rooms";

        public async Task<GameRoomDto?> GetByIdAsync(GameRoomId roomId, CancellationToken ct = default)
        {
            var json = await _redis.StringGetAsync(RoomKey(roomId));
            if (json.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<GameRoomDto>(json!, JsonOptions);
        }

        public async Task<bool> ExistsAsync(GameRoomId roomId, CancellationToken ct = default)
        {
            return await _redis.KeyExistsAsync(RoomKey(roomId));
        }

        public async Task DeleteAsync(GameRoomId roomId, CancellationToken ct = default)
        {
            var room = await GetByIdAsync(roomId, ct);
            if (room is null) return;

            // Remove player mappings
            foreach (var player in room.Players)
            {
                await _redis.KeyDeleteAsync(PlayerKey(player.UserId));
            }

            await _redis.KeyDeleteAsync(RoomKey(roomId));
            await _redis.SetRemoveAsync(ActiveRoomsKey, roomId.Value.ToString());
        }

        public async Task<Result<GameRoomDto>> CreateRoomAsync(
            GameType gameType,
            string languageCode,
            int totalRounds,
            CancellationToken ct = default)
        {
            var roomId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var room = new GameRoomDto
            {
                Id = roomId.ToString(),
                GameType = (int)gameType,
                Status = (int)GameStatus.WaitingForPlayers,
                LanguageCode = languageCode,
                TotalRounds = totalRounds,
                CurrentRound = 0,
                CreatedAt = now,
                Players = new List<GamePlayerDto>(),
                Questions = new List<GameQuestionDto>()
            };

            var roomJson = JsonSerializer.Serialize(room, JsonOptions);

            var result = await _redis.ScriptEvaluateAsync(
                LuaScripts.CreateRoom,
                new RedisKey[] { RoomKey(roomId.ToString()), ActiveRoomsKey },
                new RedisValue[] { roomJson, roomId.ToString(), (int)_roomTtl.TotalSeconds });

            var resultStr = result.ToString();
            if (resultStr == "OK")
            {
                return Result.Success(room);
            }

            return Result.Failure<GameRoomDto>(new Error("Game.CreateFailed", resultStr ?? "Unknown error"));
        }

        public async Task<Result<GamePlayerDto>> JoinRoomAsync(
            GameRoomId roomId,
            int userId,
            string displayName,
            string? photoUrl,
            CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var result = await _redis.ScriptEvaluateAsync(
                LuaScripts.JoinRoom,
                new RedisKey[] { RoomKey(roomId), PlayerKey(userId) },
                new RedisValue[]
                {
                userId,
                displayName,
                photoUrl ?? "",
                now,
                (int)_roomTtl.TotalSeconds
                });

            var resultStr = result.ToString();

            if (resultStr?.StartsWith("{") == true)
            {
                var room = JsonSerializer.Deserialize<GameRoomDto>(resultStr, JsonOptions);
                var player = room?.Players.FirstOrDefault(p => p.UserId == userId);

                if (player is not null)
                {
                    return Result.Success(player);
                }
            }

            return resultStr switch
            {
                "ROOM_NOT_FOUND" => Result.Failure<GamePlayerDto>(Error.GameNotFound),
                "GAME_ALREADY_STARTED" => Result.Failure<GamePlayerDto>(Error.GameAlreadyStarted),
                "ROOM_FULL" => Result.Failure<GamePlayerDto>(Error.GameFull),
                "PLAYER_ALREADY_IN_ROOM" => Result.Failure<GamePlayerDto>(Error.PlayerAlreadyInGame),
                _ => Result.Failure<GamePlayerDto>(new Error("Game.JoinFailed", resultStr ?? "Unknown error"))
            };
        }

        public async Task<Result> LeaveRoomAsync(
            GameRoomId roomId,
            int userId,
            CancellationToken ct = default)
        {
            var room = await GetByIdAsync(roomId, ct);
            if (room is null)
                return Result.Failure(Error.GameNotFound);

            var player = room.Players.FirstOrDefault(p => p.UserId == userId);
            if (player is null)
                return Result.Failure(Error.PlayerNotInGame);

            room.Players.Remove(player);

            // Reassign host if needed
            if (room.HostPlayerId == userId && room.Players.Any())
            {
                room.HostPlayerId = room.Players[0].UserId;
            }

            // Save updated room
            var roomJson = JsonSerializer.Serialize(room, JsonOptions);
            await _redis.StringSetAsync(RoomKey(roomId), roomJson, _roomTtl);

            // Remove player mapping
            await _redis.KeyDeleteAsync(PlayerKey(userId));

            return Result.Success();
        }

        public async Task<Result> SetPlayerReadyAsync(
            GameRoomId roomId,
            int userId,
            bool isReady,
            CancellationToken ct = default)
        {
            var result = await _redis.ScriptEvaluateAsync(
                LuaScripts.SetPlayerReady,
                new RedisKey[] { RoomKey(roomId) },
                new RedisValue[] { userId, isReady.ToString().ToLower() });

            var resultStr = result.ToString();

            if (resultStr?.Contains("success") == true)
            {
                return Result.Success();
            }

            return resultStr switch
            {
                "ROOM_NOT_FOUND" => Result.Failure(Error.GameNotFound),
                "PLAYER_NOT_IN_ROOM" => Result.Failure(Error.PlayerNotInGame),
                _ => Result.Failure(new Error("Game.SetReadyFailed", resultStr ?? "Unknown error"))
            };
        }

        public async Task<Result> SetPlayerConnectedAsync(
            GameRoomId roomId,
            int userId,
            bool isConnected,
            CancellationToken ct = default)
        {
            var room = await GetByIdAsync(roomId, ct);
            if (room is null)
                return Result.Failure(Error.GameNotFound);

            var player = room.Players.FirstOrDefault(p => p.UserId == userId);
            if (player is null)
                return Result.Failure(Error.PlayerNotInGame);

            player.IsConnected = isConnected;

            var roomJson = JsonSerializer.Serialize(room, JsonOptions);
            await _redis.StringSetAsync(RoomKey(roomId), roomJson, _roomTtl);

            return Result.Success();
        }

        public async Task<Result<GameRoomDto>> StartGameAsync(
            GameRoomId roomId,
            List<GameQuestionDto> questions,
            CancellationToken ct = default)
        {
            var room = await GetByIdAsync(roomId, ct);
            if (room is null)
                return Result.Failure<GameRoomDto>(Error.GameNotFound);

            if (room.Status != (int)GameStatus.WaitingForPlayers)
                return Result.Failure<GameRoomDto>(Error.GameAlreadyStarted);

            if (room.Players.Count < GameRoom.MinPlayers)
                return Result.Failure<GameRoomDto>(Error.NotEnoughPlayers);

            room.Status = (int)GameStatus.Starting;
            room.StartedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            room.Questions = questions;

            var roomJson = JsonSerializer.Serialize(room, JsonOptions);
            await _redis.StringSetAsync(RoomKey(roomId), roomJson, _roomTtl);

            return Result.Success(room);
        }

        public async Task<Result<GameQuestionDto>> StartNextRoundAsync(
            GameRoomId roomId,
            CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var roundDurationMs = GameRoom.RoundDurationSeconds * 1000;

            var result = await _redis.ScriptEvaluateAsync(
                LuaScripts.StartNextRound,
                new RedisKey[] { RoomKey(roomId) },
                new RedisValue[] { now, roundDurationMs });

            var resultStr = result.ToString();

            if (resultStr?.StartsWith("{") == true && resultStr.Contains("question"))
            {
                var data = JsonSerializer.Deserialize<JsonElement>(resultStr, JsonOptions);
                var questionJson = data.GetProperty("question").GetRawText();
                var question = JsonSerializer.Deserialize<GameQuestionDto>(questionJson, JsonOptions);

                return Result.Success(question!);
            }

            return resultStr switch
            {
                "ROOM_NOT_FOUND" => Result.Failure<GameQuestionDto>(Error.GameNotFound),
                "INVALID_STATE" => Result.Failure<GameQuestionDto>(new Error("Game.InvalidState", "Cannot start round.")),
                "NO_MORE_ROUNDS" => Result.Failure<GameQuestionDto>(new Error("Game.NoMoreRounds", "All rounds completed.")),
                _ => Result.Failure<GameQuestionDto>(new Error("Game.StartRoundFailed", resultStr ?? "Unknown error"))
            };
        }

        public async Task<Result<SubmitAnswerResult>> SubmitAnswerAsync(
            GameRoomId roomId,
            int userId,
            string answer,
            CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var result = await _redis.ScriptEvaluateAsync(
                LuaScripts.SubmitAnswer,
                new RedisKey[] { RoomKey(roomId) },
                new RedisValue[] { userId, answer, now });

            var resultStr = result.ToString();

            if (resultStr?.StartsWith("{") == true && resultStr.Contains("accepted"))
            {
                var data = JsonSerializer.Deserialize<SubmitAnswerResult>(resultStr, JsonOptions);
                return Result.Success(data!);
            }

            return resultStr switch
            {
                "ROOM_NOT_FOUND" => Result.Failure<SubmitAnswerResult>(Error.GameNotFound),
                "ROUND_NOT_ACTIVE" => Result.Failure<SubmitAnswerResult>(Error.RoundNotActive),
                "ROUND_EXPIRED" => Result.Failure<SubmitAnswerResult>(new Error("Game.RoundExpired", "Round time expired.")),
                "PLAYER_NOT_IN_ROOM" => Result.Failure<SubmitAnswerResult>(Error.PlayerNotInGame),
                "ALREADY_ANSWERED" => Result.Failure<SubmitAnswerResult>(Error.AlreadyAnswered),
                _ => Result.Failure<SubmitAnswerResult>(new Error("Game.AnswerFailed", resultStr ?? "Unknown error"))
            };
        }

        public async Task<Result<RoundResultDto>> EndRoundAsync(
            GameRoomId roomId,
            CancellationToken ct = default)
        {
            var result = await _redis.ScriptEvaluateAsync(
                LuaScripts.EndRound,
                new RedisKey[] { RoomKey(roomId) },
                new RedisValue[] { GameRoom.MaxPointsPerRound, GameRoom.PointDecrement });

            var resultStr = result.ToString();

            if (resultStr?.StartsWith("{") == true && resultStr.Contains("roundNumber"))
            {
                var roundResult = JsonSerializer.Deserialize<RoundResultDto>(resultStr, JsonOptions);
                return Result.Success(roundResult!);
            }

            return resultStr switch
            {
                "ROOM_NOT_FOUND" => Result.Failure<RoundResultDto>(Error.GameNotFound),
                "ROUND_NOT_ACTIVE" => Result.Failure<RoundResultDto>(Error.RoundNotActive),
                "NO_QUESTION" => Result.Failure<RoundResultDto>(Error.QuestionNotFound),
                _ => Result.Failure<RoundResultDto>(new Error("Game.EndRoundFailed", resultStr ?? "Unknown error"))
            };
        }

        public async Task<Result<GameResultDto>> EndGameAsync(
            GameRoomId roomId,
            CancellationToken ct = default)
        {
            var room = await GetByIdAsync(roomId, ct);
            if (room is null)
                return Result.Failure<GameResultDto>(Error.GameNotFound);

            room.Status = (int)GameStatus.GameEnded;

            // Build final standings
            var standings = room.Players
                .OrderByDescending(p => p.TotalScore)
                .Select((p, index) => new FinalStandingDto(
                    index + 1,
                    p.UserId,
                    p.DisplayName,
                    p.PhotoUrl,
                    p.TotalScore,
                    p.ColorHex))
                .ToList();

            var winnerId = standings.FirstOrDefault()?.UserId;

            var gameResult = new GameResultDto(
                room.Id,
                room.GameType,
                room.TotalRounds,
                winnerId,
                standings,
                room.StartedAt,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            // Cleanup
            await DeleteAsync(roomId, ct);

            return Result.Success(gameResult);
        }

        public async Task<GameRoomId?> GetRoomIdByPlayerAsync(int userId, CancellationToken ct = default)
        {
            var roomIdStr = await _redis.StringGetAsync(PlayerKey(userId));
            if (roomIdStr.IsNullOrEmpty)
                return null;

            if (Guid.TryParse(roomIdStr, out var guid))
            {
                return GameRoomId.Create(guid);
            }

            return null;
        }
    }
}

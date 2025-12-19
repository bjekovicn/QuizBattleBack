using System.Text.Json;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;
using StackExchange.Redis;
using QuizBattle.Infrastructure.Features.Games.Redis.Scripting;
using QuizBattle.Infrastructure.Features.Games.Redis.Scripting.Responses;
using QuizBattle.Application.Features.Games.Repositories;

namespace QuizBattle.Infrastructure.Features.Games.Redis.Repositories;

internal sealed class RedisGameRoomRepository : IGameRoomRepository
{
    private readonly IDatabase _redis;
    private readonly LuaScriptExecutor _scriptCaller;
    private readonly TimeSpan _roomTtl = TimeSpan.FromHours(2);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisGameRoomRepository(
        IConnectionMultiplexer connectionMultiplexer,
        LuaScriptExecutor scriptCaller)
    {
        _redis = connectionMultiplexer.GetDatabase();
        _scriptCaller = scriptCaller;
    }

    private static string RoomKey(string roomId) => $"game:room:{roomId}";
    private static string RoomKey(GameRoomId roomId) => RoomKey(roomId.Value.ToString());
    private static string PlayerKey(int userId) => $"game:player:{userId}";

    private const string ActiveRoomsKey = "game:active_rooms";

    public async Task<GameRoomDto?> GetByIdAsync(GameRoomId roomId, CancellationToken ct = default)
    {
        var result = await _redis.StringGetAsync(RoomKey(roomId));
        if (result.IsNullOrEmpty)
        {
            return null;
        }

        var rawJson = result.ToString();
        rawJson = rawJson.Replace("\"questions\":{}", "\"questions\":[]");
        rawJson = rawJson.Replace("\"players\":{}", "\"players\":[]");

        return JsonSerializer.Deserialize<GameRoomDto>(rawJson, JsonOptions);
    }

    public async Task<bool> ExistsAsync(GameRoomId roomId, CancellationToken ct = default)
    {
        return await _redis.KeyExistsAsync(RoomKey(roomId));
    }

    public async Task DeleteAsync(GameRoomId roomId, CancellationToken ct = default)
    {
        var room = await GetByIdAsync(roomId, ct);
        if (room is null) return;

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
            StartedAt = null,
            RoundStartedAt = null,
            RoundEndsAt = null,
            HostPlayerId = null,
            Players = new List<GamePlayerDto>(),
            Questions = new List<GameQuestionDto>()
        };

        var roomJson = JsonSerializer.Serialize(room, JsonOptions);

        var roomKey = RoomKey(roomId.ToString());
        var setSuccess = await _redis.StringSetAsync(roomKey, roomJson, _roomTtl);

        if (!setSuccess)
        {
            return Result.Failure<GameRoomDto>(
                new Error("Game.CreateFailed", "Failed to create room in Redis"));
        }

        await _redis.SetAddAsync(ActiveRoomsKey, roomId.ToString());

        return Result.Success(room);
    }

    public async Task<Result<GamePlayerDto>> JoinRoomAsync(
        GameRoomId roomId,
        int userId,
        string displayName,
        string? photoUrl,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var result = await _scriptCaller.EvalAsync(
            "join_room",
            new RedisKey[] { RoomKey(roomId), PlayerKey(userId) },
            new RedisValue[]
            {
                userId,
                displayName,
                photoUrl ?? "",
                now,
                (int)_roomTtl.TotalSeconds
            });

        var rawJson = result.ToString();
        rawJson = rawJson.Replace("\"questions\":{}", "\"questions\":[]");
        rawJson = rawJson.Replace("\"players\":{}", "\"players\":[]");

        var response = LuaResponseParser.Parse<JoinRoomResponse>(rawJson);

        if (response.Success && response.Player != null)
        {
            return Result.Success(response.Player);
        }

        return Result.Failure<GamePlayerDto>(MapErrorCode(response.Error ?? "Unknown error"));
    }

    public async Task<Result> LeaveRoomAsync(
        GameRoomId roomId,
        int userId,
        CancellationToken ct = default)
    {
        var room = await GetByIdAsync(roomId, ct);
        if (room is null)
        {
            return Result.Failure(Error.GameNotFound);
        }

        var player = room.Players.FirstOrDefault(p => p.UserId == userId);
        if (player is null)
        {
            return Result.Failure(Error.PlayerNotInGame);
        }

        room.Players.Remove(player);

        if (room.HostPlayerId == userId && room.Players.Any())
        {
            room.HostPlayerId = room.Players[0].UserId;
        }

        var roomJson = JsonSerializer.Serialize(room, JsonOptions);
        await _redis.StringSetAsync(RoomKey(roomId), roomJson, _roomTtl);

        await _redis.KeyDeleteAsync(PlayerKey(userId));

        return Result.Success();
    }

    public async Task<Result> SetPlayerReadyAsync(
        GameRoomId roomId,
        int userId,
        bool isReady,
        CancellationToken ct = default)
    {
        var room = await GetByIdAsync(roomId, ct);
        if (room is null)
        {
            return Result.Failure(Error.GameNotFound);
        }

        var player = room.Players.FirstOrDefault(p => p.UserId == userId);
        if (player is null)
        {
            return Result.Failure(Error.PlayerNotInGame);
        }

        player.IsReady = isReady;

        var roomJson = JsonSerializer.Serialize(room, JsonOptions);
        var setSuccess = await _redis.StringSetAsync(RoomKey(roomId), roomJson, _roomTtl);

        if (!setSuccess)
        {
            return Result.Failure(new Error("Game.UpdateFailed", "Failed to update player ready status"));
        }

        return Result.Success();
    }


    public async Task<Result> SetPlayerConnectedAsync(
        GameRoomId roomId,
        int userId,
        bool isConnected,
        CancellationToken ct = default)
    {
        var room = await GetByIdAsync(roomId, ct);
        if (room is null)
        {
            return Result.Failure(Error.GameNotFound);
        }

        var player = room.Players.FirstOrDefault(p => p.UserId == userId);
        if (player is null)
        {
            return Result.Failure(Error.PlayerNotInGame);
        }

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
        {
            return Result.Failure<GameRoomDto>(Error.GameNotFound);
        }

        if (room.Status != (int)GameStatus.WaitingForPlayers)
        {
            return Result.Failure<GameRoomDto>(Error.GameAlreadyStarted);
        }

        if (room.Players.Count < GameRoom.MinPlayers)
        {
            return Result.Failure<GameRoomDto>(Error.NotEnoughPlayers);
        }

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

        var result = await _scriptCaller.EvalAsync(
            "start_next_round",
            new RedisKey[] { RoomKey(roomId) },
            new RedisValue[] { now, roundDurationMs });

        var response = LuaResponseParser.Parse<StartNextRoundResponse>(result.ToString());

        if (response.Success && response.Question != null)
        {
            return Result.Success(response.Question);
        }

        return Result.Failure<GameQuestionDto>(MapErrorCode(response.Error ?? "Unknown error"));
    }

    public async Task<Result<SubmitAnswerResult>> SubmitAnswerAsync(
        GameRoomId roomId,
        int userId,
        string answer,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var result = await _scriptCaller.EvalAsync(
            "submit_answer",
            new RedisKey[] { RoomKey(roomId) },
            new RedisValue[] { userId, answer, now });

        var response = LuaResponseParser.Parse<SubmitAnswerResponse>(result.ToString());

        if (response.Success && response.Result != null)
        {
            return Result.Success(new SubmitAnswerResult(
                response.Result.Accepted,
                response.Result.AllPlayersAnswered,
                response.Result.PlayersAnsweredCount,
                response.Result.TotalPlayersCount));
        }

        return Result.Failure<SubmitAnswerResult>(MapErrorCode(response.Error ?? "Unknown error"));
    }

    public async Task<Result<RoundResultDto>> EndRoundAsync(
    GameRoomId roomId,
    CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var result = await _scriptCaller.EvalAsync(
            "end_current_round",
            new RedisKey[] { RoomKey(roomId) },
            new RedisValue[] { now });

        var rawJson = result.ToString();

        rawJson = rawJson.Replace("\"playerResults\":{}", "\"playerResults\":[]");
        rawJson = rawJson.Replace("\"currentStandings\":{}", "\"currentStandings\":[]");

        var response = LuaResponseParser.Parse<EndRoundResponse>(rawJson);

        if (response.Success && response.Result != null)
        {
            return Result.Success(response.Result);
        }

        return Result.Failure<RoundResultDto>(MapErrorCode(response.Error ?? "Unknown error"));
    }

    public async Task<Result<GameResultDto>> EndGameAsync(GameRoomId roomId, CancellationToken ct = default)
    {
        var room = await GetByIdAsync(roomId, ct);
        if (room is null)
        {
            return Result.Failure<GameResultDto>(Error.GameNotFound);
        }

        room.Status = (int)GameStatus.GameEnded;

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

        await DeleteAsync(roomId, ct);

        return Result.Success(gameResult);
    }

    public async Task<GameRoomId?> GetRoomIdByPlayerAsync(int userId, CancellationToken ct = default)
    {
        var roomIdStr = await _redis.StringGetAsync(PlayerKey(userId));
        if (roomIdStr.IsNullOrEmpty)
        {
            return null;
        }

        if (Guid.TryParse(roomIdStr, out var guid))
        {
            return GameRoomId.Create(guid);
        }

        return null;
    }


    private static Error MapErrorCode(string errorCode) => errorCode switch
    {
        "ROOM_EXISTS" => Error.RoomExists,
        "ROOM_NOT_FOUND" => Error.GameNotFound,
        "GAME_ALREADY_STARTED" => Error.GameAlreadyStarted,
        "ROOM_FULL" => Error.GameFull,
        "PLAYER_ALREADY_IN_ROOM" => Error.PlayerAlreadyInGame,
        "PLAYER_NOT_IN_ROOM" => Error.PlayerNotInGame,
        "ROUND_NOT_ACTIVE" => Error.RoundNotActive,
        "ROUND_EXPIRED" => Error.RoundExpired,
        "ROUND_ALREADY_ENDED" => Error.RoundAlreadyEnded,
        "ALREADY_ANSWERED" => Error.AlreadyAnswered,
        "NO_QUESTION" => Error.QuestionNotFound,
        "INVALID_STATE" => Error.InvalidState,
        "NO_MORE_ROUNDS" => Error.NoMoreRounds,
        "NO_QUESTIONS_LOADED" => Error.NoQuestionsLoaded,
        _ => new Error("Game.UnknownError", errorCode)
    };

}
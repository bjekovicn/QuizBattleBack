using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Games.Repositories;
using QuizBattle.Application.Features.Games.Services;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Infrastructure.Features.Games.Services;

internal sealed class GameRoomService : IGameRoomService
{
    private readonly IGameRoomRepository _gameRepository;
    private readonly ILogger<GameRoomService> _logger;

    public GameRoomService(
        IGameRoomRepository gameRepository,
        ILogger<GameRoomService> logger)
    {
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<Result<GameRoomDto>> CreateRoomAsync(
        GameType gameType,
        string languageCode,
        int totalRounds = 10,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating room - Type: {GameType}, Language: {Language}, Rounds: {Rounds}",
            gameType, languageCode, totalRounds);

        return await _gameRepository.CreateRoomAsync(gameType, languageCode, totalRounds, ct);
    }

    public async Task<Result<GamePlayerDto>> JoinRoomAsync(
        Guid roomId,
        int userId,
        string displayName,
        string? photoUrl,
        CancellationToken ct = default)
    {
        _logger.LogInformation("User {UserId} joining room {RoomId}", userId, roomId);

        return await _gameRepository.JoinRoomAsync(
            GameRoomId.Create(roomId),
            userId,
            displayName,
            photoUrl,
            ct);
    }

    public async Task<Result> LeaveRoomAsync(
        Guid roomId,
        int userId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("User {UserId} leaving room {RoomId}", userId, roomId);

        return await _gameRepository.LeaveRoomAsync(
            GameRoomId.Create(roomId),
            userId,
            ct);
    }

    public async Task<Result> SetPlayerReadyAsync(
        Guid roomId,
        int userId,
        bool isReady,
        CancellationToken ct = default)
    {
        _logger.LogInformation("User {UserId} setting ready status to {IsReady} in room {RoomId}",
            userId, isReady, roomId);

        return await _gameRepository.SetPlayerReadyAsync(
            GameRoomId.Create(roomId),
            userId,
            isReady,
            ct);
    }

    public async Task<Result<GameRoomDto>> GetRoomAsync(
        Guid roomId,
        CancellationToken ct = default)
    {
        var room = await _gameRepository.GetByIdAsync(GameRoomId.Create(roomId), ct);
        return room is null
            ? Result.Failure<GameRoomDto>(Error.GameNotFound)
            : Result.Success(room);
    }

    public async Task<Result<GameRoomDto?>> GetPlayerCurrentRoomAsync(
        int userId,
        CancellationToken ct = default)
    {
        var roomId = await _gameRepository.GetRoomIdByPlayerAsync(userId, ct);
        if (roomId is null)
            return Result.Success<GameRoomDto?>(null);

        var room = await _gameRepository.GetByIdAsync(roomId, ct);
        return Result.Success(room);
    }

    public async Task<Guid?> GetPlayerRoomIdAsync(
        int userId,
        CancellationToken ct = default)
    {
        var roomId = await _gameRepository.GetRoomIdByPlayerAsync(userId, ct);
        return roomId?.Value;
    }

    public async Task<Result> SetPlayerConnectedAsync(
        Guid roomId,
        int userId,
        bool isConnected,
        CancellationToken ct = default)
    {
        _logger.LogInformation("User {UserId} setting connected status to {IsConnected} in room {RoomId}",
            userId, isConnected, roomId);

        return await _gameRepository.SetPlayerConnectedAsync(
            GameRoomId.Create(roomId),
            userId,
            isConnected,
            ct);
    }
}
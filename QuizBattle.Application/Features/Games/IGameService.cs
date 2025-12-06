using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games;

public interface IGameService
{
    // Room Operations
    Task<Result<GameRoomDto>> CreateRoomAsync(GameType gameType, string languageCode, int totalRounds = 10, CancellationToken ct = default);
    Task<Result<GamePlayerDto>> JoinRoomAsync(Guid roomId, int userId, string displayName, string? photoUrl, CancellationToken ct = default);
    Task<Result> LeaveRoomAsync(Guid roomId, int userId, CancellationToken ct = default);
    Task<Result> SetPlayerReadyAsync(Guid roomId, int userId, bool isReady, CancellationToken ct = default);
    Task<Result<GameRoomDto>> GetRoomAsync(Guid roomId, CancellationToken ct = default);
    Task<Result<GameRoomDto?>> GetPlayerCurrentRoomAsync(int userId, CancellationToken ct = default);

    // Game Flow
    Task<Result<GameRoomDto>> StartGameAsync(Guid roomId, CancellationToken ct = default);
    Task<Result<RoundStartedResult>> StartRoundAsync(Guid roomId, CancellationToken ct = default);
    Task<Result<SubmitAnswerResult>> SubmitAnswerAsync(Guid roomId, int userId, string answer, CancellationToken ct = default);
    Task<Result<RoundResultDto>> EndRoundAsync(Guid roomId, CancellationToken ct = default);
    Task<Result<GameResultDto>> EndGameAsync(Guid roomId, CancellationToken ct = default);

    // Matchmaking
    Task<Result<MatchmakingOperationResult>> JoinMatchmakingAsync(int userId, string displayName, string? photoUrl, GameType gameType, string languageCode, CancellationToken ct = default);
    Task<Result> LeaveMatchmakingAsync(int userId, GameType gameType, string languageCode, CancellationToken ct = default);

    // Connection Management
    Task<Result> SetPlayerConnectedAsync(Guid roomId, int userId, bool isConnected, CancellationToken ct = default);
    Task<Guid?> GetPlayerRoomIdAsync(int userId, CancellationToken ct = default);
}

public sealed record RoundStartedResult(GameQuestionDto Question, int CurrentRound, int TotalRounds, long RoundEndsAt);
public sealed record MatchmakingOperationResult(bool MatchFound, string? RoomId, List<MatchedPlayerDto>? Players, int QueuePosition);
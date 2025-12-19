using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Services
{
    public interface IGameRoomService
    {
        Task<Result<GameRoomDto>> CreateRoomAsync(GameType gameType, string languageCode, int totalRounds = 10, CancellationToken ct = default);
        Task<Result<GamePlayerDto>> JoinRoomAsync(Guid roomId, int userId, string displayName, string? photoUrl, CancellationToken ct = default);
        Task<Result> LeaveRoomAsync(Guid roomId, int userId, CancellationToken ct = default); // FIXED
        Task<Result> SetPlayerReadyAsync(Guid roomId, int userId, bool isReady, CancellationToken ct = default); // FIXED
        Task<Result<GameRoomDto>> GetRoomAsync(Guid roomId, CancellationToken ct = default);
        Task<Result<GameRoomDto?>> GetPlayerCurrentRoomAsync(int userId, CancellationToken ct = default);
        Task<Guid?> GetPlayerRoomIdAsync(int userId, CancellationToken ct = default);
        Task<Result> SetPlayerConnectedAsync(Guid roomId, int userId, bool isConnected, CancellationToken ct = default); // FIXED
    }
}

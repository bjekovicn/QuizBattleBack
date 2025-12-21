using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Games.Repositories;
using QuizBattle.Application.Features.Games.Services;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Infrastructure.Features.Games.Services
{

    internal sealed class GameService : IGameService
    {
        private readonly IGameRoomService _roomService;
        private readonly IGameMatchmakingService _matchmakingService;
        private readonly IGameRoundService _roundService;

        public GameService(
            IGameRoomService roomService,
            IGameMatchmakingService matchmakingService,
            IGameRoundService roundService)
        {
            _roomService = roomService;
            _matchmakingService = matchmakingService;
            _roundService = roundService;
        }

        // Room operations
        public Task<Result<GameRoomDto>> CreateRoomAsync(GameType gameType, string languageCode, int totalRounds = 10, CancellationToken ct = default)
            => _roomService.CreateRoomAsync(gameType, languageCode, totalRounds, ct);

        public Task<Result<GamePlayerDto>> JoinRoomAsync(Guid roomId, int userId, string displayName, string? photoUrl, CancellationToken ct = default)
            => _roomService.JoinRoomAsync(roomId, userId, displayName, photoUrl, ct);

        public Task<Result> LeaveRoomAsync(Guid roomId, int userId, CancellationToken ct = default)
            => _roomService.LeaveRoomAsync(roomId, userId, ct);

        public Task<Result> SetPlayerReadyAsync(Guid roomId, int userId, bool isReady, CancellationToken ct = default)
            => _roomService.SetPlayerReadyAsync(roomId, userId, isReady, ct);

        public Task<Result<GameRoomDto>> GetRoomAsync(Guid roomId, CancellationToken ct = default)
            => _roomService.GetRoomAsync(roomId, ct);

        public Task<Result<GameRoomDto?>> GetPlayerCurrentRoomAsync(int userId, CancellationToken ct = default)
            => _roomService.GetPlayerCurrentRoomAsync(userId, ct);

        public Task<Guid?> GetPlayerRoomIdAsync(int userId, CancellationToken ct = default)
            => _roomService.GetPlayerRoomIdAsync(userId, ct);

        public Task<Result> SetPlayerConnectedAsync(Guid roomId, int userId, bool isConnected, CancellationToken ct = default)
            => _roomService.SetPlayerConnectedAsync(roomId, userId, isConnected, ct);

        // Matchmaking operations
        public Task<Result<MatchmakingResult>> JoinMatchmakingAsync(int userId, string displayName, string? photoUrl, GameType gameType, string languageCode, CancellationToken ct = default)
            => _matchmakingService.JoinMatchmakingAsync(userId, displayName, photoUrl, gameType, languageCode, ct);

        public Task<Result> LeaveMatchmakingAsync(int userId, GameType gameType, string languageCode, CancellationToken ct = default)
            => _matchmakingService.LeaveMatchmakingAsync(userId, gameType, languageCode, ct);

        // Round operations
        public Task<Result<GameRoomDto>> StartGameAsync(Guid roomId, CancellationToken ct = default)
            => _roundService.StartGameAsync(roomId, ct);

        public Task<Result<RoundStartedResult>> StartRoundAsync(Guid roomId, CancellationToken ct = default)
            => _roundService.StartRoundAsync(roomId, ct);

        public Task<Result<RoundResultDto>> EndRoundAsync(Guid roomId, CancellationToken ct = default)
            => _roundService.EndRoundAsync(roomId, ct);

        public Task<Result<SubmitAnswerResult>> SubmitAnswerAsync(Guid roomId, int userId, string answer, CancellationToken ct = default)
            => _roundService.SubmitAnswerAsync(roomId, userId, answer, ct);

        public Task<Result<GameResultDto>> EndGameAsync(Guid roomId, CancellationToken ct = default)
            => _roundService.EndGameAsync(roomId, ct);
    }
}
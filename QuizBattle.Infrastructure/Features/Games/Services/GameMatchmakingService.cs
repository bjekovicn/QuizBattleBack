using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Games.Repositories;
using QuizBattle.Application.Features.Games.Services;
using QuizBattle.Application.Shared.Abstractions.RealTime;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Infrastructure.Features.Games.Services
{
    internal sealed class GameMatchmakingService : IGameMatchmakingService
    {
        private readonly IMatchmakingRepository _matchmakingRepository;
        private readonly IGameRoomService _roomService;
        private readonly IGameRoundService _roundService;
        private readonly IGameNotificationsService _hubService;
        private readonly ILogger<GameMatchmakingService> _logger;

        public GameMatchmakingService(
            IMatchmakingRepository matchmakingRepository,
            IGameRoomService roomService,
            IGameRoundService roundService,
            IGameNotificationsService hubService,
            ILogger<GameMatchmakingService> logger)
        {
            _matchmakingRepository = matchmakingRepository;
            _roomService = roomService;
            _roundService = roundService;
            _hubService = hubService;
            _logger = logger;
        }

        public async Task<Result<MatchmakingResult>> JoinMatchmakingAsync( 
            int userId,
            string displayName,
            string? photoUrl,
            GameType gameType,
            string languageCode,
            CancellationToken ct = default)
        {
            _logger.LogInformation("User {UserId} joining matchmaking - Type: {GameType}, Language: {Language}",
                userId, gameType, languageCode);

            var match = await _matchmakingRepository
                .JoinQueueAsync(userId, displayName, photoUrl, gameType, languageCode, ct);

            if (match.IsFailure)
            {
                _logger.LogWarning("Matchmaking failed for user {UserId}: {Error}", userId, match.Error);
                return Result.Failure<MatchmakingResult>(match.Error); 
            }

            if (!match.Value.MatchFound || match.Value.Players is null)
            {
                _logger.LogInformation("User {UserId} added to queue, waiting for match", userId);
                return Result.Success(match.Value);  
            }

            _logger.LogInformation("Match found for user {UserId} with {PlayerCount} players",
                userId, match.Value.Players.Count);

            var room = await _roomService.CreateRoomAsync(gameType, languageCode, 10, ct);
            if (room.IsFailure)
            {
                _logger.LogError("Failed to create room after match: {Error}", room.Error);
                return Result.Failure<MatchmakingResult>(room.Error); 
            }

            var roomId = Guid.Parse(room.Value.Id);

            foreach (var player in match.Value.Players)
            {
                await _roomService.JoinRoomAsync(
                    roomId,
                    player.UserId,
                    player.DisplayName,
                    player.PhotoUrl,
                    ct);
            }

            var playerIds = match.Value.Players.Select(p => p.UserId).ToList();
            await _hubService.AddUsersToRoomAsync(roomId, playerIds);

            await _hubService.NotifyMatchFoundAsync(
                playerIds,
                new MatchFoundEvent(room.Value.Id, match.Value.Players),
                ct);

            _ = StartMatchGameFlowAsync(roomId, room.Value, ct);

            return Result.Success(new MatchmakingResult(  
                true,
                room.Value.Id,
                match.Value.Players));
        }

        public async Task<Result> LeaveMatchmakingAsync( 
            int userId,
            GameType gameType,
            string languageCode,
            CancellationToken ct = default)
        {
            _logger.LogInformation("User {UserId} leaving matchmaking - Type: {GameType}, Language: {Language}",
                userId, gameType, languageCode);

            return await _matchmakingRepository.LeaveQueueAsync(userId, gameType, languageCode, ct);
        }

        private async Task StartMatchGameFlowAsync(
            Guid roomId,
            GameRoomDto room,
            CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Starting game flow for matched room {RoomId}", roomId);

                await Task.Delay(TimeSpan.FromSeconds(3), ct);

                await _roundService.StartGameAsync(roomId, ct);

                var updatedRoom = await _roomService.GetRoomAsync(roomId, ct);
                if (updatedRoom.IsSuccess && updatedRoom.Value != null)
                {
                    await _hubService.NotifyGameStartingAsync(updatedRoom.Value.Id, updatedRoom.Value, ct);
                }

                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in match game flow for room {RoomId}", roomId);
            }
        }
    }
}
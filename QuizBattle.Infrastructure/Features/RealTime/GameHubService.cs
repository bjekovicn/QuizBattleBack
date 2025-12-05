using Microsoft.AspNetCore.SignalR;
using QuizBattle.Application.Features.Games;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Shared.Abstractions.RealTime;

namespace QuizBattle.Infrastructure.Features.RealTime
{

    internal sealed class GameHubService : IGameHubService
    {
        private readonly IHubContext<GameHub, IGameHubClient> _hubContext;

        public GameHubService(IHubContext<GameHub, IGameHubClient> hubContext)
        {
            _hubContext = hubContext;
        }

        // Room events
        public async Task NotifyRoomCreatedAsync(string roomId, GameRoomDto room, CancellationToken ct = default)
        {
            await _hubContext.Clients.Group($"room:{roomId}").RoomCreated(room);
        }

        public async Task NotifyPlayerJoinedAsync(string roomId, GamePlayerDto player, CancellationToken ct = default)
        {
            await _hubContext.Clients.Group($"room:{roomId}").PlayerJoined(player);
        }

        public async Task NotifyPlayerLeftAsync(string roomId, int userId, CancellationToken ct = default)
        {
            await _hubContext.Clients.Group($"room:{roomId}").PlayerLeft(userId);
        }

        public async Task NotifyPlayerReadyChangedAsync(string roomId, int userId, bool isReady, CancellationToken ct = default)
        {
            await _hubContext.Clients.Group($"room:{roomId}").PlayerReadyChanged(userId, isReady);
        }

        public async Task NotifyPlayerDisconnectedAsync(string roomId, int userId, CancellationToken ct = default)
        {
            await _hubContext.Clients.Group($"room:{roomId}").PlayerDisconnected(userId);
        }

        public async Task NotifyPlayerReconnectedAsync(string roomId, int userId, CancellationToken ct = default)
        {
            await _hubContext.Clients.Group($"room:{roomId}").PlayerReconnected(userId);
        }

        // Game flow events
        public async Task NotifyGameStartingAsync(string roomId, GameRoomDto room, CancellationToken ct = default)
        {
            await _hubContext.Clients.Group($"room:{roomId}").GameStarting(room);
        }

        public async Task NotifyRoundStartedAsync(string roomId, RoundStartedEvent roundEvent, CancellationToken ct = default)
        {
            await _hubContext.Clients.Group($"room:{roomId}").RoundStarted(roundEvent);
        }

        public async Task NotifyPlayerAnsweredAsync(string roomId, int userId, CancellationToken ct = default)
        {
            await _hubContext.Clients.Group($"room:{roomId}").PlayerAnswered(userId);
        }

        public async Task NotifyRoundEndedAsync(string roomId, RoundResultDto result, CancellationToken ct = default)
        {
            await _hubContext.Clients.Group($"room:{roomId}").RoundEnded(result);
        }

        public async Task NotifyGameEndedAsync(string roomId, GameResultDto result, CancellationToken ct = default)
        {
            await _hubContext.Clients.Group($"room:{roomId}").GameEnded(result);
        }

        // Matchmaking events
        public async Task NotifyMatchFoundAsync(List<int> userIds, MatchFoundEvent matchEvent, CancellationToken ct = default)
        {
            foreach (var userId in userIds)
            {
                await _hubContext.Clients.Group($"user:{userId}").MatchFound(matchEvent);
            }
        }

        public async Task NotifyMatchmakingUpdateAsync(int userId, MatchmakingUpdateEvent update, CancellationToken ct = default)
        {
            await _hubContext.Clients.Group($"user:{userId}").MatchmakingUpdate(update);
        }

        // Error events
        public async Task NotifyErrorAsync(int userId, string code, string message, CancellationToken ct = default)
        {
            await _hubContext.Clients.Group($"user:{userId}").Error(code, message);
        }

        public async Task NotifyRoomErrorAsync(string roomId, string code, string message, CancellationToken ct = default)
        {
            await _hubContext.Clients.Group($"room:{roomId}").Error(code, message);
        }
    }
}

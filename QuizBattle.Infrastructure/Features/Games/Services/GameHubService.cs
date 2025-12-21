using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Games.Repositories;
using QuizBattle.Application.Features.Games.Services;
using QuizBattle.Application.Shared.Abstractions.RealTime;
using QuizBattle.Infrastructure.Features.RealTime;

namespace QuizBattle.Infrastructure.Features.Games.Services
{
    internal sealed class GameHubService : IGameHubService
    {
        private readonly IHubContext<GameHub, IGameHubClient> _hubContext;
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<GameHubService> _logger;

        public GameHubService(
            IHubContext<GameHub, IGameHubClient> hubContext,
            IConnectionManager connectionManager,
            ILogger<GameHubService> logger)
        {
            _hubContext = hubContext;
            _connectionManager = connectionManager;
            _logger = logger;
        }

        // Room events
        public async Task NotifyRoomCreatedAsync(string roomId, GameRoomDto room, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending RoomCreated to room:{RoomId}", roomId);
            await _hubContext.Clients.Group($"room:{roomId}").RoomCreated(room);
        }

        public async Task NotifyPlayerJoinedAsync(string roomId, GamePlayerDto player, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending PlayerJoined to room:{RoomId}, player:{UserId}", roomId, player.UserId);
            await _hubContext.Clients.Group($"room:{roomId}").PlayerJoined(player);
        }

        public async Task NotifyPlayerLeftAsync(string roomId, int userId, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending PlayerLeft to room:{RoomId}, userId:{UserId}", roomId, userId);
            await _hubContext.Clients.Group($"room:{roomId}").PlayerLeft(userId);
        }

        public async Task NotifyPlayerReadyChangedAsync(string roomId, int userId, bool isReady, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending PlayerReadyChanged to room:{RoomId}, userId:{UserId}, ready:{IsReady}", roomId, userId, isReady);
            await _hubContext.Clients.Group($"room:{roomId}").PlayerReadyChanged(userId, isReady);
        }

        public async Task NotifyPlayerDisconnectedAsync(string roomId, int userId, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending PlayerDisconnected to room:{RoomId}, userId:{UserId}", roomId, userId);
            await _hubContext.Clients.Group($"room:{roomId}").PlayerDisconnected(userId);
        }

        public async Task NotifyPlayerReconnectedAsync(string roomId, int userId, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending PlayerReconnected to room:{RoomId}, userId:{UserId}", roomId, userId);
            await _hubContext.Clients.Group($"room:{roomId}").PlayerReconnected(userId);
        }

        // Game flow events
        public async Task NotifyGameStartingAsync(string roomId, GameRoomDto room, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending GameStarting to room:{RoomId}", roomId);
            await _hubContext.Clients.Group($"room:{roomId}").GameStarting(room);
        }

        public async Task NotifyRoundStartedAsync(string roomId, RoundStartedEvent roundEvent, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending RoundStarted to room:{RoomId}, round:{Round}", roomId, roundEvent.CurrentRound);
            await _hubContext.Clients.Group($"room:{roomId}").RoundStarted(roundEvent);
        }

        public async Task NotifyPlayerAnsweredAsync(string roomId, int userId, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending PlayerAnswered to room:{RoomId}, userId:{UserId}", roomId, userId);
            await _hubContext.Clients.Group($"room:{roomId}").PlayerAnswered(userId);
        }

        public async Task NotifyRoundEndedAsync(string roomId, RoundResultDto result, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending RoundEnded to room:{RoomId}, round:{Round}", roomId, result.RoundNumber);
            await _hubContext.Clients.Group($"room:{roomId}").RoundEnded(result);
        }

        public async Task NotifyGameEndedAsync(string roomId, GameResultDto result, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending GameEnded to room:{RoomId}, winner:{WinnerId}", roomId, result.WinnerUserId);
            await _hubContext.Clients.Group($"room:{roomId}").GameEnded(result);
        }

        // Matchmaking events
        public async Task NotifyMatchFoundAsync(List<int> userIds, MatchFoundEvent matchEvent, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending MatchFound to users:{UserIds}, room:{RoomId}",
                string.Join(",", userIds), matchEvent.RoomId);

            foreach (var userId in userIds)
            {
                await _hubContext.Clients.Group($"user:{userId}").MatchFound(matchEvent);
            }
        }

        public async Task NotifyMatchmakingUpdateAsync(int userId, MatchmakingUpdateEvent update, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending MatchmakingUpdate to user:{UserId}, position:{Position}",
                userId, update.QueuePosition);
            await _hubContext.Clients.Group($"user:{userId}").MatchmakingUpdate(update);
        }

        // Error events
        public async Task NotifyErrorAsync(int userId, string code, string message, CancellationToken ct = default)
        {
            _logger.LogWarning("[GameHubService] Sending Error to user:{UserId}, code:{Code}, message:{Message}",
                userId, code, message);
            await _hubContext.Clients.Group($"user:{userId}").Error(code, message);
        }

        public async Task NotifyRoomErrorAsync(string roomId, string code, string message, CancellationToken ct = default)
        {
            _logger.LogWarning("[GameHubService] Sending Error to room:{RoomId}, code:{Code}, message:{Message}",
                roomId, code, message);
            await _hubContext.Clients.Group($"room:{roomId}").Error(code, message);
        }

        public async Task AddUsersToRoomAsync(Guid roomId, IReadOnlyCollection<int> userIds)
        {
            var roomGroup = $"room:{roomId}";
            _logger.LogInformation("[GameHubService] Adding users to room:{RoomId}, users:{UserIds}",
                roomId, string.Join(",", userIds));

            foreach (var userId in userIds)
            {
                var connectionIds = await _connectionManager.GetConnectionsAsync(userId);

                _logger.LogDebug("[GameHubService] User:{UserId} has {Count} active connections",
                    userId, connectionIds.Count);

                foreach (var connectionId in connectionIds)
                {
                    await _hubContext.Groups.AddToGroupAsync(connectionId, roomGroup);
                    _logger.LogDebug("[GameHubService] Added connection:{ConnectionId} to room:{RoomId}",
                        connectionId, roomId);
                }
            }

            _logger.LogInformation("[GameHubService] Successfully added all users to room:{RoomId}", roomId);
        }

        public async Task RemoveUserFromRoomAsync(Guid roomId, int userId)
        {
            var roomGroup = $"room:{roomId}";
            _logger.LogInformation("[GameHubService] Removing user:{UserId} from room:{RoomId}", userId, roomId);

            var connectionIds = await _connectionManager.GetConnectionsAsync(userId);

            foreach (var connectionId in connectionIds)
            {
                await _hubContext.Groups.RemoveFromGroupAsync(connectionId, roomGroup);
                _logger.LogDebug("[GameHubService] Removed connection:{ConnectionId} from room:{RoomId}",
                    connectionId, roomId);
            }
        }

        // Invite events
        public async Task NotifyInviteSentAsync(int hostId, GameInviteDto invite, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending InviteSent to host:{HostId}, inviteId:{InviteId}",
                hostId, invite.Id);
            await _hubContext.Clients.Group($"user:{hostId}").InviteSent(invite);
        }

        public async Task NotifyInviteReceivedAsync(int invitedUserId, GameInviteDto invite, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending InviteReceived to user:{UserId}, inviteId:{InviteId}",
                invitedUserId, invite.Id);
            await _hubContext.Clients.Group($"user:{invitedUserId}").InviteReceived(invite);
        }

        public async Task NotifyInviteResponseAsync(int hostId, int friendId, bool accepted, GameInviteDto invite, CancellationToken ct = default)
        {
            _logger.LogInformation("[GameHubService] Sending InviteResponse to host:{HostId}, friend:{FriendId}, accepted:{Accepted}",
                hostId, friendId, accepted);

            var response = new InviteResponseEvent(
                friendId,
                invite.InvitedDisplayName,
                invite.InvitedPhotoUrl,
                accepted,
                invite.RoomId);

            await _hubContext.Clients.Group($"user:{hostId}").InviteResponse(response);
        }

    }
}

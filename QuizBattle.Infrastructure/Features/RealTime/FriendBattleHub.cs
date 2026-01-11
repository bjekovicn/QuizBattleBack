using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Games.Services;
using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.Notifications;
using QuizBattle.Application.Shared.Abstractions.RealTime;
using QuizBattle.Domain.Features.Games;

namespace QuizBattle.Infrastructure.Features.RealTime.Hubs
{
    /// <summary>
    /// SignalR Hub for Friend Battle game mode.
    /// Handles private rooms where players can invite their friends.
    /// Supports 1v1 (Friend Duel) or multiple players (Friend Battle).
    /// </summary>
    public sealed class FriendBattleHub : BaseGameHub
    {
        private readonly IGameInviteService _inviteService;
        private readonly IPushNotificationService _pushNotificationService;

        public FriendBattleHub(
            IGameRoomService roomService,
            IGameRoundService roundService,
            IGameNotificationsService hubService,
            IGameInviteService inviteService,
            IPushNotificationService pushNotificationService,
            IConnectionManager connectionManager,
            IUserQueryRepository userRepository,
            ILogger<FriendBattleHub> logger)
            : base(roomService, roundService, hubService, connectionManager, userRepository, logger)
        {
            _inviteService = inviteService;
            _pushNotificationService = pushNotificationService;
        }

        #region Room Management

        /// <summary>
        /// Create a new Friend Battle room.
        /// The host automatically joins the room upon creation.
        /// </summary>
        /// <param name="languageCode">Language for questions (e.g., "en", "sr")</param>
        /// <param name="totalRounds">Number of rounds (default: 10)</param>
        /// <returns>Created room details or null if failed</returns>
        public async Task<GameRoomDto?> CreateRoom(string languageCode, int totalRounds = 10)
        {
            var userId = GetRequiredUserId();

            Logger.LogInformation("[FriendBattleHub] User:{UserId} creating room, lang:{Lang}, rounds:{Rounds}",
                userId, languageCode, totalRounds);

            var user = await GetUserInfoAsync(userId);

            // Create the room
            var roomResult = await RoomService.CreateRoomAsync(
                GameType.FriendBattle,
                languageCode,
                totalRounds);

            if (roomResult.IsFailure)
            {
                Logger.LogWarning("[FriendBattleHub] Create room failed for user:{UserId}, error:{Error}",
                    userId, roomResult.Error.Message);
                await SendErrorAsync(roomResult.Error.Code, roomResult.Error.Message);
                return null;
            }

            var room = roomResult.Value;
            var roomId = Guid.Parse(room.Id);

            // Host automatically joins the room
            var joinResult = await RoomService.JoinRoomAsync(
                roomId,
                userId,
                user.DisplayName,
                user.PhotoUrl);

            if (joinResult.IsFailure)
            {
                Logger.LogWarning("[FriendBattleHub] Auto-join host failed for user:{UserId}, error:{Error}",
                    userId, joinResult.Error.Message);
                await SendErrorAsync(joinResult.Error.Code, joinResult.Error.Message);
                return null;
            }

            // Add host to room group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{room.Id}");

            Logger.LogInformation("[FriendBattleHub] User:{UserId} created and joined room:{RoomId}",
                userId, room.Id);

            // Return updated room with host as player
            var updatedRoom = await RoomService.GetRoomAsync(roomId);
            return updatedRoom.IsSuccess ? updatedRoom.Value : null;
        }

        #endregion

        #region Invitations

        /// <summary>
        /// Invite a friend to join the room.
        /// Sends a push notification to the invited friend.
        /// </summary>
        /// <param name="roomId">Room ID to invite friend to</param>
        /// <param name="friendId">User ID of the friend to invite</param>
        public async Task InviteFriend(string roomId, int friendId)
        {
            var hostId = GetRequiredUserId();

            Logger.LogInformation("[FriendBattleHub] User:{HostId} inviting friend:{FriendId} to room:{RoomId}",
                hostId, friendId, roomId);

            var host = await GetUserInfoAsync(hostId);

            // Create the invite
            var inviteResult = await _inviteService.CreateInviteAsync(
                Guid.Parse(roomId),
                hostId,
                host.DisplayName,
                host.PhotoUrl,
                friendId);

            if (inviteResult.IsFailure)
            {
                Logger.LogWarning("[FriendBattleHub] Create invite failed, error:{Error}",
                    inviteResult.Error.Message);
                await SendErrorAsync(inviteResult.Error.Code, inviteResult.Error.Message);
                return;
            }

            var invite = inviteResult.Value;

            // Send push notification to invited friend
            await SendInvitePushNotificationAsync(invite, host);

            // Notify host that invite was sent
            await HubService.NotifyInviteSentAsync(hostId, invite);

            // Notify invited friend via SignalR
            await HubService.NotifyInviteReceivedAsync(friendId, invite);

            Logger.LogInformation("[FriendBattleHub] Invite:{InviteId} created and sent to friend:{FriendId}",
                invite.Id, friendId);
        }

        /// <summary>
        /// Respond to a friend's game invitation.
        /// If accepted, the player automatically joins the room.
        /// </summary>
        /// <param name="inviteId">Invitation ID</param>
        /// <param name="accept">True to accept, false to reject</param>
        public async Task RespondToInvite(string inviteId, bool accept)
        {
            var userId = GetRequiredUserId();

            Logger.LogInformation("[FriendBattleHub] User:{UserId} responding to invite:{InviteId}, accept:{Accept}",
                userId, inviteId, accept);

            var user = await GetUserInfoAsync(userId);

            var inviteResult = await _inviteService.RespondToInviteAsync(
                Guid.Parse(inviteId),
                userId,
                accept);

            if (inviteResult.IsFailure)
            {
                Logger.LogWarning("[FriendBattleHub] Respond to invite failed: {Error}",
                    inviteResult.Error.Message);
                await SendErrorAsync(inviteResult.Error.Code, inviteResult.Error.Message);
                return;
            }

            var invite = inviteResult.Value;

            if (accept)
            {
                // Join the room
                var joinResult = await RoomService.JoinRoomAsync(
                    Guid.Parse(invite.RoomId),
                    userId,
                    user.DisplayName,
                    user.PhotoUrl);

                if (joinResult.IsFailure)
                {
                    Logger.LogWarning("[FriendBattleHub] Join room after accept failed: {Error}",
                        joinResult.Error.Message);
                    await SendErrorAsync(joinResult.Error.Code, joinResult.Error.Message);
                    return;
                }

                // Add to room group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{invite.RoomId}");

                // Notify room that player joined
                await HubService.NotifyPlayerJoinedAsync(invite.RoomId, joinResult.Value);

                Logger.LogInformation("[FriendBattleHub] User:{UserId} accepted invite and joined room:{RoomId}",
                    userId, invite.RoomId);
            }
            else
            {
                Logger.LogInformation("[FriendBattleHub] User:{UserId} rejected invite:{InviteId}",
                    userId, inviteId);
            }

            // Notify host of the response
            await HubService.NotifyInviteResponseAsync(invite.HostUserId, userId, accept, invite);
        }

        /// <summary>
        /// Get the status of all invites sent for a room.
        /// Only the room host can call this method.
        /// </summary>
        /// <param name="roomId">Room ID to check invite statuses for</param>
        /// <returns>List of invite statuses or null if failed</returns>
        public async Task<List<InviteStatusDto>?> GetRoomInviteStatuses(string roomId)
        {
            var userId = GetRequiredUserId();

            Logger.LogInformation("[FriendBattleHub] User:{UserId} getting invite statuses for room:{RoomId}",
                userId, roomId);

            // Verify user is the host
            var roomResult = await RoomService.GetRoomAsync(Guid.Parse(roomId));
            if (roomResult.IsFailure || roomResult.Value.HostPlayerId != userId)
            {
                await SendErrorAsync("NOT_HOST", "Only the host can view invite statuses");
                return null;
            }

            var statusesResult = await _inviteService.GetRoomInviteStatusesAsync(Guid.Parse(roomId));

            if (statusesResult.IsFailure)
            {
                await SendErrorAsync(statusesResult.Error.Code, statusesResult.Error.Message);
                return null;
            }

            return statusesResult.Value;
        }

        #endregion

        #region Game Start

        /// <summary>
        /// Start the Friend Battle game.
        /// Only the room host can start the game.
        /// Requires at least 2 players (host + 1 friend).
        /// </summary>
        /// <param name="roomId">Room ID to start</param>
        public async Task StartGame(string roomId)
        {
            var hostId = GetRequiredUserId();

            Logger.LogInformation("[FriendBattleHub] User:{HostId} starting game in room:{RoomId}",
                hostId, roomId);

            // Verify user is the host
            var roomResult = await RoomService.GetRoomAsync(Guid.Parse(roomId));
            if (roomResult.IsFailure)
            {
                await SendErrorAsync(roomResult.Error.Code, roomResult.Error.Message);
                return;
            }

            var room = roomResult.Value;

            if (room.HostPlayerId != hostId)
            {
                Logger.LogWarning("[FriendBattleHub] User:{UserId} is not host of room:{RoomId}",
                    hostId, roomId);
                await SendErrorAsync("NOT_HOST", "Only the host can start the game");
                return;
            }

            // Check minimum players
            if (room.Players.Count < 2)
            {
                Logger.LogWarning("[FriendBattleHub] Not enough players in room:{RoomId}, count:{Count}",
                    roomId, room.Players.Count);
                await SendErrorAsync("NOT_ENOUGH_PLAYERS", "At least one friend must join to start the game");
                return;
            }

            // Start the game
            var startResult = await RoundService.StartGameAsync(Guid.Parse(roomId));
            if (startResult.IsFailure)
            {
                await SendErrorAsync(startResult.Error.Code, startResult.Error.Message);
                return;
            }

            Logger.LogInformation("[FriendBattleHub] Game started for room:{RoomId}", roomId);

            // Notify all players that game is starting
            await HubService.NotifyGameStartingAsync(roomId, startResult.Value);

            // Wait 3 seconds before starting first round
            await Task.Delay(TimeSpan.FromSeconds(3));

            // Start the first round
            var roundResult = await RoundService.StartRoundAsync(Guid.Parse(roomId));
            if (roundResult.IsSuccess)
            {
                var roundEvent = new RoundStartedEvent(
                    roundResult.Value.CurrentRound,
                    roundResult.Value.TotalRounds,
                    roundResult.Value.Question,
                    roundResult.Value.RoundEndsAt);

                await HubService.NotifyRoundStartedAsync(roomId, roundEvent);

                Logger.LogInformation("[FriendBattleHub] Round {Round} started for room:{RoomId}",
                    roundResult.Value.CurrentRound, roomId);
            }
        }

        #endregion

        #region Helper Methods

        private async Task SendInvitePushNotificationAsync(GameInviteDto invite, UserInfo host)
        {
            try
            {
                await _pushNotificationService.SendNotificationAsync(new(
                    UserId: invite.InvitedUserId,
                    Title: "QuizBattle Invitation 🎮",
                    Body: $"{host.DisplayName} invited you to a QuizBattle!",
                    Data: new Dictionary<string, string>
                    {
                        { "type", "game_invite" },
                        { "inviteId", invite.Id },
                        { "roomId", invite.RoomId },
                        { "hostId", invite.HostUserId.ToString() },
                        { "hostName", host.DisplayName }
                    }
                ));

                Logger.LogInformation("[FriendBattleHub] FCM notification sent for invite:{InviteId}",
                    invite.Id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[FriendBattleHub] Failed to send FCM notification for invite:{InviteId}",
                    invite.Id);
            }
        }

        #endregion
    }
}
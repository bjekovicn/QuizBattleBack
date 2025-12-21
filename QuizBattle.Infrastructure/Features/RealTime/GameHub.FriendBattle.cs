using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Shared.Abstractions.Notifications;
using QuizBattle.Domain.Features.Games;

namespace QuizBattle.Infrastructure.Features.RealTime
{
    public sealed partial class GameHub
    {
        public async Task<GameRoomDto?> CreateFriendRoom(string languageCode, int totalRounds = 10)
        {
            var userId = GetRequiredUserId();
            _logger.LogInformation("[GameHub] User:{UserId} creating friend room, lang:{Lang}, rounds:{Rounds}",
                userId, languageCode, totalRounds);

            var user = await GetUserInfoAsync(userId);

            var roomResult = await _gameService.CreateRoomAsync(
                GameType.FriendBattle,
                languageCode,
                totalRounds);

            if (roomResult.IsFailure)
            {
                _logger.LogWarning("[GameHub] Create room failed for user:{UserId}, error:{Error}",
                    userId, roomResult.Error.Message);
                await SendErrorAsync(roomResult.Error.Code, roomResult.Error.Message);
                return null;
            }

            var room = roomResult.Value;
            var roomId = Guid.Parse(room.Id);

            var joinResult = await _gameService.JoinRoomAsync(
                roomId,
                userId,
                user.DisplayName,
                user.PhotoUrl);

            if (joinResult.IsFailure)
            {
                _logger.LogWarning("[GameHub] Auto-join host failed for user:{UserId}, error:{Error}",
                    userId, joinResult.Error.Message);
                await SendErrorAsync(joinResult.Error.Code, joinResult.Error.Message);
                return null;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{room.Id}");

            _logger.LogInformation("[GameHub] User:{UserId} created and joined friend room:{RoomId}",
                userId, room.Id);

            var updatedRoom = await _gameService.GetRoomAsync(roomId);
            return updatedRoom.IsSuccess ? updatedRoom.Value : room;
        }

        public async Task InviteFriend(string roomId, int friendId)
        {
            var hostId = GetRequiredUserId();
            _logger.LogInformation("[GameHub] User:{HostId} inviting friend:{FriendId} to room:{RoomId}",
                hostId, friendId, roomId);

            var host = await GetUserInfoAsync(hostId);

            var roomResult = await _gameService.GetRoomAsync(Guid.Parse(roomId));
            if (roomResult.IsFailure)
            {
                await SendErrorAsync(roomResult.Error.Code, roomResult.Error.Message);
                return;
            }

            var room = roomResult.Value;
            if (room.HostPlayerId != hostId)
            {
                _logger.LogWarning("[GameHub] User:{UserId} is not host of room:{RoomId}",
                    hostId, roomId);
                await SendErrorAsync("NOT_HOST", "Only the host can invite players");
                return;
            }

            var inviteResult = await _inviteService.CreateInviteAsync(
                Guid.Parse(roomId),
                hostId,
                host.DisplayName,
                host.PhotoUrl,
                friendId);

            if (inviteResult.IsFailure)
            {
                _logger.LogWarning("[GameHub] Invite creation failed: {Error}",
                    inviteResult.Error.Message);
                await SendErrorAsync(inviteResult.Error.Code, inviteResult.Error.Message);
                return;
            }

            var invite = inviteResult.Value;

            try
            {
                await _pushNotificationService.SendToUserAsync(
                    friendId,
                    new PushNotification(
                        Title: "Game Challenge! 🎮",
                        Body: $"{host.DisplayName} invited you to a QuizBattle!",
                        Data: new Dictionary<string, string>
                        {
                        { "type", "game_invite" },
                        { "inviteId", invite.Id },
                        { "roomId", roomId },
                        { "hostId", hostId.ToString() },
                        { "hostName", host.DisplayName }
                        }
                    ));

                _logger.LogInformation("[GameHub] FCM notification sent for invite:{InviteId}",
                    invite.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GameHub] Failed to send FCM notification for invite:{InviteId}",
                    invite.Id);
            }

            await _hubService.NotifyInviteSentAsync(hostId, invite);

            _logger.LogInformation("[GameHub] Invite:{InviteId} created and sent to friend:{FriendId}",
                invite.Id, friendId);
        }

        public async Task RespondToInvite(string inviteId, bool accept)
        {
            var userId = GetRequiredUserId();
            _logger.LogInformation("[GameHub] User:{UserId} responding to invite:{InviteId}, accept:{Accept}",
                userId, inviteId, accept);

            var user = await GetUserInfoAsync(userId);

            var inviteResult = await _inviteService.RespondToInviteAsync(
                Guid.Parse(inviteId),
                userId,
                accept);

            if (inviteResult.IsFailure)
            {
                _logger.LogWarning("[GameHub] Respond to invite failed: {Error}",
                    inviteResult.Error.Message);
                await SendErrorAsync(inviteResult.Error.Code, inviteResult.Error.Message);
                return;
            }

            var invite = inviteResult.Value;

            if (accept)
            {
                var joinResult = await _gameService.JoinRoomAsync(
                    Guid.Parse(invite.RoomId),
                    userId,
                    user.DisplayName,
                    user.PhotoUrl);

                if (joinResult.IsFailure)
                {
                    _logger.LogWarning("[GameHub] Join room after accept failed: {Error}",
                        joinResult.Error.Message);
                    await SendErrorAsync(joinResult.Error.Code, joinResult.Error.Message);
                    return;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{invite.RoomId}");

                _logger.LogInformation("[GameHub] User:{UserId} accepted invite and joined room:{RoomId}",
                    userId, invite.RoomId);
            }
            else
            {
                _logger.LogInformation("[GameHub] User:{UserId} rejected invite:{InviteId}",
                    userId, inviteId);
            }

            await _hubService.NotifyInviteResponseAsync(invite.HostUserId, userId, accept, invite);
        }

        public async Task<List<InviteStatusDto>?> GetRoomInviteStatuses(string roomId)
        {
            var userId = GetRequiredUserId();
            _logger.LogInformation("[GameHub] User:{UserId} getting invite statuses for room:{RoomId}",
                userId, roomId);

            var roomResult = await _gameService.GetRoomAsync(Guid.Parse(roomId));
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

        public async Task StartFriendBattle(string roomId)
        {
            var hostId = GetRequiredUserId();
            _logger.LogInformation("[GameHub] User:{HostId} starting friend battle room:{RoomId}",
                hostId, roomId);

            var roomResult = await _gameService.GetRoomAsync(Guid.Parse(roomId));
            if (roomResult.IsFailure)
            {
                await SendErrorAsync(roomResult.Error.Code, roomResult.Error.Message);
                return;
            }

            var room = roomResult.Value;
            if (room.HostPlayerId != hostId)
            {
                _logger.LogWarning("[GameHub] User:{UserId} is not host of room:{RoomId}",
                    hostId, roomId);
                await SendErrorAsync("NOT_HOST", "Only the host can start the game");
                return;
            }

            if (room.Players.Count < 2)
            {
                _logger.LogWarning("[GameHub] Not enough players in room:{RoomId}, count:{Count}",
                    roomId, room.Players.Count);
                await SendErrorAsync("NOT_ENOUGH_PLAYERS", "At least one friend must accept to start the game");
                return;
            }

            var startResult = await _gameService.StartGameAsync(Guid.Parse(roomId));
            if (startResult.IsFailure)
            {
                await SendErrorAsync(startResult.Error.Code, startResult.Error.Message);
                return;
            }

            _logger.LogInformation("[GameHub] Friend battle started for room:{RoomId}", roomId);

            await _hubService.NotifyGameStartingAsync(roomId, startResult.Value);

            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Games.Services;
using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.RealTime;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Features.Users;
using System.Security.Claims;

namespace QuizBattle.Infrastructure.Features.RealTime.Hubs
{
    /// <summary>
    /// Base hub containing shared logic for all game modes.
    /// Contains connection lifecycle, room operations, and game flow logic.
    /// </summary>
    [Authorize]
    public abstract class BaseGameHub : Hub<IGameHubClient>
    {
        protected readonly IGameRoomService RoomService;
        protected readonly IGameRoundService RoundService;
        protected readonly IGameNotificationsService HubService;
        protected readonly IConnectionManager ConnectionManager;
        protected readonly IUserQueryRepository UserRepository;
        protected readonly ILogger Logger;

        protected BaseGameHub(
            IGameRoomService roomService,
            IGameRoundService roundService,
            IGameNotificationsService hubService,
            IConnectionManager connectionManager,
            IUserQueryRepository userRepository,
            ILogger logger)
        {
            RoomService = roomService;
            RoundService = roundService;
            HubService = hubService;
            ConnectionManager = connectionManager;
            UserRepository = userRepository;
            Logger = logger;
        }

        #region Connection Lifecycle

        public override async Task OnConnectedAsync()
        {
            var userId = GetRequiredUserId();
            var hubName = GetType().Name;

            Logger.LogInformation("[{HubName}] User:{UserId} connected, ConnectionId:{ConnectionId}",
                hubName, userId, Context.ConnectionId);

            // Add to connection manager
            await ConnectionManager.AddConnectionAsync(userId, Context.ConnectionId);

            // Add to personal user group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
            Logger.LogDebug("[{HubName}] Added user:{UserId} to user group", hubName, userId);

            // Reconnect to room if user was in a game
            await ReconnectToRoomIfExistsAsync(userId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetRequiredUserId();
            var hubName = GetType().Name;

            Logger.LogInformation("[{HubName}] User:{UserId} disconnected, ConnectionId:{ConnectionId}",
                hubName, userId, Context.ConnectionId);

            // Mark player as disconnected in their room
            var roomId = await RoomService.GetPlayerRoomIdAsync(userId);
            if (roomId.HasValue)
            {
                Logger.LogDebug("[{HubName}] User:{UserId} was in room:{RoomId}, marking as disconnected",
                    hubName, userId, roomId.Value);

                await RoomService.SetPlayerConnectedAsync(roomId.Value, userId, false);
                await HubService.NotifyPlayerDisconnectedAsync(roomId.Value.ToString(), userId);
            }

            // Remove from connection manager
            await ConnectionManager.RemoveConnectionAsync(userId, Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }

        private async Task ReconnectToRoomIfExistsAsync(int userId)
        {
            var hubName = GetType().Name;
            var roomId = await RoomService.GetPlayerRoomIdAsync(userId);

            if (!roomId.HasValue)
                return;

            Logger.LogInformation("[{HubName}] User:{UserId} reconnecting to room:{RoomId}",
                hubName, userId, roomId.Value);

            await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomId.Value}");
            await RoomService.SetPlayerConnectedAsync(roomId.Value, userId, true);
            await HubService.NotifyPlayerReconnectedAsync(roomId.Value.ToString(), userId);
        }

        #endregion

        #region Room Operations

        /// <summary>
        /// Join an existing game room.
        /// </summary>
        public virtual async Task<GamePlayerDto?> JoinRoom(string roomId)
        {
            var userId = GetRequiredUserId();
            var hubName = GetType().Name;

            Logger.LogInformation("[{HubName}] User:{UserId} joining room:{RoomId}",
                hubName, userId, roomId);

            var user = await GetUserInfoAsync(userId);

            var result = await RoomService.JoinRoomAsync(
                Guid.Parse(roomId),
                userId,
                user.DisplayName,
                user.PhotoUrl);

            if (result.IsFailure)
            {
                Logger.LogWarning("[{HubName}] Join room failed for user:{UserId}, error:{Error}",
                    hubName, userId, result.Error.Message);
                await SendErrorAsync(result.Error.Code, result.Error.Message);
                return null;
            }

            // Add to room group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomId}");

            // Notify other players
            await HubService.NotifyPlayerJoinedAsync(roomId, result.Value);

            Logger.LogInformation("[{HubName}] User:{UserId} successfully joined room:{RoomId}",
                hubName, userId, roomId);

            return result.Value;
        }

        /// <summary>
        /// Leave the current game room.
        /// </summary>
        public virtual async Task LeaveRoom(string roomId)
        {
            var userId = GetRequiredUserId();
            var hubName = GetType().Name;

            Logger.LogInformation("[{HubName}] User:{UserId} leaving room:{RoomId}",
                hubName, userId, roomId);

            var result = await RoomService.LeaveRoomAsync(Guid.Parse(roomId), userId);

            if (result.IsFailure)
            {
                Logger.LogWarning("[{HubName}] Leave room failed for user:{UserId}, error:{Error}",
                    hubName, userId, result.Error.Message);
                await SendErrorAsync(result.Error.Code, result.Error.Message);
                return;
            }

            // Remove from room group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room:{roomId}");

            // Notify other players
            await HubService.NotifyPlayerLeftAsync(roomId, userId);

            Logger.LogDebug("[{HubName}] Removed user:{UserId} from room:{RoomId} group",
                hubName, userId, roomId);
        }

        /// <summary>
        /// Set player ready status in the room.
        /// </summary>
        public virtual async Task SetReady(string roomId, bool isReady)
        {
            var userId = GetRequiredUserId();
            var hubName = GetType().Name;

            Logger.LogInformation("[{HubName}] User:{UserId} setting ready:{IsReady} in room:{RoomId}",
                hubName, userId, isReady, roomId);

            var result = await RoomService.SetPlayerReadyAsync(Guid.Parse(roomId), userId, isReady);

            if (result.IsFailure)
            {
                Logger.LogWarning("[{HubName}] Set ready failed for user:{UserId}, error:{Error}",
                    hubName, userId, result.Error.Message);
                await SendErrorAsync(result.Error.Code, result.Error.Message);
                return;
            }

            // Notify other players
            await HubService.NotifyPlayerReadyChangedAsync(roomId, userId, isReady);
        }

        #endregion

        #region Game Flow

        /// <summary>
        /// Submit an answer for the current round.
        /// Automatically handles round ending and starting next round.
        /// </summary>
        public virtual async Task SubmitAnswer(string roomId, string answer)
        {
            var userId = GetRequiredUserId();
            var hubName = GetType().Name;

            Logger.LogInformation("[{HubName}] User:{UserId} submitting answer in room:{RoomId}",
                hubName, userId, roomId);

            var result = await RoundService.SubmitAnswerAsync(Guid.Parse(roomId), userId, answer);

            if (result.IsFailure)
            {
                Logger.LogWarning("[{HubName}] Submit answer failed for user:{UserId}, error:{Error}",
                    hubName, userId, result.Error.Message);
                await SendErrorAsync(result.Error.Code, result.Error.Message);
                return;
            }

            // Notify that player answered
            await HubService.NotifyPlayerAnsweredAsync(roomId, userId);

            // If all players answered, end the round
            if (result.Value.AllPlayersAnswered)
            {
                Logger.LogInformation("[{HubName}] All players answered in room:{RoomId}, ending round",
                    hubName, roomId);

                await HandleRoundEndAsync(Guid.Parse(roomId), roomId);
            }
        }

        /// <summary>
        /// Handles round ending and starting next round or ending the game.
        /// </summary>
        protected virtual async Task HandleRoundEndAsync(Guid roomGuid, string roomId)
        {
            var hubName = GetType().Name;

            // End the current round
            var endRoundResult = await RoundService.EndRoundAsync(roomGuid);

            if (endRoundResult.IsFailure)
            {
                Logger.LogError("[{HubName}] Failed to end round in room:{RoomId}, error:{Error}",
                    hubName, roomId, endRoundResult.Error.Message);
                await HubService.NotifyRoomErrorAsync(roomId, endRoundResult.Error.Code, endRoundResult.Error.Message);
                return;
            }

            // Notify round ended with results
            await HubService.NotifyRoundEndedAsync(roomId, endRoundResult.Value);

            // Wait before starting next round
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Check if game should continue
            var room = await RoomService.GetRoomAsync(roomGuid);

            if (room.IsFailure)
            {
                Logger.LogError("[{HubName}] Failed to get room:{RoomId} after round end",
                    hubName, roomId);
                return;
            }

            if (room.Value.Status == (int)GameStatus.RoundInProgress)
            {
                // Start next round
                Logger.LogInformation("[{HubName}] Starting next round in room:{RoomId}",
                    hubName, roomId);

                var startRoundResult = await RoundService.StartRoundAsync(roomGuid);

                if (startRoundResult.IsSuccess)
                {
                    // Convert GameQuestionDto to GameQuestionClientDto (without correct answer)
                    var questionClient = new GameQuestionClientDto(
                        startRoundResult.Value.Question.QuestionId,
                        startRoundResult.Value.Question.RoundNumber,
                        startRoundResult.Value.Question.Text,
                        startRoundResult.Value.Question.OptionA,
                        startRoundResult.Value.Question.OptionB,
                        startRoundResult.Value.Question.OptionC);

                    var roundEvent = new RoundStartedEvent(
                        startRoundResult.Value.CurrentRound,
                        startRoundResult.Value.TotalRounds,
                        questionClient,
                        startRoundResult.Value.RoundEndsAt);

                    await HubService.NotifyRoundStartedAsync(roomId, roundEvent);
                }
            }
            else
            {
                // Game is finished
                Logger.LogInformation("[{HubName}] Game finished in room:{RoomId}, ending game",
                    hubName, roomId);

                var endGameResult = await RoundService.EndGameAsync(roomGuid);

                if (endGameResult.IsSuccess)
                {
                    await HubService.NotifyGameEndedAsync(roomId, endGameResult.Value);
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the authenticated user ID from the JWT token.
        /// Throws HubException if user is not authenticated.
        /// </summary>
        protected int GetRequiredUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Context.User?.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new HubException("User not authenticated");
            }

            return userId;
        }

        /// <summary>
        /// Gets user information from the database.
        /// </summary>
        protected async Task<UserInfo> GetUserInfoAsync(int userId)
        {
            var user = await UserRepository.GetByIdAsync(new UserId(userId))
                ?? throw new HubException($"User {userId} not found");

            var displayName = string.IsNullOrWhiteSpace(user.FullName)
                ? $"Player {user.Id}"
                : user.FullName;

            return new UserInfo(user.Id, displayName, user.Photo);
        }

        /// <summary>
        /// Sends an error message to the calling client.
        /// </summary>
        protected Task SendErrorAsync(string code, string message)
        {
            var hubName = GetType().Name;
            Logger.LogDebug("[{HubName}] Sending error to caller: {Code} - {Message}",
                hubName, code, message);

            return Clients.Caller.Error(code, message);
        }

        #endregion

        protected sealed record UserInfo(int UserId, string DisplayName, string? PhotoUrl);
    }
}
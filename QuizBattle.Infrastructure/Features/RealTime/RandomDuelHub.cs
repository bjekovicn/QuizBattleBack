using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Games.Services;
using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.RealTime;
using QuizBattle.Domain.Features.Games;

namespace QuizBattle.Infrastructure.Features.RealTime.Hubs
{
    /// <summary>
    /// SignalR Hub for Random Duel (1v1 matchmaking) game mode.
    /// Handles matchmaking queue operations for random opponent matching.
    /// </summary>
    public sealed class RandomDuelHub : BaseGameHub
    {
        private readonly IGameMatchmakingService _matchmakingService;

        public RandomDuelHub(
            IGameRoomService roomService,
            IGameRoundService roundService,
            IGameNotificationsService hubService,
            IGameMatchmakingService matchmakingService,
            IConnectionManager connectionManager,
            IUserQueryRepository userRepository,
            ILogger<RandomDuelHub> logger)
            : base(roomService, roundService, hubService, connectionManager, userRepository, logger)
        {
            _matchmakingService = matchmakingService;
        }

        /// <summary>
        /// Join the matchmaking queue to find a random opponent.
        /// Players will be matched based on language preference.
        /// </summary>
        /// <param name="languageCode">Preferred language for questions (e.g., "en", "sr")</param>
        public async Task JoinMatchmaking(string languageCode)
        {
            var userId = GetRequiredUserId();

            Logger.LogInformation("[RandomDuelHub] User:{UserId} joining matchmaking, lang:{Lang}",
                userId, languageCode);

            var user = await GetUserInfoAsync(userId);

            var result = await _matchmakingService.JoinMatchmakingAsync(
                userId,
                user.DisplayName,
                user.PhotoUrl,
                GameType.RandomDuel,
                languageCode);

            if (result.IsFailure)
            {
                Logger.LogWarning("[RandomDuelHub] Matchmaking failed for user:{UserId}, error:{Error}",
                    userId, result.Error.Message);
                await SendErrorAsync(result.Error.Code, result.Error.Message);
                return;
            }

            Logger.LogInformation("[RandomDuelHub] User:{UserId} successfully joined matchmaking queue", userId);
        }

        /// <summary>
        /// Leave the matchmaking queue.
        /// </summary>
        /// <param name="languageCode">Language code used when joining</param>
        public async Task LeaveMatchmaking(string languageCode)
        {
            var userId = GetRequiredUserId();

            Logger.LogInformation("[RandomDuelHub] User:{UserId} leaving matchmaking", userId);

            var result = await _matchmakingService.LeaveMatchmakingAsync(
                userId,
                GameType.RandomDuel,
                languageCode);

            if (result.IsFailure)
            {
                Logger.LogWarning("[RandomDuelHub] Leave matchmaking failed for user:{UserId}, error:{Error}",
                    userId, result.Error.Message);
                await SendErrorAsync(result.Error.Code, result.Error.Message);
                return;
            }

            Logger.LogInformation("[RandomDuelHub] User:{UserId} successfully left matchmaking queue", userId);
        }
    }
}
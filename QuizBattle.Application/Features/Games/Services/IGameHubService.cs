using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Games.Repositories;
using QuizBattle.Application.Shared.Abstractions.RealTime;

namespace QuizBattle.Application.Features.Games.Services
{
    public interface IGameHubService
    {
        // Room events
        Task NotifyRoomCreatedAsync(string roomId, GameRoomDto room, CancellationToken ct = default);
        Task NotifyPlayerJoinedAsync(string roomId, GamePlayerDto player, CancellationToken ct = default);
        Task NotifyPlayerLeftAsync(string roomId, int userId, CancellationToken ct = default);
        Task NotifyPlayerReadyChangedAsync(string roomId, int userId, bool isReady, CancellationToken ct = default);
        Task NotifyPlayerDisconnectedAsync(string roomId, int userId, CancellationToken ct = default);
        Task NotifyPlayerReconnectedAsync(string roomId, int userId, CancellationToken ct = default);

        // Game flow events
        Task NotifyGameStartingAsync(string roomId, GameRoomDto room, CancellationToken ct = default);
        Task NotifyRoundStartedAsync(string roomId, RoundStartedEvent roundEvent, CancellationToken ct = default);
        Task NotifyPlayerAnsweredAsync(string roomId, int userId, CancellationToken ct = default);
        Task NotifyRoundEndedAsync(string roomId, RoundResultDto result, CancellationToken ct = default);
        Task NotifyGameEndedAsync(string roomId, GameResultDto result, CancellationToken ct = default);

        // Matchmaking events
        Task NotifyMatchFoundAsync(List<int> userIds, MatchFoundEvent matchEvent, CancellationToken ct = default);
        Task NotifyMatchmakingUpdateAsync(int userId, MatchmakingUpdateEvent update, CancellationToken ct = default);

        // Invite events
        Task NotifyInviteSentAsync(int hostId, GameInviteDto invite, CancellationToken ct = default);
        Task NotifyInviteReceivedAsync(int invitedUserId, GameInviteDto invite, CancellationToken ct = default);
        Task NotifyInviteResponseAsync(int hostId, int friendId, bool accepted, GameInviteDto invite, CancellationToken ct = default);

        // Error events
        Task NotifyErrorAsync(int userId, string code, string message, CancellationToken ct = default);
        Task NotifyRoomErrorAsync(string roomId, string code, string message, CancellationToken ct = default);

        // Group management
        Task AddUsersToRoomAsync(Guid roomId, IReadOnlyCollection<int> userIds);
        Task RemoveUserFromRoomAsync(Guid roomId, int userId);
    }
}

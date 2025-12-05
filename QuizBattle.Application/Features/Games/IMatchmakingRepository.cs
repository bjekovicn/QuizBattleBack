using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games
{

    public interface IMatchmakingRepository
    {
        /// <summary>
        /// Adds player to matchmaking queue. If enough players are waiting,
        /// creates a game room and returns it.
        /// </summary>
        Task<Result<MatchmakingResult>> JoinQueueAsync(
            int userId,
            string displayName,
            string? photoUrl,
            GameType gameType,
            string languageCode,
            CancellationToken ct = default);

        /// <summary>
        /// Removes player from matchmaking queue.
        /// </summary>
        Task<Result> LeaveQueueAsync(
            int userId,
            GameType gameType,
            string languageCode,
            CancellationToken ct = default);

        /// <summary>
        /// Checks if player is in any queue.
        /// </summary>
        Task<bool> IsInQueueAsync(int userId, CancellationToken ct = default);
    }

    public sealed record MatchmakingResult(
        bool MatchFound,
        string? GameRoomId,
        List<MatchedPlayerDto>? Players);

    public sealed record MatchedPlayerDto(
        int UserId,
        string DisplayName,
        string? PhotoUrl);
}

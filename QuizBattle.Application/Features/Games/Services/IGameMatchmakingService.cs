using QuizBattle.Application.Features.Games.Repositories;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Services
{
    public interface IGameMatchmakingService
    {
        Task<Result<MatchmakingResult>> JoinMatchmakingAsync( 
            int userId,
            string displayName,
            string? photoUrl,
            GameType gameType,
            string languageCode,
            CancellationToken ct = default);

        Task<Result> LeaveMatchmakingAsync( 
            int userId,
            GameType gameType,
            string languageCode,
            CancellationToken ct = default);
    }
}

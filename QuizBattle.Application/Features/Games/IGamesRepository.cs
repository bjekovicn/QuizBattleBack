using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;
using QuizBattle.Application.Shared.Abstractions.Repositories;

namespace QuizBattle.Application.Features.Games
{
   public interface IGamesRepository : IBaseRepository<Game, GameId>
    {
        Task<Result<bool>> AddPlayerAsync(GameId gameId, User user, CancellationToken cancellationToken = default);

        Task<Result<bool>> AddAnswerAsync(GameId gameId, UserId userId, string answer, CancellationToken cancellationToken = default);
    }
}

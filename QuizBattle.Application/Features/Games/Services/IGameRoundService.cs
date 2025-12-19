using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Games.Repositories;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Services
{
    public interface IGameRoundService
    {
        Task<Result<GameRoomDto>> StartGameAsync(Guid roomId, CancellationToken ct = default);
        Task<Result<RoundStartedResult>> StartRoundAsync(Guid roomId, CancellationToken ct = default);
        Task<Result<RoundResultDto>> EndRoundAsync(Guid roomId, CancellationToken ct = default);
        Task<Result<GameResultDto>> EndGameAsync(Guid roomId, CancellationToken ct = default);
        Task<Result<SubmitAnswerResult>> SubmitAnswerAsync(Guid roomId, int userId, string answer, CancellationToken ct = default);

    }

}

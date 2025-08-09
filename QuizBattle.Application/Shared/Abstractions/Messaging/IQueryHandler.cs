using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Shared.Abstractions.Messaging
{
    public interface IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        Task<Result<TResponse>> Handle(TQuery command, CancellationToken token);
    }
}

using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Application.Shared.Abstractions.Repositories;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Shared.Generics.Queries
{
    public record GetAllQuery<TEntity, TId>() : IQuery<IReadOnlyList<TEntity>> where TEntity : Entity<TId>;

    public sealed class GetAllQueryHandler<TEntity, TId> : IQueryHandler<GetAllQuery<TEntity, TId>, IReadOnlyList<TEntity>>
        where TEntity : Entity<TId>
        where TId : IEquatable<TId>
    {
        private readonly IBaseQueryRepository<TEntity, TId> _repository;

        public GetAllQueryHandler(IBaseQueryRepository<TEntity, TId> repository)
        {
            _repository = repository;
        }

        public async Task<Result<IReadOnlyList<TEntity>>> Handle(GetAllQuery<TEntity, TId> query, CancellationToken token)
        {
            var entities = await _repository.GetAllAsync();
            return Result.Success(entities);
        }
    }
}

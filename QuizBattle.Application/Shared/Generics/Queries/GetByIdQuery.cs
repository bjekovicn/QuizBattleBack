using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Application.Shared.Abstractions.Repositories;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Shared.Generics.Queries
{
    public record GetByIdQuery<TEntity, TId>(TId Id) : IQuery<TEntity?> where TEntity : Entity<TId>;

    public sealed class GetByIdQueryHandler<TEntity, TId> : IQueryHandler<GetByIdQuery<TEntity, TId>, TEntity?>
        where TEntity : Entity<TId>
        where TId : IEquatable<TId>
    {
        private readonly IBaseQueryRepository<TEntity, TId> _repository;

        public GetByIdQueryHandler(IBaseQueryRepository<TEntity, TId> repository)
        {
            _repository = repository;
        }

        public async Task<Result<TEntity?>> Handle(GetByIdQuery<TEntity, TId> query, CancellationToken token)
        {
            var entity = await _repository.GetByIdAsync(query.Id);
            return Result.Success(entity);
        }
    }
}

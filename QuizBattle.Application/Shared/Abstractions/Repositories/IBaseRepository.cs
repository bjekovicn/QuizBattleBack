
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Shared.Abstractions.Repositories
{
    public interface IBaseRepository<TEntity, TId> : IBaseQueryRepository<TEntity, TId>, IBaseCommandRepository<TEntity, TId>
        where TEntity : Entity<TId>
        where TId : IEquatable<TId>
    {
    }
}

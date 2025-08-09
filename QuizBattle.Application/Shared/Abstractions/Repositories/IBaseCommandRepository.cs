
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Shared.Abstractions.Repositories
{
    public interface IBaseCommandRepository<TEntity, TId>
        where TEntity : Entity<TId>
        where TId : IEquatable<TId>
    {
        Task AddAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);
    }
}

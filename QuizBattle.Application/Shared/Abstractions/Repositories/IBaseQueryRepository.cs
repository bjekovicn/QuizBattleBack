
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Shared.Abstractions.Repositories
{
    public interface IBaseQueryRepository<TEntity, TId>
        where TEntity : Entity<TId>
        where TId : IEquatable<TId>
    {
        Task<TEntity?> GetByIdAsync(TId id);
        Task<IReadOnlyList<TEntity>> GetAllAsync();
        Task<bool> ExistsAsync(TId id);
    }
}

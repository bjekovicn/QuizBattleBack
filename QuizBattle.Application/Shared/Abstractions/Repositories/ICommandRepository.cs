using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Shared.Abstractions.Repositories
{
    public interface ICommandRepository<TEntity, TId>
            where TEntity : Entity<TId>
            where TId : IEquatable<TId>
    {
        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    }
}

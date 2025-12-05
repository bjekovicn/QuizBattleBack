namespace QuizBattle.Application.Shared.Abstractions.Repositories
{
    public interface IQueryRepository<TResponse, TId>
        where TId : IEquatable<TId>
    {
        Task<TResponse?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TResponse>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
    }
}

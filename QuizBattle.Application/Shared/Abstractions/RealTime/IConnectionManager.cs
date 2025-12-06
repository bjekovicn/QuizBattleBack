namespace QuizBattle.Application.Shared.Abstractions.RealTime
{
    public interface IConnectionManager
    {
        Task AddConnectionAsync(int userId, string connectionId, CancellationToken ct = default);
        Task RemoveConnectionAsync(int userId, string connectionId, CancellationToken ct = default);
        Task<List<string>> GetConnectionsAsync(int userId, CancellationToken ct = default);
        Task<bool> IsUserConnectedAsync(int userId, CancellationToken ct = default);
    }
}

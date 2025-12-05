using QuizBattle.Domain.Features.Auth;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Application.Features.Auth
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByIdAsync(RefreshTokenId id, CancellationToken ct = default);
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
        Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(UserId userId, CancellationToken ct = default);
        Task<IReadOnlyList<RefreshToken>> GetAllByUserIdAsync(UserId userId, CancellationToken ct = default);

        Task AddAsync(RefreshToken token, CancellationToken ct = default);
        void Update(RefreshToken token);

        Task RevokeAllByUserIdAsync(UserId userId, string reason, CancellationToken ct = default);
        Task DeleteExpiredAsync(CancellationToken ct = default);
    }
}

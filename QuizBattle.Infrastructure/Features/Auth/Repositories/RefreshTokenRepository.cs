using QuizBattle.Application.Features.Auth;
using QuizBattle.Domain.Features.Auth;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Infrastructure.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace QuizBattle.Infrastructure.Features.Auth.Repositories
{

    internal sealed class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _dbContext;

        public RefreshTokenRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<RefreshToken?> GetByIdAsync(RefreshTokenId id, CancellationToken ct = default)
        {
            return await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.Id == id, ct);
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        {
            return await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
        }

        public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(UserId userId, CancellationToken ct = default)
        {
            return await _dbContext.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<RefreshToken>> GetAllByUserIdAsync(UserId userId, CancellationToken ct = default)
        {
            return await _dbContext.RefreshTokens
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
        {
            await _dbContext.RefreshTokens.AddAsync(token, ct);
        }

        public void Update(RefreshToken token)
        {
            _dbContext.RefreshTokens.Update(token);
        }

        public async Task RevokeAllByUserIdAsync(UserId userId, string reason, CancellationToken ct = default)
        {
            var activeTokens = await _dbContext.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var token in activeTokens)
            {
                token.Revoke(reason);
            }
        }

        public async Task DeleteExpiredAsync(CancellationToken ct = default)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-30); // Keep revoked tokens for 30 days for audit

            await _dbContext.RefreshTokens
                .Where(t => t.ExpiresAt < cutoffDate || t.RevokedAt != null && t.RevokedAt < cutoffDate)
                .ExecuteDeleteAsync(ct);
        }
    }
}

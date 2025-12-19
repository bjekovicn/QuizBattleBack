using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Features.Users;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Infrastructure.Shared.Persistence;

namespace QuizBattle.Infrastructure.Features.Users.Repositories
{
    internal sealed class UserCommandRepository : IUserCommandRepository
    {
        private readonly AppDbContext _dbContext;

        public UserCommandRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(User entity, CancellationToken cancellationToken = default)
        {
            await _dbContext.Users.AddAsync(entity, cancellationToken);
        }

        public void Update(User entity)
        {
            _dbContext.Users.Update(entity);
        }

        public void Delete(User entity)
        {
            _dbContext.Users.Remove(entity);
        }

        public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .Include(u => u.DeviceTokens)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .Include(u => u.DeviceTokens)
                .FirstOrDefaultAsync(u => u.GoogleId == googleId, cancellationToken);
        }

        public async Task<User?> GetByAppleIdAsync(string appleId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .Include(u => u.DeviceTokens)
                .FirstOrDefaultAsync(u => u.AppleId == appleId, cancellationToken);
        }

        public async Task<List<User>> GetByIdsAsync(IEnumerable<UserId> userIds, CancellationToken cancellationToken = default)
        {
            var ids = userIds.Select(id => id.Value).ToList();

            return await _dbContext.Users
                .Include(u => u.DeviceTokens)
                .Where(u => ids.Contains(u.Id.Value))
                .ToListAsync(cancellationToken);
        }
    }
}

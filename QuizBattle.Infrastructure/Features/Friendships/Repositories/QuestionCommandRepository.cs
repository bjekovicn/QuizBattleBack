using Microsoft.EntityFrameworkCore;
using QuizBattle.Application.Features.Friendships;
using QuizBattle.Domain.Features.Friendships;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Infrastructure.Shared.Persistence;

namespace QuizBattle.Infrastructure.Features.Friendships.Repositories
{

    internal sealed class FriendshipCommandRepository : IFriendshipCommandRepository
    {
        private readonly AppDbContext _dbContext;

        public FriendshipCommandRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Friendship?> GetAsync(UserId senderId, UserId receiverId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Friendships
                .FirstOrDefaultAsync(f =>
                    f.SenderId == senderId && f.ReceiverId == receiverId ||
                    f.SenderId == receiverId && f.ReceiverId == senderId,
                    cancellationToken);
        }

        public async Task<bool> ExistsAsync(UserId senderId, UserId receiverId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Friendships
                .AnyAsync(f =>
                    f.SenderId == senderId && f.ReceiverId == receiverId ||
                    f.SenderId == receiverId && f.ReceiverId == senderId,
                    cancellationToken);
        }

        public async Task AddAsync(Friendship entity, CancellationToken cancellationToken = default)
        {
            await _dbContext.Friendships.AddAsync(entity, cancellationToken);
        }

        public void Update(Friendship entity)
        {
            _dbContext.Friendships.Update(entity);
        }

        public void Delete(Friendship entity)
        {
            _dbContext.Friendships.Remove(entity);
        }
    }
}

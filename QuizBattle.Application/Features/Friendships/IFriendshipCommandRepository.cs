using QuizBattle.Domain.Features.Friendships;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Application.Features.Friendships
{
    public interface IFriendshipCommandRepository 
    {
        Task AddAsync(Friendship entity, CancellationToken cancellationToken = default);
        void Delete(Friendship entity);
        void Update(Friendship entity);
        Task<Friendship?> GetAsync(UserId senderId, UserId receiverId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(UserId senderId, UserId receiverId, CancellationToken cancellationToken = default);
    }
}

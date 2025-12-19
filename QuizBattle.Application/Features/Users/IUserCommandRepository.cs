using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Application.Features.Users
{
    public interface IUserCommandRepository
    {
        Task AddAsync(User entity, CancellationToken cancellationToken = default);
        void Update(User entity);
        void Delete(User entity);
        Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
        Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default);
        Task<User?> GetByAppleIdAsync(string appleId, CancellationToken cancellationToken = default);
        Task<List<User>> GetByIdsAsync(IEnumerable<UserId> userIds, CancellationToken cancellationToken = default);
    }
}

using QuizBattle.Application.Shared.Abstractions.Repositories;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Application.Features.Users
{
    public interface IUserCommandRepository : ICommandRepository<User, UserId>
    {
        Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default);
        Task<User?> GetByAppleIdAsync(string appleId, CancellationToken cancellationToken = default);
    }
}

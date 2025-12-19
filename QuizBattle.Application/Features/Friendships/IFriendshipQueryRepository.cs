using QuizBattle.Application.Features.Users;


namespace QuizBattle.Application.Features.Friendships
{
    public interface IFriendshipQueryRepository
    {
        Task<IReadOnlyList<UserResponse>> GetFriendsAsync(int userId, CancellationToken cancellationToken = default);
    }
}

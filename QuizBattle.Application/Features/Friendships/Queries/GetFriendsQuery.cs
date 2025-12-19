using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Friendships.Queries
{
    public sealed record GetFriendsQuery(int UserId) : IQuery<IReadOnlyList<UserResponse>>;

    internal sealed class GetFriendsQueryHandler : IQueryHandler<GetFriendsQuery, IReadOnlyList<UserResponse>>
    {
        private readonly IFriendshipQueryRepository _repository;

        public GetFriendsQueryHandler(IFriendshipQueryRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<IReadOnlyList<UserResponse>>> Handle(
            GetFriendsQuery query,
            CancellationToken cancellationToken)
        {
            var friends = await _repository.GetFriendsAsync(query.UserId, cancellationToken);
            return Result.Success(friends);
        }
    }
}

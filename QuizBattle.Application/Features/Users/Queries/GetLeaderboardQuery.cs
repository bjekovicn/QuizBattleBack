using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Users.Queries
{
    public sealed record GetLeaderboardQuery(int Take = 10) : IQuery<IReadOnlyList<UserResponse>>;

    internal sealed class GetLeaderboardQueryHandler : IQueryHandler<GetLeaderboardQuery, IReadOnlyList<UserResponse>>
    {
        private readonly IUserQueryRepository _repository;

        public GetLeaderboardQueryHandler(IUserQueryRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<IReadOnlyList<UserResponse>>> Handle(
            GetLeaderboardQuery query,
            CancellationToken cancellationToken)
        {
            var users = await _repository.GetLeaderboardAsync(query.Take, cancellationToken);
            return Result.Success(users);
        }
    }
}

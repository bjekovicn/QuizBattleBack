using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Users.Queries
{

    public sealed record GetAllUsersQuery(int? Skip = null, int? Take = null) : IQuery<IReadOnlyList<UserResponse>>;

    internal sealed class GetAllUsersQueryHandler : IQueryHandler<GetAllUsersQuery, IReadOnlyList<UserResponse>>
    {
        private readonly IUserQueryRepository _repository;

        public GetAllUsersQueryHandler(IUserQueryRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<IReadOnlyList<UserResponse>>> Handle(
            GetAllUsersQuery query,
            CancellationToken cancellationToken)
        {
            var users = await _repository.GetAllAsync(query.Skip, query.Take, cancellationToken);
            return Result.Success(users);
        }
    }
}

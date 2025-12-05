using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Users.Queries
{

    public sealed record GetUserByGoogleIdQuery(string GoogleId) : IQuery<UserResponse>;

    internal sealed class GetUserByGoogleIdQueryHandler : IQueryHandler<GetUserByGoogleIdQuery, UserResponse>
    {
        private readonly IUserQueryRepository _repository;

        public GetUserByGoogleIdQueryHandler(IUserQueryRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<UserResponse>> Handle(
            GetUserByGoogleIdQuery query,
            CancellationToken cancellationToken)
        {
            var user = await _repository.GetByGoogleIdAsync(query.GoogleId, cancellationToken);

            return user is null
                ? Result.Failure<UserResponse>(Error.UserNotFound)
                : Result.Success(user);
        }
    }
}

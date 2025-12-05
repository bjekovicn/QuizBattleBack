using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Users.Queries
{
    public sealed record GetUserByIdQuery(int UserId) : IQuery<UserResponse>;

    internal sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserResponse>
    {
        private readonly IUserQueryRepository _repository;

        public GetUserByIdQueryHandler(IUserQueryRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<UserResponse>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
        {
            var user = await _repository.GetByIdAsync(new UserId(query.UserId), cancellationToken);

            return user is null
                ? Result.Failure<UserResponse>(Error.UserNotFound)
                : Result.Success(user);
        }
    }
}

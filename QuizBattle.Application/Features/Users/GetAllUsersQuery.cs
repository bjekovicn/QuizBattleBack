using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Application.Shared.Generics.Queries;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Application.Features.Users
{
    public sealed record GetAllUsersQuery() : GetAllQuery<User, UserId>, IQuery<IReadOnlyList<User>>;
}

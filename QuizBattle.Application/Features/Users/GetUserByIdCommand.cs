using QuizBattle.Application.Shared.Generics.Queries;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Application.Features.Users
{
    public sealed record GetUserByIdQuery(UserId UserId) : GetByIdQuery<User, UserId>(UserId);
}

using QuizBattle.Application.Shared.Generics.Commands;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Application.Features.Users
{
    public sealed record DeleteUserCommand(UserId UserId) : DeleteCommand<UserId>(UserId);
}

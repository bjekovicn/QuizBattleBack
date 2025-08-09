using QuizBattle.Application.Shared.Generics.Commands;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Application.Features.Users
{
    public sealed record CreateUserCommand(
    string? FirstName,
    string? LastName,
    string? Photo,
    string GoogleId) : CreateCommand<User, UserId>
    {
        public override User ToDomainModel()
        {
            var UserId = new UserId(0);
            return new User(UserId, GoogleId, FirstName, LastName, Photo);
        }
    }
}

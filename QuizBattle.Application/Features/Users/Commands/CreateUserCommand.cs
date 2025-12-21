using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Users.Commands
{
    public sealed record CreateUserCommand(
        string? GoogleId,
        string? AppleId,
        string? FirstName,
        string? LastName,
        string? Photo,
        string? Email) : ICommand<int>;

    internal sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, int>
    {
        private readonly IUserCommandRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateUserCommandHandler(IUserCommandRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<int>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
        {
            // Check if user already exists
            if (!string.IsNullOrWhiteSpace(command.GoogleId))
            {
                var existingByGoogle = await _repository.GetByGoogleIdAsync(command.GoogleId, cancellationToken);
                if (existingByGoogle is not null)
                    return Result.Failure<int>(Error.UserAlreadyExists);
            }

            if (!string.IsNullOrWhiteSpace(command.AppleId))
            {
                var existingByApple = await _repository.GetByAppleIdAsync(command.AppleId, cancellationToken);
                if (existingByApple is not null)
                    return Result.Failure<int>(Error.UserAlreadyExists);
            }

            User user;
            if (!string.IsNullOrWhiteSpace(command.GoogleId))
            {
                user = User.CreateWithGoogle(
                    command.GoogleId,
                    command.FirstName,
                    command.LastName,
                    command.Photo,
                    command.Email);
            }
            else if (!string.IsNullOrWhiteSpace(command.AppleId))
            {
                user = User.CreateWithApple(
                    command.AppleId,
                    command.FirstName,
                    command.LastName,
                    command.Email);
            }
            else
            {
                return Result.Failure<int>(new Error("User.NoProvider", "GoogleId or AppleId is required."));
            }

            await _repository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(user.Id.Value);
        }
    }
}

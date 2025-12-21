using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Users.Commands
{

    public sealed record LoginUserCommand(
        string? GoogleId,
        string? AppleId,
        string? FirstName,
        string? LastName,
        string? Photo,
        string? Email,
        string? DeviceToken,
        DevicePlatform? DevicePlatform) : ICommand<UserResponse>;

    internal sealed class LoginUserCommandHandler : ICommandHandler<LoginUserCommand, UserResponse>
    {
        private readonly IUserCommandRepository _commandRepository;
        private readonly IUserQueryRepository _queryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public LoginUserCommandHandler(
            IUserCommandRepository commandRepository,
            IUserQueryRepository queryRepository,
            IUnitOfWork unitOfWork)
        {
            _commandRepository = commandRepository;
            _queryRepository = queryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UserResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
        {
            User? user = null;

            // Try to find existing user
            if (!string.IsNullOrWhiteSpace(command.GoogleId))
            {
                user = await _commandRepository.GetByGoogleIdAsync(command.GoogleId, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(command.AppleId))
            {
                user = await _commandRepository.GetByAppleIdAsync(command.AppleId, cancellationToken);
            }

            // Create new user if not found
            if (user is null)
            {
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
                    return Result.Failure<UserResponse>(new Error("User.NoProvider", "GoogleId or AppleId is required."));
                }

                await _commandRepository.AddAsync(user, cancellationToken);
            }
            else
            {
                // Update existing user profile
                user.UpdateProfile(
                    command.FirstName ?? user.FirstName,
                    command.LastName ?? user.LastName,
                    command.Photo ?? user.Photo);
                user.UpdateLastLogin();
            }

            // Register device token if provided
            if (!string.IsNullOrWhiteSpace(command.DeviceToken) && command.DevicePlatform.HasValue)
            {
                user.AddDeviceToken(command.DeviceToken, command.DevicePlatform.Value);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Return user response
            var response = await _queryRepository.GetByIdAsync(user.Id, cancellationToken);
            return Result.Success(response!);
        }
    }
}

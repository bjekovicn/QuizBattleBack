using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Users.Commands
{

    public sealed record RegisterDeviceTokenCommand(
        int UserId,
        string Token,
        DevicePlatform Platform) : ICommand;

    internal sealed class RegisterDeviceTokenCommandHandler : ICommandHandlerMediatR<RegisterDeviceTokenCommand>
    {
        private readonly IUserCommandRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterDeviceTokenCommandHandler(IUserCommandRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(RegisterDeviceTokenCommand command, CancellationToken cancellationToken)
        {
            var user = await _repository.GetByIdAsync(new UserId(command.UserId), cancellationToken);
            if (user is null)
                return Result.Failure(Error.UserNotFound);

            user.AddDeviceToken(command.Token, command.Platform);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}

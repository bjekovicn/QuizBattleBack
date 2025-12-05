using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Users.Commands
{
    public sealed record RemoveDeviceTokenCommand(int UserId, string Token) : ICommand;

    internal sealed class RemoveDeviceTokenCommandHandler : ICommandHandlerMediatR<RemoveDeviceTokenCommand>
    {
        private readonly IUserCommandRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveDeviceTokenCommandHandler(IUserCommandRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(RemoveDeviceTokenCommand command, CancellationToken cancellationToken)
        {
            var user = await _repository.GetByIdAsync(new UserId(command.UserId), cancellationToken);
            if (user is null)
                return Result.Failure(Error.UserNotFound);

            user.RemoveDeviceToken(command.Token);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}

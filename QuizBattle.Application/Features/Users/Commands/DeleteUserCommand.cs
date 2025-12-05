using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Users.Commands
{
    public sealed record DeleteUserCommand(int UserId) : ICommand;

    internal sealed class DeleteUserCommandHandler : ICommandHandlerMediatR<DeleteUserCommand>
    {
        private readonly IUserCommandRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteUserCommandHandler(IUserCommandRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
        {
            var user = await _repository.GetByIdAsync(new UserId(command.UserId), cancellationToken);
            if (user is null)
                return Result.Failure(Error.UserNotFound);

            _repository.Delete(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}

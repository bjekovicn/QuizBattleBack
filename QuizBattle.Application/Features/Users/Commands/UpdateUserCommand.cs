using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Users.Commands
{

    public sealed record UpdateUserCommand(
        int UserId,
        string? FirstName,
        string? LastName,
        string? Photo) : ICommand;

    internal sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
    {
        private readonly IUserCommandRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateUserCommandHandler(IUserCommandRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
        {
            var user = await _repository.GetByIdAsync(new UserId(command.UserId), cancellationToken);
            if (user is null)
                return Result.Failure(Error.UserNotFound);

            user.UpdateProfile(command.FirstName, command.LastName, command.Photo);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}

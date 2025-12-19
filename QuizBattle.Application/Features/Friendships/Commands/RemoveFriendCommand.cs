using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Friendships.Commands
{
    public sealed record RemoveFriendCommand(int UserId, int FriendId) : ICommand;

    internal sealed class RemoveFriendCommandHandler : ICommandHandler<RemoveFriendCommand>
    {
        private readonly IFriendshipCommandRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveFriendCommandHandler(IFriendshipCommandRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }


        public async Task<Result> Handle(RemoveFriendCommand command, CancellationToken cancellationToken)
        {
            var userId = UserId.Create(command.UserId);
            var friendId = UserId.Create(command.FriendId);

            var friendship = await _repository.GetAsync(userId, friendId, cancellationToken);
            if (friendship is null)
            {
                return Result.Failure(Error.FriendshipNotFound);
            }

            _repository.Delete(friendship);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}

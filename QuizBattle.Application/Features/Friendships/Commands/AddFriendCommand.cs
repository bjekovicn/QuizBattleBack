
using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Friendships;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Friendships.Commands
{
    public sealed record AddFriendCommand(int SenderId, int ReceiverId) : ICommand;

    internal sealed class AddFriendCommandHandler : ICommandHandler<AddFriendCommand>
    {
        private readonly IFriendshipCommandRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public AddFriendCommandHandler(IFriendshipCommandRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(AddFriendCommand command, CancellationToken cancellationToken)
        {
            var senderId = UserId.Create(command.SenderId);
            var receiverId = UserId.Create(command.ReceiverId);

            if (senderId == receiverId)
            {
                return Result.Failure(Error.CannotAddYourself);
            }

            var exists = await _repository.ExistsAsync(senderId, receiverId, cancellationToken);
            if (exists)
            {
                return Result.Failure(Error.FriendshipAlreadyExists);
            }

            var friendship = Friendship.Create(senderId, receiverId);
            await _repository.AddAsync(friendship, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}

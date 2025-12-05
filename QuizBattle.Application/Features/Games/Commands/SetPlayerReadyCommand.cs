using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Commands
{

    public sealed record SetPlayerReadyCommand(
        Guid RoomId,
        int UserId,
        bool IsReady) : ICommand;

    internal sealed class SetPlayerReadyCommandHandler : ICommandHandlerMediatR<SetPlayerReadyCommand>
    {
        private readonly IGameRoomRepository _repository;

        public SetPlayerReadyCommandHandler(IGameRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result> Handle(SetPlayerReadyCommand command, CancellationToken cancellationToken)
        {
            return await _repository.SetPlayerReadyAsync(
                GameRoomId.Create(command.RoomId),
                command.UserId,
                command.IsReady,
                cancellationToken);
        }
    }
}

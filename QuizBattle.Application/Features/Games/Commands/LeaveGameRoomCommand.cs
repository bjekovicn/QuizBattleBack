using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Commands
{

    public sealed record LeaveGameRoomCommand(Guid RoomId, int UserId) : ICommand;

    internal sealed class LeaveGameRoomCommandHandler : ICommandHandlerMediatR<LeaveGameRoomCommand>
    {
        private readonly IGameRoomRepository _repository;

        public LeaveGameRoomCommandHandler(IGameRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result> Handle(LeaveGameRoomCommand command, CancellationToken cancellationToken)
        {
            return await _repository.LeaveRoomAsync(
                GameRoomId.Create(command.RoomId),
                command.UserId,
                cancellationToken);
        }
    }
}

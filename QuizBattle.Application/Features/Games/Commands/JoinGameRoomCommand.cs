using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Commands
{

    public sealed record JoinGameRoomCommand(
        Guid RoomId,
        int UserId,
        string DisplayName,
        string? PhotoUrl) : ICommand<GamePlayerDto>;

    internal sealed class JoinGameRoomCommandHandler : ICommandHandlerMediatR<JoinGameRoomCommand, GamePlayerDto>
    {
        private readonly IGameRoomRepository _repository;

        public JoinGameRoomCommandHandler(IGameRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<GamePlayerDto>> Handle(
            JoinGameRoomCommand command,
            CancellationToken cancellationToken)
        {
            return await _repository.JoinRoomAsync(
                GameRoomId.Create(command.RoomId),
                command.UserId,
                command.DisplayName,
                command.PhotoUrl,
                cancellationToken);
        }
    }
}

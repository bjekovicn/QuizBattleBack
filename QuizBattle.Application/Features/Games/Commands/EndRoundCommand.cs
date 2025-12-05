using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Commands
{
    public sealed record EndRoundCommand(Guid RoomId) : ICommand<RoundResultDto>;

    internal sealed class EndRoundCommandHandler : ICommandHandlerMediatR<EndRoundCommand, RoundResultDto>
    {
        private readonly IGameRoomRepository _repository;

        public EndRoundCommandHandler(IGameRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<RoundResultDto>> Handle(EndRoundCommand command, CancellationToken cancellationToken)
        {
            return await _repository.EndRoundAsync(
                GameRoomId.Create(command.RoomId),
                cancellationToken);
        }
    }
}

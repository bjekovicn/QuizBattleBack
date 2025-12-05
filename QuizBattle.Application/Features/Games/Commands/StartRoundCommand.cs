using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Commands
{
    public sealed record StartRoundCommand(Guid RoomId) : ICommand<GameQuestionDto>;

    internal sealed class StartRoundCommandHandler : ICommandHandlerMediatR<StartRoundCommand, GameQuestionDto>
    {
        private readonly IGameRoomRepository _repository;

        public StartRoundCommandHandler(IGameRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<GameQuestionDto>> Handle(StartRoundCommand command, CancellationToken cancellationToken)
        {
            return await _repository.StartNextRoundAsync(
                GameRoomId.Create(command.RoomId),
                cancellationToken);
        }
    }
}

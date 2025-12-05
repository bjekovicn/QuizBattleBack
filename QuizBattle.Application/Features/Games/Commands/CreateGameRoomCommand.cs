using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Commands
{

    public sealed record CreateGameRoomCommand(
        GameType GameType,
        string LanguageCode,
        int TotalRounds = 10) : ICommand<GameRoomDto>;

    internal sealed class CreateGameRoomCommandHandler : ICommandHandlerMediatR<CreateGameRoomCommand, GameRoomDto>
    {
        private readonly IGameRoomRepository _repository;

        public CreateGameRoomCommandHandler(IGameRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<GameRoomDto>> Handle(
            CreateGameRoomCommand command,
            CancellationToken cancellationToken)
        {
            return await _repository.CreateRoomAsync(
                command.GameType,
                command.LanguageCode,
                command.TotalRounds,
                cancellationToken);
        }
    }
}

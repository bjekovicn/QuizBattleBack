using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Commands
{
    public sealed record LeaveMatchmakingCommand(
        int UserId,
        GameType GameType,
        string LanguageCode) : ICommand;

    internal sealed class LeaveMatchmakingCommandHandler : ICommandHandlerMediatR<LeaveMatchmakingCommand>
    {
        private readonly IMatchmakingRepository _repository;

        public LeaveMatchmakingCommandHandler(IMatchmakingRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result> Handle(LeaveMatchmakingCommand command, CancellationToken cancellationToken)
        {
            return await _repository.LeaveQueueAsync(
                command.UserId,
                command.GameType,
                command.LanguageCode,
                cancellationToken);
        }
    }
}

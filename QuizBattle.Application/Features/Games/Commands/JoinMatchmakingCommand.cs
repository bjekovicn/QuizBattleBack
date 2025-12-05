using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Commands
{
    public sealed record JoinMatchmakingCommand(
        int UserId,
        string DisplayName,
        string? PhotoUrl,
        GameType GameType,
        string LanguageCode) : ICommand<MatchmakingResult>;

    internal sealed class JoinMatchmakingCommandHandler : ICommandHandlerMediatR<JoinMatchmakingCommand, MatchmakingResult>
    {
        private readonly IMatchmakingRepository _repository;

        public JoinMatchmakingCommandHandler(IMatchmakingRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<MatchmakingResult>> Handle(
            JoinMatchmakingCommand command,
            CancellationToken cancellationToken)
        {
            return await _repository.JoinQueueAsync(
                command.UserId,
                command.DisplayName,
                command.PhotoUrl,
                command.GameType,
                command.LanguageCode,
                cancellationToken);
        }
    }
}

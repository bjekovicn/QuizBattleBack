using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Commands
{
    public sealed record SubmitAnswerCommand(
     Guid RoomId,
     int UserId,
     string Answer) : ICommand<SubmitAnswerResult>;

    internal sealed class SubmitAnswerCommandHandler : ICommandHandlerMediatR<SubmitAnswerCommand, SubmitAnswerResult>
    {
        private readonly IGameRoomRepository _repository;

        public SubmitAnswerCommandHandler(IGameRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<SubmitAnswerResult>> Handle(
            SubmitAnswerCommand command,
            CancellationToken cancellationToken)
        {
            return await _repository.SubmitAnswerAsync(
                GameRoomId.Create(command.RoomId),
                command.UserId,
                command.Answer,
                cancellationToken);
        }
    }
}

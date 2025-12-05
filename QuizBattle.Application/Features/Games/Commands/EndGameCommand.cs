using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Commands
{
    public sealed record EndGameCommand(Guid RoomId) : ICommand<GameResultDto>;

    internal sealed class EndGameCommandHandler : ICommandHandlerMediatR<EndGameCommand, GameResultDto>
    {
        private readonly IGameRoomRepository _gameRepository;
        private readonly IUserCommandRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public EndGameCommandHandler(
            IGameRoomRepository gameRepository,
            IUserCommandRepository userRepository,
            IUnitOfWork unitOfWork)
        {
            _gameRepository = gameRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<GameResultDto>> Handle(EndGameCommand command, CancellationToken cancellationToken)
        {
            var result = await _gameRepository.EndGameAsync(
                GameRoomId.Create(command.RoomId),
                cancellationToken);

            if (result.IsFailure)
                return result;

            var gameResult = result.Value;

            // Update user statistics
            foreach (var standing in gameResult.FinalStandings)
            {
                var user = await _userRepository.GetByIdAsync(new UserId(standing.UserId), cancellationToken);
                if (user is null) continue;

                if (standing.Position == 1)
                {
                    user.RecordWin();
                    user.AddCoins(gameResult.TotalRounds * 10); // Bonus coins for winner
                }
                else
                {
                    user.RecordLoss();
                }

                // Everyone gets some coins based on score
                user.AddCoins(standing.TotalScore / 100);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return result;
        }
    }
}

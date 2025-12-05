using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Questions;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Commands
{
    public sealed record StartGameCommand(Guid RoomId) : ICommand<GameRoomDto>;

    internal sealed class StartGameCommandHandler : ICommandHandlerMediatR<StartGameCommand, GameRoomDto>
    {
        private readonly IGameRoomRepository _gameRepository;
        private readonly IQuestionQueryRepository _questionRepository;

        public StartGameCommandHandler(
            IGameRoomRepository gameRepository,
            IQuestionQueryRepository questionRepository)
        {
            _gameRepository = gameRepository;
            _questionRepository = questionRepository;
        }

        public async Task<Result<GameRoomDto>> Handle(StartGameCommand command, CancellationToken cancellationToken)
        {
            var roomId = GameRoomId.Create(command.RoomId);
            var room = await _gameRepository.GetByIdAsync(roomId, cancellationToken);

            if (room is null)
                return Result.Failure<GameRoomDto>(Error.GameNotFound);

            // Fetch random questions
            var questions = await _questionRepository.GetRandomQuestionsAsync(
                room.LanguageCode,
                room.TotalRounds,
                cancellationToken);

            if (questions.Count < room.TotalRounds)
                return Result.Failure<GameRoomDto>(Error.NotEnoughQuestions);

            // Convert to game questions with shuffled answers
            var gameQuestions = questions.Select((q, index) =>
            {
                var gq = GameQuestion.CreateShuffled(
                    q.Id,
                    index + 1,
                    q.Text,
                    q.AnswerA,  // Correct answer
                    q.AnswerB,
                    q.AnswerC);

                return new GameQuestionDto
                {
                    QuestionId = gq.QuestionId,
                    RoundNumber = gq.RoundNumber,
                    Text = gq.Text,
                    OptionA = gq.OptionA,
                    OptionB = gq.OptionB,
                    OptionC = gq.OptionC,
                    CorrectOption = gq.CorrectOption
                };
            }).ToList();

            return await _gameRepository.StartGameAsync(roomId, gameQuestions, cancellationToken);
        }
    }

}

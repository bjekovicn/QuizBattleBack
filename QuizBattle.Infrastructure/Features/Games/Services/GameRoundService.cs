using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Games.Repositories;
using QuizBattle.Application.Features.Games.Services;
using QuizBattle.Application.Features.Questions;
using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Infrastructure.Features.Games.Services;

internal sealed class GameRoundService : IGameRoundService
{
    private readonly IGameRoomRepository _gameRepository;
    private readonly IQuestionQueryRepository _questionRepository;
    private readonly IGameTimerService _timerService;
    private readonly IUserCommandRepository _userRepository;
    private readonly ILogger<GameRoundService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public GameRoundService(
        IUserCommandRepository userRepository,
        IGameRoomRepository gameRepository,
        IQuestionQueryRepository questionRepository,
        IGameTimerService timerService,
        IUnitOfWork unitOfWork,
        ILogger<GameRoundService> logger)
    {
        _userRepository = userRepository;
        _gameRepository = gameRepository;
        _questionRepository = questionRepository;
        _timerService = timerService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<GameRoomDto>> StartGameAsync(Guid roomId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting game for room {RoomId}", roomId);

        var room = await _gameRepository.GetByIdAsync(GameRoomId.Create(roomId), ct);
        if (room is null)
        {
            _logger.LogWarning("Room {RoomId} not found", roomId);
            return Result.Failure<GameRoomDto>(Error.GameNotFound);
        }

        var questions = await _questionRepository.GetRandomQuestionsAsync(room.LanguageCode, room.TotalRounds, ct);
        if (questions.Count < room.TotalRounds)
        {
            _logger.LogWarning("Not enough questions for room {RoomId}. Need {Required}, got {Available}",
                roomId, room.TotalRounds, questions.Count);
            return Result.Failure<GameRoomDto>(Error.NotEnoughQuestions);
        }

        var gameQuestions = questions.Select((q, i) =>
        {
            var gq = GameQuestion.CreateShuffled(q.Id, i + 1, q.Text, q.AnswerA, q.AnswerB, q.AnswerC);
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

        var result = await _gameRepository.StartGameAsync(GameRoomId.Create(roomId), gameQuestions, ct);
        if (result.IsSuccess)
        {
            _logger.LogInformation("Game started successfully for room {RoomId}, scheduling first round", roomId);
            await _timerService.ScheduleFirstRoundAsync(roomId, ct);
        }
        else
        {
            _logger.LogWarning("Failed to start game for room {RoomId}: {Error}", roomId, result.Error);
        }

        return result;
    }

    public async Task<Result<RoundStartedResult>> StartRoundAsync(Guid roomId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting next round for room {RoomId}", roomId);

        var result = await _gameRepository.StartNextRoundAsync(GameRoomId.Create(roomId), ct);
        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to start round for room {RoomId}: {Error}", roomId, result.Error);
            return Result.Failure<RoundStartedResult>(result.Error);
        }

        var room = await _gameRepository.GetByIdAsync(GameRoomId.Create(roomId), ct);
        if (room is null)
        {
            _logger.LogWarning("Room {RoomId} not found after starting round", roomId);
            return Result.Failure<RoundStartedResult>(Error.GameNotFound);
        }

        _logger.LogInformation("Round {CurrentRound}/{TotalRounds} started for room {RoomId}",
            room.CurrentRound, room.TotalRounds, roomId);

        return Result.Success(new RoundStartedResult(
            result.Value,
            room.CurrentRound,
            room.TotalRounds,
            room.RoundEndsAt ?? 0));
    }

    public async Task<Result<RoundResultDto>> EndRoundAsync(Guid roomId, CancellationToken ct = default)
    {
        _logger.LogInformation("Ending round for room {RoomId}", roomId);
        return await _gameRepository.EndRoundAsync(GameRoomId.Create(roomId), ct);
    }

    public async Task<Result<GameResultDto>> EndGameAsync(
        Guid roomId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Ending game for room {RoomId}", roomId);

        var result = await _gameRepository.EndGameAsync(GameRoomId.Create(roomId), ct);
        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to end game for room {RoomId}: {Error}", roomId, result.Error);
            return result;
        }

        _logger.LogInformation("Game ended for room {RoomId}, processing rewards", roomId);

        _logger.LogInformation("Processing rewards for {PlayerCount} players",
                result.Value.FinalStandings.Count);

        foreach (var standing in result.Value.FinalStandings)
        {
            var user = await _userRepository.GetByIdAsync(new UserId(standing.UserId), ct);
            if (user is null)
            {
                _logger.LogWarning("User {UserId} not found for reward processing", standing.UserId);
                continue;
            }

            if (standing.Position == 1)
            {
                var winReward = result.Value.TotalRounds * 10;
                user.RecordWin();
                user.AddCoins(winReward);
                _logger.LogInformation("User {UserId} won! Awarded {Coins} coins",
                    standing.UserId, winReward);
            }
            else
            {
                user.RecordLoss();
                _logger.LogInformation("User {UserId} lost (Position: {Position})",
                    standing.UserId, standing.Position);
            }

            var scoreReward = standing.TotalScore / 100;
            user.AddCoins(scoreReward);
            _logger.LogInformation("User {UserId} awarded {Coins} coins for score {Score}",
                standing.UserId, scoreReward, standing.TotalScore);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Rewards saved successfully");

        return result;
    }

    public async Task<Result<SubmitAnswerResult>> SubmitAnswerAsync(
         Guid roomId,
         int userId,
         string answer,
         CancellationToken ct = default)
    {
        _logger.LogInformation("User {UserId} submitting answer '{Answer}' in room {RoomId}",
            userId, answer, roomId);

        var result = await _gameRepository.SubmitAnswerAsync(
            GameRoomId.Create(roomId),
            userId,
            answer,
            ct);

        if (result.IsSuccess && result.Value.AllPlayersAnswered)
        {
            _logger.LogInformation("All players answered in room {RoomId}, forcing round end", roomId);
            await _timerService.ForceEndRoundAsync(roomId, ct);
        }

        return result;
    }
}
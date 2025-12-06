using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Games;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Questions;
using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.RealTime;
using QuizBattle.Application.Shared.Abstractions.Services;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Infrastructure.Features.Games.Services;

internal sealed class GameService : IGameService
{
    private readonly IGameRoomRepository _gameRepository;
    private readonly IMatchmakingRepository _matchmakingRepository;
    private readonly IQuestionQueryRepository _questionRepository;
    private readonly IUserCommandRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGameTimerService _timerService;
    private readonly IGameHubService _hubService;
    private readonly ILogger<GameService> _logger;

    public GameService(
        IGameRoomRepository gameRepository,
        IMatchmakingRepository matchmakingRepository,
        IQuestionQueryRepository questionRepository,
        IUserCommandRepository userRepository,
        IUnitOfWork unitOfWork,
        IGameTimerService timerService,
        IGameHubService hubService,
        ILogger<GameService> logger)
    {
        _gameRepository = gameRepository;
        _matchmakingRepository = matchmakingRepository;
        _questionRepository = questionRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _timerService = timerService;
        _hubService = hubService;
        _logger = logger;
    }

    public async Task<Result<GameRoomDto>> CreateRoomAsync(GameType gameType, string languageCode, int totalRounds = 10, CancellationToken ct = default)
    {
        return await _gameRepository.CreateRoomAsync(gameType, languageCode, totalRounds, ct);
    }

    public async Task<Result<GamePlayerDto>> JoinRoomAsync(Guid roomId, int userId, string displayName, string? photoUrl, CancellationToken ct = default)
    {
        return await _gameRepository.JoinRoomAsync(GameRoomId.Create(roomId), userId, displayName, photoUrl, ct);
    }

    public async Task<Result> LeaveRoomAsync(Guid roomId, int userId, CancellationToken ct = default)
    {
        return await _gameRepository.LeaveRoomAsync(GameRoomId.Create(roomId), userId, ct);
    }

    public async Task<Result> SetPlayerReadyAsync(Guid roomId, int userId, bool isReady, CancellationToken ct = default)
    {
        return await _gameRepository.SetPlayerReadyAsync(GameRoomId.Create(roomId), userId, isReady, ct);
    }

    public async Task<Result<GameRoomDto>> GetRoomAsync(Guid roomId, CancellationToken ct = default)
    {
        var room = await _gameRepository.GetByIdAsync(GameRoomId.Create(roomId), ct);
        return room is null ? Result.Failure<GameRoomDto>(Error.GameNotFound) : Result.Success(room);
    }

    public async Task<Result<GameRoomDto?>> GetPlayerCurrentRoomAsync(int userId, CancellationToken ct = default)
    {
        var roomId = await _gameRepository.GetRoomIdByPlayerAsync(userId, ct);
        if (roomId is null) return Result.Success<GameRoomDto?>(null);
        var room = await _gameRepository.GetByIdAsync(roomId, ct);
        return Result.Success(room);
    }

    public async Task<Result<GameRoomDto>> StartGameAsync(Guid roomId, CancellationToken ct = default)
    {
        var room = await _gameRepository.GetByIdAsync(GameRoomId.Create(roomId), ct);
        if (room is null) return Result.Failure<GameRoomDto>(Error.GameNotFound);

        var questions = await _questionRepository.GetRandomQuestionsAsync(room.LanguageCode, room.TotalRounds, ct);
        if (questions.Count < room.TotalRounds)
            return Result.Failure<GameRoomDto>(Error.NotEnoughQuestions);

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
            await _timerService.ScheduleFirstRoundAsync(roomId, ct);

        return result;
    }

    public async Task<Result<RoundStartedResult>> StartRoundAsync(Guid roomId, CancellationToken ct = default)
    {
        var result = await _gameRepository.StartNextRoundAsync(GameRoomId.Create(roomId), ct);
        if (result.IsFailure) return Result.Failure<RoundStartedResult>(result.Error);

        var room = await _gameRepository.GetByIdAsync(GameRoomId.Create(roomId), ct);

        return Result.Success(new RoundStartedResult(result.Value, room!.CurrentRound, room.TotalRounds, room.RoundEndsAt ?? 0));
    }

    public async Task<Result<SubmitAnswerResult>> SubmitAnswerAsync(Guid roomId, int userId, string answer, CancellationToken ct = default)
    {
        var result = await _gameRepository.SubmitAnswerAsync(GameRoomId.Create(roomId), userId, answer, ct);
        if (result.IsSuccess && result.Value.AllPlayersAnswered)
        {
            await _timerService.ForceEndRoundAsync(roomId, ct);
        }
        return result;
    }

    public async Task<Result<RoundResultDto>> EndRoundAsync(Guid roomId, CancellationToken ct = default)
    {
        return await _gameRepository.EndRoundAsync(GameRoomId.Create(roomId), ct);
    }

    public async Task<Result<GameResultDto>> EndGameAsync(Guid roomId, CancellationToken ct = default)
    {
        var result = await _gameRepository.EndGameAsync(GameRoomId.Create(roomId), ct);
        if (result.IsFailure) return result;

        foreach (var standing in result.Value.FinalStandings)
        {
            var user = await _userRepository.GetByIdAsync(new UserId(standing.UserId), ct);
            if (user is null) continue;
            if (standing.Position == 1) { user.RecordWin(); user.AddCoins(result.Value.TotalRounds * 10); }
            else { user.RecordLoss(); }
            user.AddCoins(standing.TotalScore / 100);
        }
        await _unitOfWork.SaveChangesAsync(ct);

        return result;
    }
    public async Task<Result<MatchmakingOperationResult>> JoinMatchmakingAsync(
        int userId,
        string displayName,
        string? photoUrl,
        GameType gameType,
        string languageCode,
        CancellationToken ct = default)
    {
        var match = await _matchmakingRepository
            .JoinQueueAsync(userId, displayName, photoUrl, gameType, languageCode, ct);

        if (match.IsFailure)
        {
            return Result.Failure<MatchmakingOperationResult>(match.Error);
        }

        if (!match.Value.MatchFound || match.Value.Players is null)
        {

            return Result.Success(new MatchmakingOperationResult(false, null, null, 1));
        }

        var room = await _gameRepository.CreateRoomAsync(gameType, languageCode, 10, ct);
        if (room.IsFailure)
        {
            return Result.Failure<MatchmakingOperationResult>(room.Error);
        }
        var roomId = Guid.Parse(room.Value.Id);

        foreach (var p in match.Value.Players)
        {
            await _gameRepository.JoinRoomAsync(
                GameRoomId.Create(roomId),
                p.UserId,
                p.DisplayName,
                p.PhotoUrl,
                ct);
        }

        var playerIds = match.Value.Players.Select(p => p.UserId).ToList();
        await _hubService.AddUsersToRoomAsync(roomId, playerIds);

        await _hubService.NotifyMatchFoundAsync(
            match.Value.Players.Select(p => p.UserId).ToList(),
            new MatchFoundEvent(room.Value.Id, match.Value.Players),
            ct);


        _ = StartGameFlowAsync(roomId, languageCode, ct);

        return Result.Success(new MatchmakingOperationResult(true, room.Value.Id, match.Value.Players, 0));
    }

    private async Task StartGameFlowAsync(
    Guid roomId,
    string languageCode,
    CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), ct);

        var questions = await _questionRepository.GetRandomQuestionsAsync(languageCode, 10, ct);

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

        await _gameRepository.StartGameAsync(
            GameRoomId.Create(roomId),
            gameQuestions,
            ct);


        var room = await _gameRepository.GetByIdAsync(GameRoomId.Create(roomId), ct);
        if (room != null)
        {
            await _hubService.NotifyGameStartingAsync(room.Id, room, ct);
        }

        await Task.Delay(TimeSpan.FromSeconds(3), ct);
        await _timerService.ScheduleFirstRoundAsync(roomId, ct);
    }


    public async Task<Result> LeaveMatchmakingAsync(int userId, GameType gameType, string languageCode, CancellationToken ct = default)
    {
        return await _matchmakingRepository.LeaveQueueAsync(userId, gameType, languageCode, ct);
    }

    public async Task<Result> SetPlayerConnectedAsync(Guid roomId, int userId, bool isConnected, CancellationToken ct = default)
    {
        return await _gameRepository.SetPlayerConnectedAsync(GameRoomId.Create(roomId), userId, isConnected, ct);
    }

    public async Task<Guid?> GetPlayerRoomIdAsync(int userId, CancellationToken ct = default)
    {
        var roomId = await _gameRepository.GetRoomIdByPlayerAsync(userId, ct);
        return roomId?.Value;
    }
}
using MediatR;
using Microsoft.AspNetCore.SignalR;
using QuizBattle.Application.Features.Games;
using QuizBattle.Application.Features.Games.Commands;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Shared.Abstractions.RealTime;
using QuizBattle.Domain.Features.Games;

namespace QuizBattle.Infrastructure.Features.RealTime
{
    public sealed class GameHub : Hub<IGameHubClient>
    {
        private readonly ISender _sender;
        private readonly IGameRoomRepository _gameRepository;

        public GameHub(ISender sender, IGameRoomRepository gameRepository)
        {
            _sender = sender;
            _gameRepository = gameRepository;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

                var roomId = await _gameRepository.GetRoomIdByPlayerAsync(userId.Value);
                if (roomId is not null)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomId.Value}");
                    await _gameRepository.SetPlayerConnectedAsync(roomId, userId.Value, true);
                    await Clients.Group($"room:{roomId.Value}").PlayerReconnected(userId.Value);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                var roomId = await _gameRepository.GetRoomIdByPlayerAsync(userId.Value);
                if (roomId is not null)
                {
                    await _gameRepository.SetPlayerConnectedAsync(roomId, userId.Value, false);
                    await Clients.Group($"room:{roomId.Value}").PlayerDisconnected(userId.Value);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        #region Room Operations

        public async Task<GameRoomDto?> CreateRoom(int gameType, string languageCode, int totalRounds = 10)
        {
            var command = new CreateGameRoomCommand((GameType)gameType, languageCode, totalRounds);
            var result = await _sender.Send(command);

            if (result.IsFailure)
            {
                await Clients.Caller.Error(result.Error.Code, result.Error.Message);
                return null;
            }

            var room = result.Value;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{room.Id}");

            return room;
        }

        public async Task<GamePlayerDto?> JoinRoom(string roomId, int userId, string displayName, string? photoUrl)
        {
            var command = new JoinGameRoomCommand(Guid.Parse(roomId), userId, displayName, photoUrl);
            var result = await _sender.Send(command);

            if (result.IsFailure)
            {
                await Clients.Caller.Error(result.Error.Code, result.Error.Message);
                return null;
            }

            var player = result.Value;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

            await Clients.OthersInGroup($"room:{roomId}").PlayerJoined(player);

            return player;
        }

        public async Task LeaveRoom(string roomId, int userId)
        {
            var command = new LeaveGameRoomCommand(Guid.Parse(roomId), userId);
            var result = await _sender.Send(command);

            if (result.IsFailure)
            {
                await Clients.Caller.Error(result.Error.Code, result.Error.Message);
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room:{roomId}");
            await Clients.Group($"room:{roomId}").PlayerLeft(userId);
        }

        public async Task SetReady(string roomId, int userId, bool isReady)
        {
            var command = new SetPlayerReadyCommand(Guid.Parse(roomId), userId, isReady);
            var result = await _sender.Send(command);

            if (result.IsFailure)
            {
                await Clients.Caller.Error(result.Error.Code, result.Error.Message);
                return;
            }

            await Clients.Group($"room:{roomId}").PlayerReadyChanged(userId, isReady);
        }

        #endregion

        #region Game Flow

        public async Task<GameRoomDto?> StartGame(string roomId)
        {
            var command = new StartGameCommand(Guid.Parse(roomId));
            var result = await _sender.Send(command);

            if (result.IsFailure)
            {
                await Clients.Caller.Error(result.Error.Code, result.Error.Message);
                return null;
            }

            var room = result.Value;
            await Clients.Group($"room:{roomId}").GameStarting(room);

            return room;
        }

        public async Task<RoundStartedEvent?> StartRound(string roomId)
        {
            var command = new StartRoundCommand(Guid.Parse(roomId));
            var result = await _sender.Send(command);

            if (result.IsFailure)
            {
                await Clients.Caller.Error(result.Error.Code, result.Error.Message);
                return null;
            }

            var question = result.Value;
            var room = await _gameRepository.GetByIdAsync(GameRoomId.Create(Guid.Parse(roomId)));

            var roundEvent = new RoundStartedEvent(
                room!.CurrentRound,
                room.TotalRounds,
                new GameQuestionClientDto(
                    question.QuestionId,
                    question.RoundNumber,
                    question.Text,
                    question.OptionA,
                    question.OptionB,
                    question.OptionC),
                room.RoundEndsAt ?? 0);

            await Clients.Group($"room:{roomId}").RoundStarted(roundEvent);

            return roundEvent;
        }

        public async Task<SubmitAnswerResult?> SubmitAnswer(string roomId, int userId, string answer)
        {
            var command = new SubmitAnswerCommand(Guid.Parse(roomId), userId, answer);
            var result = await _sender.Send(command);

            if (result.IsFailure)
            {
                await Clients.Caller.Error(result.Error.Code, result.Error.Message);
                return null;
            }

            var answerResult = result.Value;

            await Clients.OthersInGroup($"room:{roomId}").PlayerAnswered(userId);

            return answerResult;
        }

        public async Task<RoundResultDto?> EndRound(string roomId)
        {
            var command = new EndRoundCommand(Guid.Parse(roomId));
            var result = await _sender.Send(command);

            if (result.IsFailure)
            {
                await Clients.Caller.Error(result.Error.Code, result.Error.Message);
                return null;
            }

            var roundResult = result.Value;
            await Clients.Group($"room:{roomId}").RoundEnded(roundResult);

            return roundResult;
        }

        public async Task<GameResultDto?> EndGame(string roomId)
        {
            var command = new EndGameCommand(Guid.Parse(roomId));
            var result = await _sender.Send(command);

            if (result.IsFailure)
            {
                await Clients.Caller.Error(result.Error.Code, result.Error.Message);
                return null;
            }

            var gameResult = result.Value;
            await Clients.Group($"room:{roomId}").GameEnded(gameResult);

            return gameResult;
        }

        #endregion

        #region Matchmaking

        public async Task<MatchmakingResult?> JoinMatchmaking(
            int userId,
            string displayName,
            string? photoUrl,
            int gameType,
            string languageCode)
        {
            var command = new JoinMatchmakingCommand(
                userId,
                displayName,
                photoUrl,
                (GameType)gameType,
                languageCode);

            var result = await _sender.Send(command);

            if (result.IsFailure)
            {
                await Clients.Caller.Error(result.Error.Code, result.Error.Message);
                return null;
            }

            var matchResult = result.Value;

            if (matchResult.MatchFound && matchResult.Players is not null)
            {
                var createCommand = new CreateGameRoomCommand((GameType)gameType, languageCode, 10);
                var roomResult = await _sender.Send(createCommand);

                if (roomResult.IsSuccess)
                {
                    var room = roomResult.Value;

                    foreach (var player in matchResult.Players)
                    {
                        var joinCommand = new JoinGameRoomCommand(
                            Guid.Parse(room.Id),
                            player.UserId,
                            player.DisplayName,
                            player.PhotoUrl);

                        await _sender.Send(joinCommand);
                    }

                    var matchEvent = new MatchFoundEvent(room.Id, matchResult.Players);
                    foreach (var player in matchResult.Players)
                    {
                        await Clients.Group($"user:{player.UserId}").MatchFound(matchEvent);
                    }

                    return new MatchmakingResult(true, room.Id, matchResult.Players);
                }
            }

            return matchResult;
        }

        public async Task LeaveMatchmaking(int userId, int gameType, string languageCode)
        {
            var command = new LeaveMatchmakingCommand(userId, (GameType)gameType, languageCode);
            await _sender.Send(command);
        }

        #endregion

        #region Helpers

        private int? GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? Context.User?.FindFirst("sub")?.Value;

            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        #endregion
    }
}

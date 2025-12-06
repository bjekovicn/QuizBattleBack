using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Games;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.RealTime;
using QuizBattle.Domain.Features.Games;
using QuizBattle.Domain.Features.Users;
using System.Security.Claims;

namespace QuizBattle.Infrastructure.Features.RealTime;

[Authorize]
public sealed class GameHub : Hub<IGameHubClient>
{
    private readonly IGameService _gameService;
    private readonly IUserQueryRepository _userRepository;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<GameHub> _logger;

    public GameHub(
        IGameService gameService,
        IUserQueryRepository userRepository,
        IConnectionManager connectionManager,
        ILogger<GameHub> logger)
    {
        _gameService = gameService;
        _userRepository = userRepository;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetRequiredUserId();
        _logger.LogInformation("[GameHub] User:{UserId} connected, ConnectionId:{ConnectionId}",
            userId, Context.ConnectionId);

        await _connectionManager.AddConnectionAsync(userId, Context.ConnectionId);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        _logger.LogDebug("[GameHub] Added user:{UserId} to user group", userId);

        var roomId = await _gameService.GetPlayerRoomIdAsync(userId);
        if (roomId.HasValue)
        {
            _logger.LogInformation("[GameHub] User:{UserId} reconnecting to room:{RoomId}",
                userId, roomId.Value);

            await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomId.Value}");
            await _gameService.SetPlayerConnectedAsync(roomId.Value, userId, true);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserIdOrDefault();

        if (userId.HasValue)
        {
            _logger.LogInformation("[GameHub] User:{UserId} disconnected, ConnectionId:{ConnectionId}",
                userId.Value, Context.ConnectionId);

            await _connectionManager.RemoveConnectionAsync(userId.Value, Context.ConnectionId);

            var isStillConnected = await _connectionManager.IsUserConnectedAsync(userId.Value);

            if (!isStillConnected)
            {
                var roomId = await _gameService.GetPlayerRoomIdAsync(userId.Value);
                if (roomId.HasValue)
                {
                    _logger.LogWarning("[GameHub] User:{UserId} fully disconnected from room:{RoomId}",
                        userId.Value, roomId.Value);
                    await _gameService.SetPlayerConnectedAsync(roomId.Value, userId.Value, false);
                }
            }
            else
            {
                _logger.LogDebug("[GameHub] User:{UserId} still has active connections", userId.Value);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinMatchmaking(int gameType, string languageCode)
    {
        var userId = GetRequiredUserId();
        _logger.LogInformation("[GameHub] User:{UserId} joining matchmaking, gameType:{GameType}, lang:{Lang}",
            userId, gameType, languageCode);

        var user = await GetUserInfoAsync(userId);

        var result = await _gameService.JoinMatchmakingAsync(
            userId,
            user.DisplayName,
            user.PhotoUrl,
            (GameType)gameType,
            languageCode);

        if (result.IsFailure)
        {
            _logger.LogWarning("[GameHub] Matchmaking failed for user:{UserId}, error:{Error}",
                userId, result.Error.Message);
            await SendErrorAsync(result.Error.Code, result.Error.Message);
        }
        else
        {
            _logger.LogInformation("[GameHub] User:{UserId} successfully joined matchmaking", userId);
        }
    }

    public async Task LeaveMatchmaking(int gameType, string languageCode)
    {
        var userId = GetRequiredUserId();
        _logger.LogInformation("[GameHub] User:{UserId} leaving matchmaking", userId);

        await _gameService.LeaveMatchmakingAsync(userId, (GameType)gameType, languageCode);
    }

    public async Task<GamePlayerDto?> JoinRoom(string roomId)
    {
        var userId = GetRequiredUserId();
        _logger.LogInformation("[GameHub] User:{UserId} joining room:{RoomId}", userId, roomId);

        var user = await GetUserInfoAsync(userId);

        var result = await _gameService.JoinRoomAsync(
            Guid.Parse(roomId),
            userId,
            user.DisplayName,
            user.PhotoUrl);

        if (result.IsFailure)
        {
            _logger.LogWarning("[GameHub] Join room failed for user:{UserId}, error:{Error}",
                userId, result.Error.Message);
            await SendErrorAsync(result.Error.Code, result.Error.Message);
            return null;
        }

        // (Ovo je backup ako AddUsersToRoomAsync nije bio pozvan iz matchmaking-a)
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomId}");
        _logger.LogInformation("[GameHub] User:{UserId} successfully joined room:{RoomId}", userId, roomId);

        return result.Value;
    }

    public async Task LeaveRoom(string roomId)
    {
        var userId = GetRequiredUserId();
        _logger.LogInformation("[GameHub] User:{UserId} leaving room:{RoomId}", userId, roomId);

        await _gameService.LeaveRoomAsync(Guid.Parse(roomId), userId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room:{roomId}");
        _logger.LogDebug("[GameHub] Removed user:{UserId} from room:{RoomId} group", userId, roomId);
    }

    public async Task SetReady(string roomId, bool isReady)
    {
        var userId = GetRequiredUserId();
        _logger.LogInformation("[GameHub] User:{UserId} setting ready:{IsReady} in room:{RoomId}",
            userId, isReady, roomId);

        var result = await _gameService.SetPlayerReadyAsync(Guid.Parse(roomId), userId, isReady);

        if (result.IsFailure)
        {
            _logger.LogWarning("[GameHub] Set ready failed for user:{UserId}, error:{Error}",
                userId, result.Error.Message);
            await SendErrorAsync(result.Error.Code, result.Error.Message);
        }
    }

    public async Task SubmitAnswer(string roomId, string answer)
    {
        var userId = GetRequiredUserId();
        _logger.LogInformation("[GameHub] User:{UserId} submitting answer in room:{RoomId}",
            userId, roomId);

        var result = await _gameService.SubmitAnswerAsync(Guid.Parse(roomId), userId, answer);

        if (result.IsFailure)
        {
            _logger.LogWarning("[GameHub] Submit answer failed for user:{UserId}, error:{Error}",
                userId, result.Error.Message);
            await SendErrorAsync(result.Error.Code, result.Error.Message);
        }
    }

    private int GetRequiredUserId() => GetUserIdOrDefault()
        ?? throw new HubException("User not authenticated");

    private int? GetUserIdOrDefault()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private async Task<UserInfo> GetUserInfoAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(new UserId(userId))
            ?? throw new HubException($"User {userId} not found");

        return new UserInfo(
            user.Id,
            string.IsNullOrWhiteSpace(user.FullName) ? $"Player {user.Id}" : user.FullName,
            user.Photo);
    }

    private Task SendErrorAsync(string code, string message)
    {
        _logger.LogDebug("[GameHub] Sending error to caller: {Code} - {Message}", code, message);
        return Clients.Caller.Error(code, message);
    }

    private sealed record UserInfo(int UserId, string DisplayName, string? PhotoUrl);

}

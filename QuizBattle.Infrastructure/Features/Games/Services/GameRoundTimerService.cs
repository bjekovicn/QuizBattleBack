using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Games.Services;
using QuizBattle.Application.Shared.Abstractions.RealTime;
using QuizBattle.Domain.Features.Games;

namespace QuizBattle.Infrastructure.Features.Games.Services;

internal sealed class GameRoundTimerService : BackgroundService, IGameTimerService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GameRoundTimerService> _logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeTimers = new();

    public GameRoundTimerService(IServiceScopeFactory scopeFactory, ILogger<GameRoundTimerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task ScheduleFirstRoundAsync(Guid roomId, CancellationToken ct = default)
    {
        return ScheduleAsync(
            $"first:{roomId}",
            roomId,
            TimeSpan.FromSeconds(GameTimingConfig.DelayBeforeFirstRoundSeconds),
            StartFirstRoundAsync,
            ct);
    }

    public Task ScheduleRoundEndAsync(Guid roomId, TimeSpan delay, CancellationToken ct = default)
    {
        return ScheduleAsync($"round:{roomId}", roomId, delay, EndRoundAsync, ct);
    }

    public Task CancelRoundTimerAsync(Guid roomId, CancellationToken ct = default)
    {
        CancelTimer($"round:{roomId}");
        return Task.CompletedTask;
    }

    private Task ScheduleAsync(
        string key,
        Guid roomId,
        TimeSpan delay,
        Func<IServiceScope, Guid, Task> action,
        CancellationToken ct)
    {
        CancelTimer(key);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _activeTimers[key] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogDebug("[GameRoundTimer] Scheduled {Key} for room:{RoomId}, delay:{Delay}s",
                    key, roomId, delay.TotalSeconds);

                await Task.Delay(delay, cts.Token);

                if (!cts.Token.IsCancellationRequested)
                {
                    _logger.LogDebug("[GameRoundTimer] Executing {Key} for room:{RoomId}", key, roomId);
                    using var scope = _scopeFactory.CreateScope();
                    await action(scope, roomId);
                }
                else
                {
                    _logger.LogDebug("[GameRoundTimer] Cancelled {Key} for room:{RoomId}", key, roomId);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("[GameRoundTimer] Cancelled {Key} for room:{RoomId}", key, roomId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GameRoundTimer] Error executing {Key} for room:{RoomId}", key, roomId);
            }
            finally
            {
                _activeTimers.TryRemove(key, out _);
            }
        }, ct);

        return Task.CompletedTask;
    }

    private void CancelTimer(string key)
    {
        if (_activeTimers.TryRemove(key, out var cts))
        {
            try
            {
                _logger.LogDebug("[GameRoundTimer] Cancelling timer: {Key}", key);
                cts.Cancel();
            }
            finally
            {
                cts.Dispose();
            }
        }
    }

    private async Task StartFirstRoundAsync(IServiceScope scope, Guid roomId)
    {
        var gameService = scope.ServiceProvider.GetRequiredService<IGameOrchestrator>();
        var hubService = scope.ServiceProvider.GetRequiredService<IGameNotificationsService>();

        _logger.LogInformation("[GameRoundTimer] Starting first round for room:{RoomId}", roomId);

        var result = await gameService.StartRoundAsync(roomId);
        if (result.IsSuccess)
        {
            var r = result.Value;
            await hubService.NotifyRoundStartedAsync(
                roomId.ToString(),
                new RoundStartedEvent(
                    r.CurrentRound,
                    r.TotalRounds,
                    new GameQuestionClientDto(
                        r.Question.QuestionId,
                        r.Question.RoundNumber,
                        r.Question.Text,
                        r.Question.OptionA,
                        r.Question.OptionB,
                        r.Question.OptionC),
                    r.RoundEndsAt));

            // Schedule round end - THIS IS THE ONLY PLACE WHERE TIMER IS SCHEDULED
            await ScheduleRoundEndAsync(
                roomId,
                TimeSpan.FromSeconds(GameTimingConfig.RoundDurationSeconds),
                CancellationToken.None);
        }
        else
        {
            _logger.LogWarning("[GameRoundTimer] Failed to start first round for room:{RoomId}, error:{Error}",
                roomId, result.Error.Message);
            await hubService.NotifyRoomErrorAsync(roomId.ToString(), result.Error.Code, result.Error.Message);
        }
    }

    private async Task StartNextRoundAsync(IServiceScope scope, Guid roomId)
    {
        var gameService = scope.ServiceProvider.GetRequiredService<IGameOrchestrator>();
        var hubService = scope.ServiceProvider.GetRequiredService<IGameNotificationsService>();

        _logger.LogInformation("[GameRoundTimer] Starting next round for room:{RoomId}", roomId);

        var result = await gameService.StartRoundAsync(roomId);
        if (result.IsSuccess)
        {
            await hubService.NotifyRoundStartedAsync(
                roomId.ToString(),
                new RoundStartedEvent(
                    result.Value.CurrentRound,
                    result.Value.TotalRounds,
                    new GameQuestionClientDto(
                        result.Value.Question.QuestionId,
                        result.Value.Question.RoundNumber,
                        result.Value.Question.Text,
                        result.Value.Question.OptionA,
                        result.Value.Question.OptionB,
                        result.Value.Question.OptionC),
                    result.Value.RoundEndsAt));

            // Schedule round end - THIS IS THE ONLY PLACE WHERE TIMER IS SCHEDULED
            await ScheduleRoundEndAsync(
                roomId,
                TimeSpan.FromSeconds(GameTimingConfig.RoundDurationSeconds),
                CancellationToken.None);
        }
        else
        {
            _logger.LogWarning("[GameRoundTimer] Failed to start next round for room:{RoomId}, error:{Error}",
                roomId, result.Error.Message);
            await hubService.NotifyRoomErrorAsync(roomId.ToString(), result.Error.Code, result.Error.Message);
        }
    }

    private async Task EndRoundAsync(IServiceScope scope, Guid roomId)
    {
        var gameService = scope.ServiceProvider.GetRequiredService<IGameOrchestrator>();
        var hubService = scope.ServiceProvider.GetRequiredService<IGameNotificationsService>();

        _logger.LogInformation("[GameRoundTimer] Ending round for room:{RoomId}", roomId);

        var result = await gameService.EndRoundAsync(roomId);
        if (result.IsSuccess)
        {
            await hubService.NotifyRoundEndedAsync(roomId.ToString(), result.Value);

            // Get room to check if more rounds remain
            var roomResult = await gameService.GetRoomAsync(roomId);
            if (roomResult.IsSuccess)
            {
                var room = roomResult.Value;

                if (room.CurrentRound >= room.TotalRounds)
                {
                    // All rounds completed - end game after short delay
                    _logger.LogInformation("[GameRoundTimer] All rounds completed for room:{RoomId}, ending game", roomId);

                    await Task.Delay(TimeSpan.FromSeconds(GameTimingConfig.DelayBeforeGameEndSeconds));

                    var endResult = await gameService.EndGameAsync(roomId);
                    if (endResult.IsSuccess)
                    {
                        await hubService.NotifyGameEndedAsync(roomId.ToString(), endResult.Value);
                    }
                    else
                    {
                        _logger.LogWarning("[GameRoundTimer] Failed to end game for room:{RoomId}, error:{Error}",
                            roomId, endResult.Error.Message);
                    }
                }
                else
                {
                    // Schedule next round after delay
                    _logger.LogInformation("[GameRoundTimer] Scheduling next round for room:{RoomId}", roomId);

                    await ScheduleAsync(
                        $"nextround:{roomId}",
                        roomId,
                        TimeSpan.FromSeconds(GameTimingConfig.DelayBetweenRoundsSeconds),
                        StartNextRoundAsync,
                        CancellationToken.None);
                }
            }
        }
        else
        {
            _logger.LogWarning("[GameRoundTimer] Failed to end round for room:{RoomId}, error:{Error}",
                roomId, result.Error.Message);
            await hubService.NotifyRoomErrorAsync(roomId.ToString(), result.Error.Code, result.Error.Message);
        }
    }

    public async Task ForceEndRoundAsync(Guid roomId, CancellationToken ct = default)
    {
        _logger.LogInformation("[GameRoundTimer] Force ending round for room:{RoomId}", roomId);

        CancelTimer($"round:{roomId}");
        using var scope = _scopeFactory.CreateScope();
        await EndRoundAsync(scope, roomId);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[GameRoundTimer] Stopping service, cancelling {Count} active timers", _activeTimers.Count);

        foreach (var (key, cts) in _activeTimers)
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GameRoundTimer] Error cancelling timer: {Key}", key);
            }
        }

        _activeTimers.Clear();
        await base.StopAsync(cancellationToken);
    }
}
using System.Collections.Concurrent;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Games.Commands;
using QuizBattle.Application.Shared.Abstractions.RealTime;
using QuizBattle.Application.Shared.Abstractions.Services;

namespace QuizBattle.Infrastructure.Features.Games.Services
{

    internal sealed class GameRoundTimerService : BackgroundService, IGameTimerService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GameRoundTimerService> _logger;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeTimers = new();

        public GameRoundTimerService(
            IServiceScopeFactory scopeFactory,
            ILogger<GameRoundTimerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task ScheduleRoundEndAsync(Guid roomId, TimeSpan delay, CancellationToken ct = default)
        {
            // Cancel existing timer if any
            CancelRoundTimerAsync(roomId, ct).Wait(ct);

            var timerCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _activeTimers[roomId] = timerCts;

            _logger.LogInformation("Scheduled round end for room {RoomId} in {Delay}", roomId, delay);

            // Fire and forget - the timer will handle completion
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay, timerCts.Token);

                    if (!timerCts.Token.IsCancellationRequested)
                    {
                        await EndRoundAsync(roomId);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Round timer cancelled for room {RoomId}", roomId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in round timer for room {RoomId}", roomId);
                }
                finally
                {
                    _activeTimers.TryRemove(roomId, out _);
                }
            }, ct);

            return Task.CompletedTask;
        }

        public Task CancelRoundTimerAsync(Guid roomId, CancellationToken ct = default)
        {
            if (_activeTimers.TryRemove(roomId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _logger.LogDebug("Cancelled round timer for room {RoomId}", roomId);
            }

            return Task.CompletedTask;
        }

        private async Task EndRoundAsync(Guid roomId)
        {
            using var scope = _scopeFactory.CreateScope();

            try
            {
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var hubService = scope.ServiceProvider.GetRequiredService<IGameHubService>();

                // End the round
                var endRoundCommand = new EndRoundCommand(roomId);
                var roundResult = await sender.Send(endRoundCommand);

                if (roundResult.IsSuccess)
                {
                    await hubService.NotifyRoundEndedAsync(roomId.ToString(), roundResult.Value);
                    _logger.LogInformation("Round ended automatically for room {RoomId}", roomId);

                    // Check if game should end
                    // This would require getting room info and checking currentRound vs totalRounds
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to end round for room {RoomId}: {Error}",
                        roomId,
                        roundResult.Error.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending round for room {RoomId}", roomId);
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // This service doesn't need continuous execution
            // It manages timers on-demand via ScheduleRoundEndAsync
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Cancel all active timers
            foreach (var (roomId, cts) in _activeTimers)
            {
                cts.Cancel();
                cts.Dispose();
                _logger.LogDebug("Cancelled timer for room {RoomId} during shutdown", roomId);
            }

            _activeTimers.Clear();

            await base.StopAsync(cancellationToken);
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Auth;

namespace QuizBattle.Infrastructure.Features.Auth.Services
{
    internal sealed class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RefreshTokenCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24);

        public RefreshTokenCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<RefreshTokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RefreshTokenCleanupService started");

            // Čekaj 1 minut nakon starta prije prvog cleanup-a
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredTokensAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during refresh token cleanup");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }

        private async Task CleanupExpiredTokensAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

            _logger.LogInformation("Starting expired refresh tokens cleanup...");

            await repository.DeleteExpiredAsync(ct);

            _logger.LogInformation("Expired refresh tokens cleanup completed");
        }
    }
}
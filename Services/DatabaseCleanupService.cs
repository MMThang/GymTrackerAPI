using GymTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Services
{
    public class DatabaseCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DatabaseCleanupService> _logger;

        public DatabaseCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<DatabaseCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var db = scope.ServiceProvider
                        .GetRequiredService<AppDBContext>();

                    await CleanupOAuthLoginCodesAsync(
                        db,
                        stoppingToken);

                    await CleanupRefreshTokensAsync(
                        db,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error occurred during database cleanup.");
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(10),
                    stoppingToken);
            }
        }

        private static async Task CleanupOAuthLoginCodesAsync(
            AppDBContext db,
            CancellationToken cancellationToken)
        {
            await db.OAuthLoginCodes
                .Where(x =>
                    x.ExpiresAt <= DateTime.UtcNow ||
                    x.UsedAt != null)
                .ExecuteDeleteAsync(cancellationToken);
        }

        private static async Task CleanupRefreshTokensAsync(
            AppDBContext db,
            CancellationToken cancellationToken)
        {
            await db.RefreshTokens
                .Where(x => x.ExpiryDate <= DateTime.UtcNow)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
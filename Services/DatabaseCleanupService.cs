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

                    var now = DateTime.UtcNow;

                    await CleanupOAuthLoginCodesAsync(
                        db,
                        now,
                        stoppingToken);

                    await CleanupRefreshTokensAsync(
                        db,
                        now,
                        stoppingToken);

                    await CleanupEmailVerificationCodesAsync(
                        db,
                        now,
                        stoppingToken);

                    await CleanupUnverifiedUsersAsync(
                        db,
                        now,
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
            DateTime now,
            CancellationToken cancellationToken)
        {
            await db.OAuthLoginCodes
                .Where(x =>
                    x.ExpiresAt <= now ||
                    x.UsedAt != null)
                .ExecuteDeleteAsync(cancellationToken);
        }

        private static async Task CleanupRefreshTokensAsync(
            AppDBContext db,
            DateTime now,
            CancellationToken cancellationToken)
        {
            await db.RefreshTokens
                .Where(x => x.ExpiryDate <= now)
                .ExecuteDeleteAsync(cancellationToken);
        }

        private static async Task CleanupEmailVerificationCodesAsync(
            AppDBContext db,
            DateTime now,
            CancellationToken cancellationToken)
        {
            await db.EmailVerificationCodes
                .Where(x =>
                    x.ExpiresAt <= now ||
                    x.UsedAt != null)
                .ExecuteDeleteAsync(cancellationToken);
        }

        private static async Task CleanupUnverifiedUsersAsync(
            AppDBContext db,
            DateTime now,
            CancellationToken cancellationToken)
        {
            DateOnly cutoff = DateOnly.FromDateTime(now.AddDays(1));

            await db.Users
                .Where(x =>
                    !x.EmailVerified &&
                    x.RegisterDate <= cutoff)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
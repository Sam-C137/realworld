using Microsoft.EntityFrameworkCore;
using RealWorldApi.Infrastructure.Data;

namespace RealWorldApi.Core.Workers;

public class SessionCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<SessionCleanupService> logger,
    TimeProvider timeProvider): BackgroundService
{
    
    protected override async Task ExecuteAsync(
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Calculate time until next midnight UTC
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var nextMidnight = now.Date.AddDays(1);
            var delay = nextMidnight - now;

            // Wait until midnight (or until cancellation)
            await Task.Delay(delay, timeProvider, ct);

            if (ct.IsCancellationRequested)
                break;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-7);

                var deletedCount = await context.UserSessions
                    .Where(s => s.ExpiresAt < cutoff || (s.IsRevoked && s.RevokedAt < cutoff))
                    .ExecuteDeleteAsync(ct);

                logger.LogInformation("Session cleanup completed. Deleted {Count} sessions.", deletedCount);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during session cleanup");
            }
        }
    }
}

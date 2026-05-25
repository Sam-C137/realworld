namespace RealWorldApi.Core.Workers;

public class SessionCleanupWorker(IServiceScopeFactory scopeFactory, ILogger<SessionCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Calculate time until next midnight UTC
            var now = DateTime.UtcNow;
            var nextMidnight = now.Date.AddDays(1);
            var delay = nextMidnight - now;

            // Wait until midnight (or until cancellation)
            await Task.Delay(delay, ct);

            if (ct.IsCancellationRequested)
                break;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var cleanup = scope.ServiceProvider.GetRequiredService<SessionCleanupService>();

                var deletedCount = await cleanup.DeleteStaleSessionsAsync(DateTime.UtcNow, ct);

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

using Microsoft.EntityFrameworkCore;
using RealWorldApi.Infrastructure.Data;

namespace RealWorldApi.Core.Workers;

public class SessionCleanupService(AppDbContext db)
{
    public async Task<int> DeleteStaleSessionsAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var cutoff = utcNow.AddDays(-7);

        var staleSessions = await db.UserSessions
            .Where(s => s.ExpiresAt < cutoff || (s.IsRevoked && s.RevokedAt < cutoff))
            .ToListAsync(ct);

        db.UserSessions.RemoveRange(staleSessions);
        return await db.SaveChangesAsync(ct);
    }
}

using Microsoft.EntityFrameworkCore;
using RealWorldApi.Core.Features.Articles.Services;
using RealWorldApi.Core.Features.Profiles.Dto;
using RealWorldApi.Infrastructure.Data;
using ErrorOr;
using Mapster;

namespace RealWorldApi.Core.Features.Profiles.Services;

public class ProfileService(AppDbContext db, ArticleCacheService articleCache, ILogger<Program> logger)
    : IProfileService
{
    public async Task<ErrorOr<GetProfileResponseDto>> GetProfile(string username, Guid? userId = null)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Username == username);
        if (profile is null) return Error.NotFound($"Profile with username {username} not found");
        var following = userId.HasValue && userId.Value != Guid.Empty && await db.Follows
            .Include(f => f.Follower)
            .AnyAsync(f => f.Follower.UserId == userId && f.FolloweeId == profile.Id);
       
        return profile.BuildAdapter()
            .AddParameters("following", following)
            .AdaptToType<GetProfileResponseDto>();
    }

    public async Task<ErrorOr<GetProfileResponseDto>> FollowProfile(Guid userId, string username)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Username == username);
        if (profile is null) return Error.NotFound($"Profile with username {username} not found");
        if (profile.UserId == userId) return Error.Failure("Users cannot follow themselves");
        
        await db.Database.ExecuteSqlInterpolatedAsync($"""
               INSERT INTO follows (id, follower_id, followee_id, created_at, updated_at)
               SELECT {Guid.CreateVersion7()}, id, {profile.Id}, NOW(), NOW()
               FROM profiles
               WHERE user_id = {userId}
               ON CONFLICT (follower_id, followee_id) DO NOTHING
               """);
        await articleCache.BumpVersionAsync();
        
        return profile.BuildAdapter()
            .AddParameters("following", true)
            .AdaptToType<GetProfileResponseDto>();
    }

    public async Task<ErrorOr<GetProfileResponseDto>> UnfollowProfile(Guid userId, string username)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Username == username);
        if (profile is null) return Error.NotFound($"Profile with username {username} not found");
        if (profile.UserId == userId) return Error.Failure("Users cannot unfollow themselves");
        
        await db.Follows.Where(f => f.Follower.UserId == userId && f.FolloweeId == profile.Id)
            .ExecuteDeleteAsync();
        await articleCache.BumpVersionAsync();
        
        return profile.BuildAdapter()
            .AddParameters("following", false)
            .AdaptToType<GetProfileResponseDto>();
    }
}

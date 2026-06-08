using System.Text.Json;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Articles.Dto;
using StackExchange.Redis;

namespace RealWorldApi.Core.Features.Articles.Services;

public class ArticleCacheService(IDatabase redis): ICacheService
{
    private static readonly TimeSpan ItemTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ListTtl = TimeSpan.FromMinutes(2);

    public async Task<PaginatedResponse<GetArticleResponseDto>?> GetArticlesFromCache(string fingerprint)
    {
        var version = await GetVersionAsync();
        var key = ArticlesCacheKeys.List(version, fingerprint);
        var raw = await redis.StringGetAsync(key);
        if (!raw.HasValue) return null;
        return JsonSerializer.Deserialize<PaginatedResponse<GetArticleResponseDto>>((ReadOnlySpan<byte>)raw);
    }
    
    public async Task SetArticlesToCache(string fingerprint, PaginatedResponse<GetArticleResponseDto> payload)
    {
        var version = await GetVersionAsync();
        var key = ArticlesCacheKeys.List(version, fingerprint);
        await redis.StringSetAsync(key, JsonSerializer.Serialize(payload), ListTtl);
    }
    
    public async Task<GetArticleResponseDto?> GetArticleFromCache(string slug, string? loggedInUserId = null)
    {
        var version = await GetVersionAsync();
        var key = ArticlesCacheKeys.Item(slug + loggedInUserId, version);
        var raw = await redis.StringGetAsync(key);
        if (!raw.HasValue) return null;
        return JsonSerializer.Deserialize<GetArticleResponseDto>((ReadOnlySpan<byte>)raw);
    }
    
    public async Task SetArticleToCache(string slug, GetArticleResponseDto payload, string? loggedInUserId = null)
    {
        var version = await GetVersionAsync();
        var key = ArticlesCacheKeys.Item(slug + loggedInUserId, version);
        await redis.StringSetAsync(key, JsonSerializer.Serialize(payload), ItemTtl);
        
        await redis.SetAddAsync(ArticlesCacheKeys.Tag(slug + loggedInUserId), key);
        await redis.KeyExpireAsync(ArticlesCacheKeys.Tag(slug + loggedInUserId), ItemTtl);
    }
    
    public async Task InvalidateArticleCache(string slug, string? loggedInUserId = null)
    {
        var key = ArticlesCacheKeys.Tag(slug + loggedInUserId);
        var members = await redis.SetMembersAsync(key);
        if (members.Length > 0)
        {
            var keys = members
                .Where(m => m.HasValue)
                .Select(m => (RedisKey)m.ToString())
                .ToArray();
            if (keys.Length > 0)
            {
                await redis.KeyDeleteAsync(keys);
            }
        }
        await redis.KeyDeleteAsync(key);
    }

    public async Task<long> GetVersionAsync()
    {
        var raw = await redis.StringGetAsync(ArticlesCacheKeys.Version());
        if (raw.HasValue && long.TryParse((ReadOnlySpan<byte>)raw, out var version)) return version;
       
        await redis.StringSetAsync(ArticlesCacheKeys.Version(), ArticlesCacheKeys.DefaultVersion.ToString());
        return ArticlesCacheKeys.DefaultVersion;
    }

    public async Task<long> BumpVersionAsync()
    {
        return await redis.StringIncrementAsync(ArticlesCacheKeys.Version());
    }
}
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Infrastructure.Data;
using StackExchange.Redis;

namespace RealWorldApi.Core.Features.Articles.Services;

public class ArticleCacheService(IDatabase redis): ICacheService
{
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
using System.Text.Json;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Infrastructure.Data.Models;
using StackExchange.Redis;

namespace RealWorldApi.Core.Features.Tags.Services;

public class TagsCacheService(IDatabase redis): ICacheService
{
    private static readonly TimeSpan ItemTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ListTtl = TimeSpan.FromMinutes(2);
    
    public async Task<PaginatedResponse<string>?> GetTagsFromCache(string fingerprint)
    {
        var version = await GetVersionAsync();
        var key = TagsCacheKeys.List(version, fingerprint);
        var raw = await redis.StringGetAsync(key);
        if (!raw.HasValue) return null;
        return JsonSerializer.Deserialize<PaginatedResponse<string>>((ReadOnlySpan<byte>)raw);
    }

    public async Task SetTagsToCache(string fingerprint, PaginatedResponse<string> payload)
    {
        var version = await GetVersionAsync();
        var key = TagsCacheKeys.List(version, fingerprint);
        await redis.StringSetAsync(key, JsonSerializer.Serialize(payload), ListTtl);
    }
    
    public async Task<Tag?> GetTagFromCache(Guid id)
    {
        var version = await GetVersionAsync();
        var key = TagsCacheKeys.Item(id, version);
        var raw = await redis.StringGetAsync(key);
        if (!raw.HasValue) return null;
        return JsonSerializer.Deserialize<Tag>((ReadOnlySpan<byte>)raw);
    }

    public async Task SetTagToCache(Tag tag)
    {
        var version = await GetVersionAsync();
        var key = TagsCacheKeys.Item(tag.Id, version);
        await redis.StringSetAsync(key, JsonSerializer.Serialize(tag), ItemTtl);
        
        await redis.SetAddAsync(TagsCacheKeys.Tag(tag.Id), key);
        await redis.KeyExpireAsync(TagsCacheKeys.Tag(tag.Id), ItemTtl);
    }

    public async Task InvalidateTagCache(Guid id)
    {
        var key = TagsCacheKeys.Tag(id);
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
       var raw = await redis.StringGetAsync(TagsCacheKeys.Version());
       if (raw.HasValue && long.TryParse((ReadOnlySpan<byte>)raw, out var version)) return version;
       
       await redis.StringSetAsync(TagsCacheKeys.Version(), TagsCacheKeys.DefaultVersion.ToString());
       return TagsCacheKeys.DefaultVersion;
    }

    public async Task<long> BumpVersionAsync()
    {
        return await redis.StringIncrementAsync(TagsCacheKeys.Version());
    }
}
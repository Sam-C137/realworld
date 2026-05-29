namespace RealWorldApi.Core.Abstractions;

public interface ICacheKeyResource
{
    static abstract string Resource { get; }
}

public interface ICacheKeyVersion
{
    static abstract long DefaultVersion { get; }
}

public abstract class CacheKeysBase<TSelf> : CacheKeysBase<TSelf, Guid>
    where TSelf : CacheKeysBase<TSelf>, ICacheKeyResource
{
}

public abstract class CacheKeysBase<TSelf, TId> : ICacheKeys<TId>
    where TSelf : CacheKeysBase<TSelf, TId>, ICacheKeyResource
    where TId : notnull
{
    private static string Prefix => Constants.AppCachePrefix;
    private static string Resource => TSelf.Resource;

    public static string Version() => $"{Prefix}{Resource}:version";

    // The version is part of the data key, so a version bump invalidates all caches.
    public static string Item(TId id, long version) => $"{Prefix}{Resource}:{version}:{id}";

    // Tag sets allow targeted invalidation (single resource -> remove all related keys).
    public static string Tag(TId id) => $"{Prefix}tag:{Resource}:{id}";

    // List/query cache keys include a normalized query fingerprint.
    public static string List(long version, string fingerprint) => $"{Prefix}{Resource}:list:{version}:{fingerprint}";
}

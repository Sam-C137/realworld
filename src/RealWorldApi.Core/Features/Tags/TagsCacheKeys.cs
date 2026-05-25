using RealWorldApi.Core.Abstractions;

namespace RealWorldApi.Core.Features.Tags;

public abstract class TagsCacheKeys : CacheKeysBase<TagsCacheKeys>, ICacheKeyResource, ICacheKeyVersion
{
    public static string Resource => "tag";

    public static long DefaultVersion => 1;
}

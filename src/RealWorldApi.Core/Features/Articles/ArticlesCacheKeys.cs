using RealWorldApi.Core.Abstractions;

namespace RealWorldApi.Core.Features.Articles;

public class ArticlesCacheKeys : CacheKeysBase<ArticlesCacheKeys>, ICacheKeyResource, ICacheKeyVersion
{
    public static string Resource => "article";

    public static long DefaultVersion => 1;
}
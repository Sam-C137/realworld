using FluentValidation;
using RealWorldApi.Core.Abstractions;

namespace RealWorldApi.Core.Features.Articles.Dto;

public class GetArticlesRequestDto: PaginatedSortableRequest
{
    public ArticlesSortField Sort { get; set; } = ArticlesSortField.CreatedAt;
    public string? Tag { get; set; }
    public string? Favorited { get; set; }
    public string? Author { get; set; }
}

public enum ArticlesSortField
{
    CreatedAt,
    Title,
}

public class GetArticlesRequestValidator : AbstractValidator<GetArticlesRequestDto>
{
    public GetArticlesRequestValidator()
    {
        Include(new PaginatedRequestValidator());
        Include(new SortableRequestValidator<GetArticlesRequestDto, ArticlesSortField>(x => x.Sort));
    }
}

public static class GetArticlesRequestExtensions
{
    public static string GetCacheFingerPrint(this GetArticlesRequestDto request, string? userId = null, bool? feed = null)
    {
        var uid = userId is not null ? $"u={userId}&" : string.Empty;
        var fid = feed is not null ? $"f={feed}&" : string.Empty;
        return $"${uid}${fid}p={request.Page}&l={request.Limit}&s={request.Sort}&o={request.Order}&t={request.Tag}&f={request.Favorited}&a={request.Author}";
    }
}
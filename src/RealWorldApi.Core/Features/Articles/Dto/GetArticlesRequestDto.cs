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
    public static string GetCacheFingerPrint(this GetArticlesRequestDto request, string? feedId = null)
    {
        var fid = feedId is not null ? $"fe={feedId}&" : string.Empty;
        return $"${fid}p={request.Page}&l={request.Limit}&s={request.Sort}&o={request.Order}&t={request.Tag}&f={request.Favorited}&a={request.Author}";
    }
}
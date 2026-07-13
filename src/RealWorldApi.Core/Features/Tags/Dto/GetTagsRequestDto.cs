using FluentValidation;
using RealWorldApi.Core.Abstractions;

namespace RealWorldApi.Core.Features.Tags.Dto;

public class GetTagsRequestDto: PaginatedSortableRequest
{
    public TagsSortField Sort { get; set; } = TagsSortField.CreatedAt;
}

public enum TagsSortField
{
    CreatedAt,
    Name,
    Popularity
}

public class GetTagsRequestValidator : AbstractValidator<GetTagsRequestDto>
{
    public GetTagsRequestValidator()
    {
        Include(new PaginatedRequestValidator());
        Include(new SortableRequestValidator<GetTagsRequestDto, TagsSortField>(x => x.Sort));
    }
}

public static class GetTagsRequestExtensions
{
    public static string GetCacheFingerPrint(this GetTagsRequestDto request)
    {
        return $"p={request.Page}&l={request.Limit}&s={request.Sort}&o={request.Order}";
    }
}
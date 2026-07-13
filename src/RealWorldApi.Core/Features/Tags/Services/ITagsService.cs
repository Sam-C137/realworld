using ErrorOr;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Tags.Dto;

namespace RealWorldApi.Core.Features.Tags.Services;

public interface ITagsService
{
    Task<ErrorOr<GetTagResponseDto>> CreateTag(CreateTagRequestDto request);
    Task<ErrorOr<GetTagResponseDto>> GetTag(Guid id);
    Task<ErrorOr<GetTagResponseDto>> GetTag(string name);
    Task<ErrorOr<PaginatedResponse<string>>> GetTags(GetTagsRequestDto request);
    Task<ErrorOr<GetTagResponseDto>> DeleteTag(string name);
}
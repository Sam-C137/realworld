using ErrorOr;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Tags.Dto;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Tags.Services;

public interface ITagsService
{
    Task<ErrorOr<Tag>> CreateTag(CreateTagRequestDto request);
    Task<ErrorOr<Tag>> GetTag(Guid id);
    Task<ErrorOr<Tag>> GetTag(string name);
    Task<ErrorOr<PaginatedResponse<string>>> GetTags(GetTagsRequestDto request);
    Task<ErrorOr<Tag>> DeleteTag(string name);
}
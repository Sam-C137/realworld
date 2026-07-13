using ErrorOr;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Tags.Dto;
using RealWorldApi.Infrastructure.Data;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Tags.Services;

public class TagsService(AppDbContext db, TagsCacheService cache, ILogger<Program> logger): ITagsService
{
    public async Task<ErrorOr<GetTagResponseDto>> CreateTag(CreateTagRequestDto request)
    {
        try
        {
            var tag = new Tag { Name = request.Tag.Name.Trim() };
            db.Tags.Add(tag);
            await db.SaveChangesAsync();
            await cache.InvalidateTagCache(tag.Id);
            await cache.BumpVersionAsync();
            var response = tag.Adapt<GetTagResponseDto>();
            await cache.SetTagToCache(response);
            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error creating tag with name {name}", request.Tag.Name);
            return Error.Failure(description: "An error occurred while creating tag");
        }
    }
    
    public async Task<ErrorOr<GetTagResponseDto>> GetTag(Guid id)
    {
        try
        {
            var cached = await cache.GetTagFromCache(id);
            if (cached is not null) return cached;

            var tag = await db.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (tag is null) return Error.NotFound("Tag not found");

            var response = tag.Adapt<GetTagResponseDto>();
            await cache.SetTagToCache(response);
            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error fetching tag with id {id}", id);
            return Error.Failure(description: "An error occurred while fetching tag");
        }
    }
    
    public async Task<ErrorOr<GetTagResponseDto>> GetTag(string name)
    {
        try
        {
            var tag = await db.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Name == name);
            if (tag is null) return Error.NotFound("Tag not found");
            return tag.Adapt<GetTagResponseDto>();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error fetching tag with name {name}", name);
            return Error.Failure(description: "An error occurred while fetching tag");
        }
    }
    
    public async Task<ErrorOr<PaginatedResponse<string>>> GetTags(GetTagsRequestDto request)
    {
        try
        {
            var fingerprint = request.GetCacheFingerPrint();
            var cached = await cache.GetTagsFromCache(fingerprint);
            if (cached is not null && !ShouldSkipGetTagsCache(request)) return cached;

            var sorted = db.Tags
                .AsNoTracking()
                .FilterTagsAsync(request)
                .SortTagsAsync(request);

            var total = await sorted.CountAsync();
            var tags = await sorted.PaginateTagsAsync(request)
                .Select(t => t.Name)
                .ToListAsync();

            var response = new PaginatedResponse<string>
            {
                Data = tags,
                Total = total,
            };
            if (!ShouldSkipGetTagsCache(request)) await cache.SetTagsToCache(fingerprint, response);
            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error fetching tags with request {@request}", request);
            return Error.Failure(description: "An error occurred while fetching tags");
        }
    }

    public async Task<ErrorOr<GetTagResponseDto>> DeleteTag(string name)
    {
        try
        {
            var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == name);
            if (tag is null) return Error.NotFound("Tag not found");

            db.Tags.Remove(tag);
            await db.SaveChangesAsync();
            await cache.InvalidateTagCache(tag.Id);
            await cache.BumpVersionAsync();
            return tag.Adapt<GetTagResponseDto>();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error deleting tag with name {name}", name);
            return Error.Failure(description: "An error occurred while deleting tag");
        }
    }
    
    private static bool ShouldSkipGetTagsCache(GetTagsRequestDto request) => !string.IsNullOrWhiteSpace(request.Search);
}

public static class TagsFilterSortExtensions
{
    extension(IQueryable<Tag> query)
    {
        public IQueryable<Tag> FilterTagsAsync(GetTagsRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(e => EF.Functions.ILike(e.Name, $"%{request.Search.Trim().ToLowerInvariant()}%"));
            }
            
            // if (!string.IsNullOrWhiteSpace(request.Search))
            // {
            //     var term = request.Search.Trim();
            //     query = query.Where(e => EF.Functions.TrigramsSimilarity(e.Name, term) > 0.2);
            // }

            return query;
        }
        
        public IOrderedQueryable<Tag> SortTagsAsync(GetTagsRequestDto request)
        {
            var sorted = (request.Sort, request.Order) switch
            {
                (TagsSortField.Name, SortOrder.Asc) => query.OrderBy(e => e.Name),
                (TagsSortField.Name, SortOrder.Desc) => query.OrderByDescending(e => e.Name),
                (TagsSortField.Popularity, SortOrder.Desc) => query.OrderByDescending(e => e.ArticleTags.Count),
                (TagsSortField.Popularity, SortOrder.Asc) => query.OrderBy(e => e.ArticleTags.Count),
                (TagsSortField.CreatedAt, SortOrder.Desc) => query.OrderByDescending(e => e.CreatedAt),
                _ => query.OrderBy(e => e.CreatedAt)
            };
            
            // if (!string.IsNullOrWhiteSpace(request.Search))
            // {
            //     var term = request.Search.Trim();
            //     sorted = sorted.ThenByDescending(e => EF.Functions.TrigramsSimilarity(e.Name, term));
            // }
        
            return sorted.ThenBy(e => e.Id);
        }
        
        public IQueryable<Tag> PaginateTagsAsync(GetTagsRequestDto request)
        {
            if (request.Limit is -1) return query;
        
            return query
                .Skip((request.Page - 1) * request.Limit)
                .Take(request.Limit);
        }
    }
}

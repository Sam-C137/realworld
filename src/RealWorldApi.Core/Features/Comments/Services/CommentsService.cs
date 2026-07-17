using System.Security.Claims;
using System.Text.Json;
using ErrorOr;
using Mapster;
using Microsoft.EntityFrameworkCore;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Comments.Dto;
using RealWorldApi.Infrastructure.Data;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Comments.Services;

public class CommentsService(AppDbContext db, IHttpContextAccessor http, ILogger<Program> logger): ICommentsService
{
    public async Task<ErrorOr<GetCommentResponseDto>> GetComment(Guid commentId)
    {
        try
        {
            var userId = Guid.TryParse(http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed)
                ? parsed
                : Guid.Empty;

            var comment = await db.Comments.Include(c => c.Author).FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment is null) return Error.NotFound($"Comment with ID {commentId} not found.");

            var isFollowing = await ComputeCommentProperties(commentId, userId);
            return comment.BuildAdapter()
                .AddParameters("following", isFollowing)
                .AdaptToType<GetCommentResponseDto>();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error retrieving comment with ID {CommentId}", commentId);
            return Error.Failure("An error occurred while retrieving the comment.");
        }
    }

    public async Task<ErrorOr<CursorPaginatedResponse<GetCommentResponseDto>>> GetComments(string slug, GetCommentsRequestDto request)
    {
        try
        {
            var userId = Guid.TryParse(http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed)
                ? parsed
                : Guid.Empty;

            var articleExists = await db.Articles.AnyAsync(a => a.Slug == slug);
            if (!articleExists) return Error.NotFound($"Article with slug {slug} not found");

            var query = db.Comments
                .AsNoTracking()
                .Include(c => c.Article)
                .Where(c => c.Article.Slug == slug)
                .FilterCommentsAsync(request);

            var comments = await query
                .PaginateCommentsAsync(request)
                .Include(c => c.Author)
                .Take(request.Limit + 1)
                .ToListAsync();

            if (comments.Count > request.Limit) comments = comments.Take(request.Limit).ToList();

            var commentIds = comments.Select(c => c.Id).ToList();
            var following = commentIds.Count == 0
                ? new Dictionary<Guid, bool>()
                : await ComputeCommentsProperties(commentIds, userId);

            var (next, prev) = await GetPaginationCursors(query, comments, request);

            return new CursorPaginatedResponse<GetCommentResponseDto>
            {
                Data = comments.Select(c =>
                {
                    following.TryGetValue(c.Id, out var isFollowing);
                    return c.BuildAdapter()
                        .AddParameters("following", isFollowing)
                        .AdaptToType<GetCommentResponseDto>();
                }).ToList(),
                Next = next,
                Previous = prev
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error retrieving comments for article with slug {slug} and parameters {@request}", slug, request);
            return Error.Failure("An error occurred while retrieving the comments.");
        }
    }

    public async Task<ErrorOr<GetCommentResponseDto>> CreateComment(string slug, CreateCommentRequestDto request)
    {
        try
        {
            var userId = Guid.TryParse(http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed)
                ? parsed
                : Guid.Empty;
            if (userId == Guid.Empty) return Error.Unauthorized("User not authenticated");
            
            var article = await db.Articles.FirstOrDefaultAsync(a => a.Slug == slug);
            if (article is null) return Error.NotFound($"Article with slug {slug} not found");
            
            var profile = await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile is null) return Error.Unauthorized("User profile not found");

            var comment = new Comment
            {
                Body = request.Comment.Body,
                BodyJson = request.Comment.BodyJson is not null ? JsonDocument.Parse(request.Comment.BodyJson) : null,
                ArticleId = article.Id,
                AuthorId = profile.Id,
            };
            await db.Comments.AddAsync(comment);
            await db.SaveChangesAsync();
            var isFollowing = await ComputeCommentProperties(comment.Id, userId);
            comment = await db.Comments.Include(c => c.Author).FirstOrDefaultAsync(c => c.Id == comment.Id);

            return comment.BuildAdapter()
                .AddParameters("following", isFollowing)
                .AdaptToType<GetCommentResponseDto>();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error creating comment for article with slug {slug} and request {@request}", slug, request);
            return Error.Failure("An error occurred while creating the comment.");
        }
    }

    public async Task<ErrorOr<object>> DeleteComment(Guid commentId)
    {
        try
        {
            var userId = Guid.TryParse(http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed)
                ? parsed
                : Guid.Empty;
            if (userId == Guid.Empty) return Error.Unauthorized("User not authenticated");

            var existing = await db.Comments
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == commentId);
            if (existing is null) return Error.NotFound($"Comment with ID {commentId} not found.");
            if (existing.Author.UserId != userId) return Error.Forbidden("User not authorized to delete this comment");

            db.Comments.Remove(existing);
            await db.SaveChangesAsync();
            return existing;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error deleting comment with ID {CommentId}", commentId);
            return Error.Failure("An error occurred while deleting the comment.");
        }
    }
    
    private async Task<bool> ComputeCommentProperties(Guid commentId, Guid userId)
    {
        if (userId == Guid.Empty) return false;
        return await db.Comments
            .AsNoTracking()
            .Where(c => c.Id == commentId)
            .Select(c => db.Follows.Any(f => f.Follower.UserId == userId && f.FolloweeId == c.AuthorId))
            .FirstOrDefaultAsync();
    }
    
    private async Task<Dictionary<Guid, bool>> ComputeCommentsProperties(IEnumerable<Guid> commentIds, Guid userId)
    {
        if (userId == Guid.Empty) return new Dictionary<Guid, bool>();
        return await db.Comments
            .AsNoTracking()
            .Where(c => commentIds.Contains(c.Id))
            .Select(c => new { c.Id, Following = db.Follows.Any(f => f.Follower.UserId == userId && f.FolloweeId == c.AuthorId) })
            .ToDictionaryAsync(x => x.Id, x => x.Following);
    }
    
    private static async Task<(string? Next, string? Prev)> GetPaginationCursors(IQueryable<Comment> query, List<Comment> materialized, GetCommentsRequestDto request)
    {
        var count = materialized.Count;
        if(count < 0) return (null, null);
        
        var last = materialized.LastOrDefault()?.Id ?? Guid.Empty;
        var first = materialized.FirstOrDefault()?.Id ?? Guid.Empty;

        var hasNextPage = count > request.Limit || await query.AnyAsync(c => c.Id < last);
        var hasPrevPage = await query.AnyAsync(c => c.Id > first);
        
        return (hasNextPage ? last.ToString() : null, hasPrevPage ? first.ToString() : null);
    }
}

public static class CommentFilterSortExtensions
{
    extension(IQueryable<Comment> query)
    {
        public IQueryable<Comment> PaginateCommentsAsync(GetCommentsRequestDto request)
        {
            Guid? afterId = Guid.TryParse(request.After, out var after) ? after : null;
            Guid? beforeId = Guid.TryParse(request.Before, out var before) ? before : null;
            
            IQueryable<Comment> ordered = query.OrderByDescending(c => c.Id);
            if (afterId.HasValue) ordered = ordered.Where(c => c.Id < afterId);
            if (beforeId.HasValue) ordered = ordered.Where(c => c.Id > beforeId);

            return ordered;
        }

        public IQueryable<Comment> FilterCommentsAsync(GetCommentsRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim();
                query = query.Where(e => EF.Functions.ILike(e.Body, $"%{term}%"));
            }
            
            return query;
        }
    }
}

using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using ErrorOr;
using Mapster;
using Microsoft.EntityFrameworkCore;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Articles.Dto;
using RealWorldApi.Core.Features.Tags.Services;
using RealWorldApi.Infrastructure.Data;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Articles.Services;

public partial class ArticlesService(AppDbContext db, ArticleCacheService cache, TagsCacheService tagsCache, IHttpContextAccessor http, ILogger<Program> logger)
    : IArticlesService
{
    private sealed record ArticlePropertiesComputed
    {
        public bool Following { get; set; }
        public long FavoritesCount { get; set; }
        public bool Favorited { get; set; }
    }
    
    public async Task<ErrorOr<GetArticleResponseDto>> CreateArticle(CreateArticleRequestDto request)
    {
        try
        {
            
            var ctx = http.HttpContext!;
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Error.Unauthorized(description: "User must be authenticated to create articles");
        
            var tags = await EvaluateNewTags(request.Article.TagList);
            var slug = await Slugify(request.Article.Title, request.Article.Description);
            var article = new Article
            {
                Title = request.Article.Title,
                Description = request.Article.Description,
                Body = request.Article.Body,
                BodyJson = request.Article.BodyJson is not null ? JsonDocument.Parse(request.Article.BodyJson) : null,
                Slug = slug,
                AuthorId = Guid.Parse(userId),
                ArticleTags = tags.Select(t => new ArticleTag { TagId = t.Id }).ToList()
            };
            await db.Articles.AddAsync(article);
            await db.SaveChangesAsync();    
        
            article = await db.Articles
                .AsNoTracking()
                .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
                .Include(a => a.Author).ThenInclude(a => a.Profile)
                .FirstAsync(a => a.Id == article.Id);
        
            var computed = await ComputeArticleProperties(article.Id, Guid.Parse(userId));

            var response = article.BuildAdapter()
                .AddParameters("following", computed.Following)
                .AddParameters("favoritesCount", computed.FavoritesCount)
                .AddParameters("favorited", computed.Favorited)
                .AdaptToType<GetArticleResponseDto>();
            
            await cache.InvalidateArticleCache(article.Slug, userId);
            await cache.BumpVersionAsync();
            await cache.SetArticleToCache(response.Article.Slug, response, userId);

            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error creating article with details {@request}", request);
            return Error.Failure(description: "An error occurred while creating article");
        }
    }
    
    public async Task<ErrorOr<GetArticleResponseDto>> GetArticle(string slug)
    {
        try
        {
            var ctx = http.HttpContext!;
            var userId = Guid.TryParse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed)
                ? parsed
                : Guid.Empty;
            
            var cached = await cache.GetArticleFromCache(slug, userId.ToString());
            if (cached is not null) return cached;

            var article = await db.Articles
                .AsNoTracking()
                .Where(a => a.Slug == slug)
                .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
                .Include(a => a.Author)
                .ThenInclude(a => a.Profile)
                .FirstOrDefaultAsync();

            if (article is null) return Error.NotFound("Article not found");

            var computed = await ComputeArticleProperties(article.Id, userId);

            var response = article.BuildAdapter()
                .AddParameters("following", computed.Following)
                .AddParameters("favoritesCount", computed.FavoritesCount)
                .AddParameters("favorited", computed.Favorited)
                .AdaptToType<GetArticleResponseDto>();
            
            await cache.SetArticleToCache(response.Article.Slug, response, userId.ToString());
            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error fetching article with slug {slug}", slug);
            return Error.Failure(description: "An error occurred while fetching article");
        }
    }

    public async Task<ErrorOr<PaginatedResponse<GetArticleResponseDto>>> GetArticles(GetArticlesRequestDto request)
    {
        try
        {
            var ctx = http.HttpContext!;
            Guid? userId = Guid.TryParse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : null;

            var fingerprint = request.GetCacheFingerPrint(userId?.ToString());
            var cached = await cache.GetArticlesFromCache(fingerprint);
            if (cached is not null && !ShouldSkipGetArticlesCache(request)) return cached;
            
            var sorted = db.Articles
                .AsNoTracking()
                .FilterArticlesAsync(request, db)
                .SortArticlesAsync(request);
            
            var total = await sorted.CountAsync();
            var paginated = await sorted
                .PaginateArticlesAsync(request)
                .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
                .Include(a => a.Author)
                .ThenInclude(a => a.Profile)
                .ToListAsync();

            var computed = await ComputeArticlesProperties(paginated, userId ?? Guid.Empty);

            var response =  new PaginatedResponse<GetArticleResponseDto>
            {
                Data = paginated.Select(a =>
                {
                    computed.TryGetValue(a.Id, out var c);
                        
                    return a.BuildAdapter()
                        .AddParameters("following", c?.Following ?? false)
                        .AddParameters("favoritesCount", c?.FavoritesCount ?? 0L)
                        .AddParameters("favorited", c?.Favorited ?? false)
                        .AdaptToType<GetArticleResponseDto>();
                }).ToList(),
                Total = total
            };
            if (!ShouldSkipGetArticlesCache(request)) await cache.SetArticlesToCache(fingerprint, response);
            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error fetching articles with details {@request}", request);
            return Error.Failure(description: "An error occurred while fetching articles");
        }
    }
    
    public async Task<ErrorOr<PaginatedResponse<GetArticleResponseDto>>> GetFeed(Guid userId, GetArticlesRequestDto request)
    {
        try
        {
            var fingerprint = request.GetCacheFingerPrint(userId.ToString(), true);
            var cached = await cache.GetArticlesFromCache(fingerprint);
            if (cached is not null && !ShouldSkipGetArticlesCache(request)) return cached;
            
            var sorted = db.Articles
                .AsNoTracking()
                .FilterByFeedAsync(userId, db)
                .FilterArticlesAsync(request, db)
                .SortArticlesAsync(request);
            
            var total = await sorted.CountAsync();
            var paginated = await sorted
                .PaginateArticlesAsync(request)
                .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
                .Include(a => a.Author)
                .ThenInclude(a => a.Profile)
                .ToListAsync();

            var computed = await ComputeArticlesProperties(paginated, userId);

            var response =  new PaginatedResponse<GetArticleResponseDto>
            {
                Data = paginated.Select(a =>
                {
                    computed.TryGetValue(a.Id, out var c);
                        
                    return a.BuildAdapter()
                        .AddParameters("following", c?.Following ?? false)
                        .AddParameters("favoritesCount", c?.FavoritesCount ?? 0L)
                        .AddParameters("favorited", c?.Favorited ?? false)
                        .AdaptToType<GetArticleResponseDto>();
                }).ToList(),
                Total = total
            };
            if (!ShouldSkipGetArticlesCache(request)) await cache.SetArticlesToCache(fingerprint, response);
            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error fetching feed for {userId} with details {@request}", userId, request);
            return Error.Failure(description: "An error occurred while fetching feed");
        }
    }
    
    public async Task<ErrorOr<GetArticleResponseDto>> UpdateArticle(string slug, UpdateArticleRequestDto request)
    {
        try
        {
            var ctx = http.HttpContext!;
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existing = await db.Articles
                .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
                .Include(a => a.Author)
                .ThenInclude(a => a.Profile)
                .FirstOrDefaultAsync(a => a.Slug == slug);

            if (existing is null) return Error.NotFound("Article not found");
            if (existing.Author.Id.ToString() != userId)
                return Error.Forbidden(description: "You are not authorized to update this article");

            if (!string.IsNullOrWhiteSpace(request.Article.Title))
            {
                existing.Title = request.Article.Title;
                if (string.Equals(existing.Title, request.Article.Title, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(existing.Description, request.Article.Description,
                        StringComparison.OrdinalIgnoreCase))
                {
                    // If title and description are unchanged (ignoring case), keep the existing slug
                }
                else
                {
                    existing.Slug = await Slugify(existing.Title, existing.Description);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Article.Description))
            {
                existing.Description = request.Article.Description;
            }

            if (!string.IsNullOrWhiteSpace(request.Article.Body))
            {
                existing.Body = request.Article.Body;
            }

            if (request.Article.TagList is not null)
            {
                var tags = await EvaluateNewTags(request.Article.TagList);
                existing.ArticleTags = tags.Select(t => new ArticleTag { TagId = t.Id }).ToList();
            }

            await db.SaveChangesAsync();
            var computed = await ComputeArticleProperties(existing.Id, Guid.Parse(userId));
            
            await cache.InvalidateArticleCache(existing.Slug, userId);
            await cache.BumpVersionAsync();

            return existing.BuildAdapter()
                .AddParameters("following", computed.Following)
                .AddParameters("favoritesCount", computed.FavoritesCount)
                .AddParameters("favorited", computed.Favorited)
                .AdaptToType<GetArticleResponseDto>();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error updating article with slug {slug} and details {@request}", slug, request);
            return Error.Failure(description: "An error occurred while updating article");
        }
    }

    public async Task<ErrorOr<object>> DeleteArticle(string slug)
    {
        try
        {
            var ctx = http.HttpContext!;
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var exiting = await db.Articles
                .Include(a => a.Author)
                .ThenInclude(a => a.Profile)
                .FirstOrDefaultAsync(a => a.Slug == slug);

            if (exiting is null) return Error.NotFound("Article not found");
            if (exiting.Author.Id.ToString() != userId)
                return Error.Forbidden(description: "You are not authorized to delete this article");

            db.Articles.Remove(exiting);
            await db.SaveChangesAsync();
            
            await cache.InvalidateArticleCache(exiting.Slug, userId);
            await cache.BumpVersionAsync();
            
            return exiting;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error deleting article with slug {slug}", slug);
            return Error.Failure(description: "An error occurred while deleting article");
        }
    }

    public async Task<ErrorOr<GetArticleResponseDto>> FavoriteArticle(string slug)
    {
        try
        {
            if (!Guid.TryParse(http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Error.Unauthorized("You must be logged in to favorite an article");

            var affected = await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO likes (id, article_id, user_id, created_at, updated_at)
                SELECT {Guid.CreateVersion7()}, id, {userId}, NOW(), NOW()
                FROM articles
                WHERE slug = {slug}
                ON CONFLICT (user_id, article_id) DO UPDATE
                SET updated_at = likes.updated_at
                """);
            if (affected == 0) return Error.NotFound("Article not found");

            await cache.BumpVersionAsync();
            await cache.InvalidateArticleCache(slug, userId.ToString());
            return await GetArticle(slug);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error favoriting article with slug {slug}", slug);
            return Error.Failure(description: "An error occurred while favoriting article");
        }
    }

    public async Task<ErrorOr<GetArticleResponseDto>> UnfavoriteArticle(string slug)
    {
        try
        {
            if (!Guid.TryParse(http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Error.Unauthorized("You must be logged in to unfavorite an article");

            var affected = await db.Database.ExecuteSqlInterpolatedAsync($"""
                WITH target AS (
                    SELECT id
                    FROM articles
                    WHERE slug = {slug}
                ),
                deleted AS (
                    DELETE FROM likes
                    USING target
                    WHERE likes.article_id = target.id
                      AND likes.user_id = {userId}
                    RETURNING likes.id
                )
                UPDATE articles
                SET updated_at = articles.updated_at
                WHERE id IN (SELECT id FROM target)
                """);
            if (affected == 0) return Error.NotFound("Article not found");

            await cache.BumpVersionAsync();
            await cache.InvalidateArticleCache(slug, userId.ToString());
            return await GetArticle(slug);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error unfavoriting article with slug {slug}", slug);
            return Error.Failure(description: "An error occurred while unfavoriting article");
        }
    }
    
    private async Task<ArticlePropertiesComputed> ComputeArticleProperties(Guid articleId, Guid userId)
    {
        return await db.Articles
            .AsNoTracking()
            .Where(a => a.Id == articleId)
            .Select(a => new ArticlePropertiesComputed {
                Following = userId != Guid.Empty && db.Follows.Any(follow =>
                    follow.Followee.UserId == a.AuthorId &&
                    follow.Follower.UserId == userId),
                FavoritesCount = db.Likes.LongCount(like => like.ArticleId == a.Id),
                Favorited = userId != Guid.Empty && db.Likes.Any(like => like.ArticleId == a.Id && like.UserId == userId)
            })
            .FirstAsync();
    }

    private async Task<Dictionary<Guid, ArticlePropertiesComputed>> ComputeArticlesProperties(
        IEnumerable<Article> articles, Guid userId)
    {
        var articleDetails = articles.Select(a => (articleId: a.Id, authorId: a.AuthorId)).ToList();
        
        if (articleDetails.Count == 0) return new Dictionary<Guid, ArticlePropertiesComputed>();

        var result = articleDetails.ToDictionary(
            x => x.articleId,
            _ => new ArticlePropertiesComputed());

        var articleIdsByAuthor = articleDetails
            .GroupBy(x => x.authorId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.articleId).ToList());

        var articleIds = articleDetails.Select(d => d.articleId).ToList();
        var authorIds = articleDetails.Select(d => d.authorId).ToList();

        var likes = await db.Likes
            .Where(l => articleIds.Contains(l.ArticleId))
            .GroupBy(l => l.ArticleId)
            .Select(g => new { g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        foreach (var (articleId, count) in likes)
        {
            result[articleId].FavoritesCount = count;
        }

        if (userId == Guid.Empty) return result;
        
        var favorited = await db.Likes
            .Where(l => articleIds.Contains(l.ArticleId) && l.UserId == userId)
            .Select(l => l.ArticleId)
            .ToHashSetAsync();

        foreach (var articleId in favorited)
        {
            result[articleId].Favorited = true;
        }

        var following = await db.Follows
            .Where(f => authorIds.Contains(f.Followee.UserId) && f.Follower.UserId == userId)
            .Select(f => f.Followee.UserId)
            .ToHashSetAsync();

        foreach (var authorId in following)
        {
            if (!articleIdsByAuthor.TryGetValue(authorId, out var ids)) continue;
            foreach (var articleId in ids)
            {
                result[articleId].Following = true;
            }
        }

        return result;
    }
    
    private async Task<IEnumerable<Tag>> EvaluateNewTags(IEnumerable<string> tagList)
    {
        var requestedTags = tagList
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .ToList();

        if (requestedTags.Count == 0) return [];

        var existing = await db.Tags
            .Where(t => requestedTags.Contains(t.Name.ToLower()))
            .ToListAsync();

        var existingNames = existing.Select(t => t.Name.ToLowerInvariant()).ToHashSet();
        var newTags = requestedTags
            .Where(t => !existingNames.Contains(t))
            .Select(t => new Tag { Name = t })
            .ToList();

        if (newTags.Count == 0) return existing;

        await db.Tags.AddRangeAsync(newTags);
        await db.SaveChangesAsync();
        await tagsCache.BumpVersionAsync();
        return existing.Concat(newTags);
    }
    
    [GeneratedRegex(@"[^a-z0-9\s\-_]")]
    private static partial Regex SlugSanityRegex();
    
    private async Task<string> Slugify(string title, string description)
    {
        var baseSlug = SlugSanityRegex()
            .Replace((title + " " + description).ToLowerInvariant(), "")
            .Replace(" ", "-").Trim('-').Trim();

        var slugParts = baseSlug.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (slugParts.Length > 10)
            baseSlug = string.Join("-", slugParts.Take(10));

        var existingSlugs = await db.Articles
            .Where(a => a.Slug == baseSlug || a.Slug.StartsWith(baseSlug + "-"))
            .Select(a => a.Slug)
            .ToHashSetAsync();

        var slug = baseSlug;
        var counter = 1;
        while (existingSlugs.Contains(slug))
            slug = $"{baseSlug}-{counter++}";

        return slug;
    }
    
    private static bool ShouldSkipGetArticlesCache(GetArticlesRequestDto request) => !string.IsNullOrWhiteSpace(request.Search);
}

public static class ArticleFilterSortExtensions
{
    extension(IQueryable<Article> query)
    {
        public IQueryable<Article> FilterByFeedAsync(Guid loggedInUserId, AppDbContext db)
        {
            if (loggedInUserId == Guid.Empty) return query;
            
            return query.Where(a => db.Follows
                .Where(f => f.Follower.UserId == loggedInUserId)
                .Select(f => f.Followee.UserId)
                .Distinct()
                .Any(followeeId => followeeId == a.AuthorId));
        }
        
        public IQueryable<Article> FilterArticlesAsync(GetArticlesRequestDto request, AppDbContext db)
        {
            if (!string.IsNullOrWhiteSpace(request.Tag))
            {
                var term = request.Tag.Trim();
                query = query.Where(a => a.ArticleTags.Any(at => EF.Functions.ILike(at.Tag.Name, $"%{term}%")));
            }

            if (!string.IsNullOrWhiteSpace(request.Author))
            {
                var author = request.Author.Trim();
                query = query.Where(a => EF.Functions.ILike(a.Author.Profile.Username, $"%{author}%"));
            }

            if (!string.IsNullOrWhiteSpace(request.Favorited))
            {
                query = query.Where(a => db.Likes.Any(l => 
                    l.ArticleId == a.Id && 
                    EF.Functions.ILike(l.User.Profile.Username, $"%{request.Favorited.Trim()}%")));
            }
            
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim();
                query = query.Where(a => a.SearchVector.Matches(
                    EF.Functions.WebSearchToTsQuery("english", term)));
            }
            
            return query;
        }

        public IOrderedQueryable<Article> SortArticlesAsync(GetArticlesRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim();
                return query.OrderByDescending(e => e.SearchVector.Rank(
                        EF.Functions.WebSearchToTsQuery("english", term)))
                    .ThenBy(e => e.Id);
            }

            var sorted = (request.Sort, request.Order) switch
            {
                (ArticlesSortField.Title, SortOrder.Asc) => query.OrderBy(e => e.Title),
                (ArticlesSortField.Title, SortOrder.Desc) => query.OrderByDescending(e => e.Title),
                (ArticlesSortField.CreatedAt, SortOrder.Desc) => query.OrderByDescending(e => e.CreatedAt),
                _ => query.OrderBy(e => e.CreatedAt)
            };
            
            return sorted.ThenBy(e => e.Id);
        }

        public IQueryable<Article> PaginateArticlesAsync(GetArticlesRequestDto request)
        {
            if (request.Limit is -1) return query;
            
            return query
                .Skip((request.Page - 1) * request.Limit)
                .Take(request.Limit);
        }
    }
}

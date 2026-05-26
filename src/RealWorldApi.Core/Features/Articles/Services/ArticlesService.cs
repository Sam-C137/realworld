using System.Security.Claims;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using RealWorldApi.Core.Features.Articles.Dto;
using RealWorldApi.Core.Features.Tags.Services;
using RealWorldApi.Infrastructure.Data;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Articles.Services;

public class ArticlesService(AppDbContext db, ArticleCacheService cache, TagsCacheService tagsCache, IHttpContextAccessor http, ILogger<Program> logger)
    : IArticlesService
{
    public async Task<ErrorOr<Article>> CreateArticle(CreateArticleRequestDto request)
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
            Slug = slug,
            AuthorId = Guid.Parse(userId),
            ArticleTags = tags.Select(t => new ArticleTag { TagId = t.Id }).ToList()
        };
        await db.Articles.AddAsync(article);
        await db.SaveChangesAsync();    
        return article;
    }
    
    public async Task<ErrorOr<Article>> GetArticle(string slug)
    {
        var article = await db.Articles
            .AsNoTracking()
            .Include(a => a.ArticleTags)
            .ThenInclude(at => at.Tag)
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Slug == slug);
        if (article is null) return Error.NotFound("Article not found");
        return article;   
    }

    public async Task<ErrorOr<Article>> UpdateArticle(string slug, UpdateArticleRequestDto request)
    {
        var ctx = http.HttpContext!;
        var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var existing = await db.Articles
            .Include(a => a.ArticleTags)
            .ThenInclude(at => at.Tag)
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Slug == slug);
        
        if (existing is null) return Error.NotFound("Article not found");
        if (existing.Author.Id.ToString() != userId) return Error.Forbidden(description: "You are not authorized to update this article");
        
        if (!string.IsNullOrWhiteSpace(request.Article.Title))
        {
            existing.Title = request.Article.Title;
            if (string.Equals(existing.Title, request.Article.Title, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.Description, request.Article.Description, StringComparison.OrdinalIgnoreCase))
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
        return existing;
    }

    public Task<ErrorOr<Article>> DeleteArticle(string slug)
    {
        throw new NotImplementedException();
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
    
    private async Task<string> Slugify(string title, string description)
    {
        var baseSlug = (title + " " + description)
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("?", "")
            .Replace("!", "")
            .Replace(":", "")
            .Replace(";", "")
            .Trim('-');

        var existingSlugs = await db.Articles
            .Where(a => a.Slug == baseSlug || a.Slug.StartsWith(baseSlug + "-"))
            .Select(a => a.Slug)
            .ToHashSetAsync();

        var slug = baseSlug;
        var counter = 1;

        while (existingSlugs.Contains(slug))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        return slug;
    }
}
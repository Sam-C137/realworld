using System.Security.Claims;
using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Articles.Dto;
using RealWorldApi.Core.Features.Articles.Services;

namespace RealWorldApi.Core.Features.Articles;

public class ArticlesController(IArticlesService articlesService): BaseController
{
    /// <summary>
    /// Get article by slug
    /// </summary>
    /// <param name="slug">The article slug</param>
    /// <returns />
    [HttpGet("{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetArticleResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticleBySlug([FromRoute] string slug)
    {
        return await articlesService.GetArticle(slug)
            .Match<GetArticleResponseDto, IActionResult>(Ok,
                errors =>
                {
                    var error = errors.First();
                    return error.Type switch
                    {
                        ErrorType.NotFound => NotFound(),
                        _ => Problem(error.Description)
                    };
                });
    }
    
    /// <summary>
    /// Create a new article
    /// </summary>
    /// <param name="request"></param>
    /// <returns />
    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(GetArticleResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateArticle([FromBody] CreateArticleRequestDto request)
    {
        return await articlesService.CreateArticle(request)
            .Match(
                a => CreatedAtAction(nameof(GetArticleBySlug), new { slug = a.Article.Slug }, a), 
                errors => Problem(errors.First().Description));
    }
    
    /// <summary>
    /// Update an existing article. Only the author of the article can update it.
    /// </summary>
    /// <param name="slug">The article slug</param>
    /// <param name="request">Optional parts of the article to be updated</param>
    /// <returns />
    [Authorize]
    [HttpPut("{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetArticleResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateArticle([FromRoute] string slug, [FromBody] UpdateArticleRequestDto request)
    {
        return await articlesService.UpdateArticle(slug, request)
            .Match<GetArticleResponseDto, IActionResult>(
                Ok, errors => {
                    var error = errors.First();
                    return error.Type switch
                    {
                        ErrorType.NotFound => NotFound(),
                        ErrorType.Forbidden => Forbid(),
                        _ => Problem(error.Description)
                    };
                });
    }
    
    /// <summary>
    /// Delete an existing article. Only the author of the article can delete it.
    /// </summary>
    /// <param name="slug"></param>
    /// <returns/>
    [Authorize]
    [HttpDelete("{slug}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteArticle([FromRoute] string slug)
    {
        return await articlesService.DeleteArticle(slug)
            .Match<object, IActionResult>(
                _ => NoContent(), 
                errors => {
                    var error = errors.First();
                    return error.Type switch
                    {
                        ErrorType.NotFound => NotFound(),
                        ErrorType.Forbidden => Forbid(),
                        _ => Problem(error.Description)
                    };
                });
    }
    
    /// <summary>
    /// Get articles with optional filters. The response is paginated.
    /// </summary>
    /// <param name="request"></param>
    /// <returns/>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<GetArticleResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]   
    public async Task<IActionResult> GetArticles([FromQuery] GetArticlesRequestDto request)
    {
        return await articlesService.GetArticles(request)
            .Match(Ok, errors => Problem(errors.First().Description));
    }
    
    /// <summary>
    /// Get articles from users followed by the current user. The response is paginated.
    /// </summary>
    /// <param name="request"></param>
    /// <returns/>
    [Authorize]
    [HttpGet("feed")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<GetArticleResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] 
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFeed([FromQuery] GetArticlesRequestDto request)
    {
        var userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsed) ? parsed : Guid.Empty;
        return await articlesService.GetFeed(userId, request)
            .Match(Ok, errors => Problem(errors.First().Description));
    }

    /// <summary>
    /// Favorite article.
    /// Only authenticated users can favorite an article.
    /// Favoriting an article that is already favorited by the user will have no effect.
    /// </summary>
    /// <param name="slug"></param>
    /// <returns></returns>
    [Authorize]
    [HttpPost("{slug}/favorite")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetArticleResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> FavoriteArticle([FromRoute] string slug)
    {
        return await articlesService.FavoriteArticle(slug)
            .Match<GetArticleResponseDto, IActionResult>(
                Ok, errors =>
                {
                    var error = errors.First();
                    return error.Type switch
                    {
                        ErrorType.NotFound => NotFound(),
                        ErrorType.Unauthorized => Unauthorized(),
                        _ => Problem(error.Description)
                    };
                });
    }

    /// <summary>
    /// Unfavorite article.
    /// Only authenticated users can unfavorite an article.
    /// Unfavoriting an article that is not favorited by the user will have no effect.
    /// </summary>
    /// <param name="slug"></param>
    /// <returns></returns>
    [Authorize]
    [HttpDelete("{slug}/favorite")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetArticleResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnfavoriteArticle([FromRoute] string slug)
    {
        return await articlesService.UnfavoriteArticle(slug)
            .Match<GetArticleResponseDto, IActionResult>(
                Ok, errors =>
                {
                    var error = errors.First();
                    return error.Type switch
                    {
                        ErrorType.NotFound => NotFound(),
                        ErrorType.Unauthorized => Unauthorized(),
                        _ => Problem(error.Description)
                    };
                });
    }
}
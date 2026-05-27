using ErrorOr;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Articles.Dto;
using RealWorldApi.Core.Features.Articles.Services;
using RealWorldApi.Infrastructure.Data.Models;

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
                errors => Problem(errors.First().Description));;
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
    
    [HttpGet]
    public async Task<IActionResult> GetArticles()
    {
        await Task.CompletedTask;
        return Ok();
    }
}
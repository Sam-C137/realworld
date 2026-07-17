using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Comments.Dto;
using RealWorldApi.Core.Features.Comments.Services;

namespace RealWorldApi.Core.Features.Comments;

[Route("api/v1/articles/{slug}/comments")]
public class CommentsController(ICommentsService commentsService): BaseController
{
    /// <summary>
    /// Get comments for an article
    /// </summary>
    /// <param name="slug">The slug of the article containing the comment.</param>
    /// <param name="request">Pagination cursor</param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CursorPaginatedResponse<GetCommentResponseDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComments([FromRoute] string slug, [FromQuery] GetCommentsRequestDto request)
    {
        return await commentsService.GetComments(slug, request)
            .Match(Ok, errors =>
            {
                var error = errors.First();
                return error.Type switch
                {
                    ErrorType.NotFound => NotFound(error.Description),
                    _ => Problem(error.Description)
                };
            });
    }

    /// <summary>
    /// Retrieves a specific comment for a given article.
    /// </summary>
    /// <param name="slug">The slug of the article containing the comment.</param>
    /// <param name="commentId">The ID of the comment to retrieve.</param>
    /// <returns />
    [HttpGet("{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetCommentResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComment([FromRoute] string slug, [FromRoute] Guid commentId)
    {
        return await commentsService.GetComment(commentId)
            .Match(Ok, errors =>
            {
                var error = errors.First();
                return error.Type switch
                {
                    ErrorType.NotFound => NotFound(error.Description),
                    _ => Problem(error.Description)
                };
            });
    }
    
    /// <summary>
    /// Add a new comment to an article.
    /// </summary>
    /// <param name="slug">The slug of the article to which the comment will be added.</param>
    /// <param name="request">The comment details, including the comment body.</param>
    /// <returns />
    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(GetCommentResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddComment([FromRoute] string slug, [FromBody] CreateCommentRequestDto request)
    {
        return await commentsService.CreateComment(slug, request)
            .Match(
                c => CreatedAtAction(nameof(GetComment), new { slug, commentId = c.Id }, c),
                errors =>
                {
                    var error = errors.First();
                    return error.Type switch
                    {
                        ErrorType.NotFound => NotFound(error.Description),
                        _ => Problem(error.Description)
                    };
                });
    }
    
    /// <summary>
    /// Delete a comment. Only the author of the comment can delete it.
    /// </summary>
    /// <param name="slug">The slug of the article containing the comment.</param>
    /// <param name="commentId">The ID of the comment to delete.</param>
    /// <returns />
    [Authorize]
    [HttpDelete("{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]   
    public async Task<IActionResult> DeleteComment([FromRoute] string slug, [FromRoute] Guid commentId)
    {
        return await commentsService.DeleteComment(commentId)
            .Match<object, IActionResult>(_ => NoContent(), errors =>
            {
                var error = errors.First();
                return error.Type switch
                {
                    ErrorType.NotFound => NotFound(error.Description),
                    ErrorType.Forbidden => Forbid(),
                    _ => Problem(error.Description)
                };
            });
    }
}

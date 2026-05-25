using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Tags.Dto;
using RealWorldApi.Core.Features.Tags.Services;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Tags;

public class TagsController(ITagsService tagsService): BaseController
{
    
    [HttpGet("{nameOrId:minLength(3)}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Tag))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTag([FromRoute] string nameOrId)
    {
        var isGuid = Guid.TryParse(nameOrId, out var guid);
        var result = isGuid ? await tagsService.GetTag(id: guid) : await tagsService.GetTag(name: nameOrId);
        return result.Match<IActionResult>(Ok, errors =>
            {
                var error = errors.First();
                return error.Type switch
                {   
                    ErrorType.NotFound => NotFound(),
                    _ => Problem(error.Description)
                };
            });
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Tag))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagRequestDto request)
    {
        return await tagsService.CreateTag(request)
            .Match(
                tag => CreatedAtAction(nameof(GetTag), new { NameOrId = tag.Id }, tag), 
                errors => Problem(errors.First().Description));
    }
    
    /// <summary>
    /// Get all tags
    /// </summary>
    /// <param name="request"><see cref="GetTagsRequestDto"/></param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginatedResponse<string>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTags([FromQuery] GetTagsRequestDto request)
    {
        return await tagsService.GetTags(request)
            .Match(Ok, errors => Problem(errors.First().Description));
    }
    
    [Authorize]
    [HttpDelete("{name:minlength(3):maxlength(255)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTag([FromRoute] string name)
    {
        return await tagsService.DeleteTag(name)
            .Match<Tag, IActionResult>(_ => NoContent(), errors => Problem(errors.First().Description));
    }
}
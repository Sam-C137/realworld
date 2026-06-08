using System.Security.Claims;
using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Profiles.Dto;
using RealWorldApi.Core.Features.Profiles.Services;

namespace RealWorldApi.Core.Features.Profiles;

public class ProfilesController(IProfileService profileService): BaseController
{
    [HttpGet("{username}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetProfileResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile([FromRoute] string username)
    {
        Guid? userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsed) ? parsed : null;
        return await profileService.GetProfile(username, userId)
            .Match<GetProfileResponseDto, IActionResult>(Ok,
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

    [Authorize]
    [HttpPost("{username}/follow")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetProfileResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FollowUser([FromRoute] string username)
    {
        var userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsed) ? parsed : 
            Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();
        
        return await profileService.FollowProfile(userId, username)
            .Match<GetProfileResponseDto, IActionResult>(Ok,
                errors =>
                {
                    var error = errors.First();
                    return error.Type switch
                    {
                        ErrorType.NotFound => NotFound(),
                        ErrorType.Failure => BadRequest(error.Description),
                        _ => Problem(error.Description)
                    };
                });
    }

    [Authorize]
    [HttpDelete("{username}/follow")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetProfileResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnfollowUser([FromRoute] string username)
    {
        var userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsed) ? parsed : 
            Guid.Empty;
        if (userId == Guid.Empty) return Unauthorized();
        
        return await profileService.UnfollowProfile(userId, username)
            .Match<GetProfileResponseDto, IActionResult>(Ok,
                errors =>
                {
                    var error = errors.First();
                    return error.Type switch
                    {
                        ErrorType.NotFound => NotFound(),
                        ErrorType.Failure => BadRequest(error.Description),
                        _ => Problem(error.Description)
                    };
                });
    }
}
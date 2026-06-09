using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Users.Dto;
using RealWorldApi.Core.Features.Users.Services;

namespace RealWorldApi.Core.Features.Users;

public class UserController(IUserService userService)
    : BaseController
{
    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser()
    {
        return await userService.GetUser()
            .Match<LoginResponseDto, IActionResult>(
                Ok, errors =>
                {
                    var error = errors.First();
                    return error.Type switch
                    {
                        ErrorType.Unauthorized => Unauthorized(),
                        ErrorType.NotFound => NotFound(),
                        ErrorType.Validation => BadRequest(error.Description),
                        _ => Problem(error.Description)
                    };
                });
    }

    [Authorize]
    [HttpPut]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser([FromForm] UpdateUserRequestDto request)
    {
        return await userService.UpdateUser(request)
            .Match<LoginResponseDto, IActionResult>(
                Ok, errors =>
                {
                    var error = errors.First();
                    return error.Type switch
                    {
                        ErrorType.Unauthorized => Unauthorized(),
                        ErrorType.NotFound => NotFound(),
                        ErrorType.Validation => BadRequest(error.Description),
                        _ => Problem(error.Description)
                    };
                });
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Users.Dto;
using RealWorldApi.Core.Features.Users.Services;

namespace RealWorldApi.Core.Features.Users;

[EnableRateLimiting("default_sliding")]
public class UsersController(IUsersService usersService, ILogger<Program> logger): BaseController
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var (user, token) = await usersService.Register(request);
        if (user is null)
        {
            logger.LogError("User already exists for request {request}", request);
            return Conflict(new {message = "User already exists"});
        }
        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var (user, token) = await usersService.Login(request);
        if (user is null) return Unauthorized(new {message = "Invalid credentials"});
        return Ok();
    }
}
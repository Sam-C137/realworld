
using System.Security.Claims;
using ErrorOr;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Users.Dto;
using RealWorldApi.Core.Features.Users.Services;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Users;

[EnableRateLimiting("default_sliding")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public class UsersController(IUsersService usersService, EmailRateLimitService limiter): BaseController
{
    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request"><see cref="RegisterResponseDto"/></param>
    /// <returns>Basic user info and access token</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RegisterResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        return await usersService.Register(request)
            .Match<(User, string), IActionResult>(
                value => Ok(value.Item1
                    .BuildAdapter()
                    .AddParameters("token", value.Item2)
                    .AdaptToType<RegisterResponseDto>()),
                errors =>
                {
                    return errors.First().Type switch
                    {
                        ErrorType.Conflict => Conflict(new {message = "User already exists"}),
                        _ => Problem("Registration failed")
                    };
                });
    }

    /// <summary>
    /// Log in an existing user
    /// </summary>
    /// <param name="request"><see cref="LoginResponseDto"/></param>
    /// <returns>Basic user info and access token</returns>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var (allowed, retryAfter) = await limiter.CheckAndRecordAsync(request.User.Email);
        if (!allowed)
        {
            Response.Headers.RetryAfter = retryAfter.ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Too many login attempts for this account" });
        }
        
        return await usersService.Login(request)
            .MatchAsync<(User, string), IActionResult>(
                async value =>
                {
                    await limiter.ClearFailuresAsync(request.User.Email);
                    return Ok(value.Item1
                        .BuildAdapter()
                        .AddParameters("token", value.Item2)
                        .AdaptToType<LoginResponseDto>());
                },
                 async errors =>
                {
                    await limiter.RecordFailureAsync(request.User.Email);
                    return errors.First().Type switch
                    {
                        ErrorType.NotFound => Unauthorized(new { message = "Invalid credentials" }),
                        ErrorType.Unauthorized => Unauthorized(new { message = "Invalid credentials" }),
                        _ => Problem("Login failed")
                    };
                });
    }
    
    [HttpDelete("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout()
    {
        var sessionId = User.FindFirstValue("sid");
        if (string.IsNullOrWhiteSpace(sessionId)) return BadRequest(new { message = "Invalid session" });
        await usersService.Logout(Guid.Parse(sessionId));
        return NoContent();
    }

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken()
    {
        return await usersService.RefreshToken()
            .Match<string, IActionResult>(
                token => Ok(new { token }),
                errors =>
                {
                    var error = errors.First();
                    return error.Type switch
                    {
                        ErrorType.Unauthorized => Unauthorized(new { message = error.Description }),
                        _ => Problem("Refresh token failed")
                    };
                }
                );
    }
}
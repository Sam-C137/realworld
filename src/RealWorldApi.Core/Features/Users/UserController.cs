using Microsoft.AspNetCore.Mvc;
using RealWorldApi.Core.Abstractions;

namespace RealWorldApi.Core.Features.Users;

public class UserController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetUser()
    {
        await Task.CompletedTask;
        return Ok();
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateUser()
    {
        await Task.CompletedTask;
        return Ok();
    }
}
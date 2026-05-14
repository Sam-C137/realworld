using RealWorldApi.Core.Features.Users.Dto;
using RealWorldApi.Infrastructure.Data;

namespace RealWorldApi.Core.Features.Users.Services;

public class UsersService(AppDbContext db, IConfiguration configuration) : IUsersService
{
    public async Task<(User?, string)> Register(RegisterRequestDto request)
    {
        await Task.CompletedTask;
        return (null, string.Empty);
    }
    
    public async Task<(User?, string)> Login(LoginRequestDto request)
    {
        await Task.CompletedTask;
        return (null, string.Empty);
    }
}
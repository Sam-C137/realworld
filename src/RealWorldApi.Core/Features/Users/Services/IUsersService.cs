using RealWorldApi.Core.Features.Users.Dto;

namespace RealWorldApi.Core.Features.Users.Services;

public interface IUsersService
{
    Task<(User?, string)> Register(RegisterRequestDto request);
    Task<(User?, string)> Login(LoginRequestDto request);
}
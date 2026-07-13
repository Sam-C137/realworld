using ErrorOr;
using RealWorldApi.Core.Features.Users.Dto;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Users.Services;

public interface IUsersService
{
    Task<ErrorOr<(User, string accessToken, string csrfToken)>> Register(RegisterRequestDto request);
    Task<ErrorOr<(User, string accessToken, string csrfToken)>> Login(LoginRequestDto request);
    Task Logout(Guid sessionId);
    Task<ErrorOr<string>> RefreshToken();
}
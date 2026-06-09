using ErrorOr;
using RealWorldApi.Core.Features.Users.Dto;

namespace RealWorldApi.Core.Features.Users.Services;

public interface IUserService
{
    public Task<ErrorOr<LoginResponseDto>> GetUser();
    public Task<ErrorOr<LoginResponseDto>> UpdateUser(UpdateUserRequestDto request);
}
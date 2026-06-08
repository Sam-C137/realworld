using RealWorldApi.Core.Features.Profiles.Dto;
using ErrorOr;

namespace RealWorldApi.Core.Features.Profiles.Services;

public interface IProfileService
{
    public Task<ErrorOr<GetProfileResponseDto>> GetProfile(string username, Guid? userId = null);
    public Task<ErrorOr<GetProfileResponseDto>> FollowProfile(Guid userId, string username);
    public Task<ErrorOr<GetProfileResponseDto>> UnfollowProfile(Guid userId, string username);
}
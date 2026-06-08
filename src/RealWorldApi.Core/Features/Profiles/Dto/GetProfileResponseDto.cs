using Mapster;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Profiles.Dto;

public class GetProfileResponseDto
{
    public ProfileDto Profile { get; set; } = null!;
}

public class GetProfileResponseDtoMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Profile, GetProfileResponseDto>()
            .Map(dest => dest.Profile, src => src);
    }
}
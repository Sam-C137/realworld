using Mapster;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Profiles.Dto;

public record ProfileDto(
    string Username,
    string? Bio,
    string? Image,
    bool Following
);

public class ProfileDtoMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Profile, ProfileDto>()
            .Map(dest => dest.Following, src => MapContext.Current.Parameters["following"]);
    }
}
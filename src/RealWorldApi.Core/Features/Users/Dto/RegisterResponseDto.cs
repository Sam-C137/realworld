using Mapster;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Users.Dto;

public class RegisterResponseDto
{
    public required UserResponseDto User { get; set; }
}

public class RegisterResponseMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, RegisterResponseDto>()
            .Map(dest => dest.User.Email, src => src.Email)
            .Map(dest => dest.User.Username, src => src.Profile.Username)
            .Map(dest => dest.User.Bio, src => src.Profile.Username)
            .Map(dest => dest.User.Image, src => src.Profile.Username)
            .Map(dest => dest.User.Token, src => MapContext.Current.Parameters["token"]);
    }
}
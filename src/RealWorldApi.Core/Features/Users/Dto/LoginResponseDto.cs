using Mapster;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Users.Dto;

public class LoginResponseDto
{
    public UserResponseDto User { get; set; } = null!;
}

public class LoginResponseMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, LoginResponseDto>()
            .Map(dest => dest.User.Email, src => src.Email)
            .Map(dest => dest.User.Username, src => src.Profile.Username)
            .Map(dest => dest.User.Bio, src => src.Profile.Bio)
            .Map(dest => dest.User.Image, src => src.Profile.Image)
            .Map(dest => dest.User.Token, src => MapContext.Current.Parameters["token"])
            .Map(dest => dest.User.CsrfToken, src => MapContext.Current.Parameters["csrfToken"]);
    }
}
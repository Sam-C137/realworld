using Mapster;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Tags.Dto;

public class GetTagResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GetTagResponseMapper: IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Tag, GetTagResponseDto>();
    }
}
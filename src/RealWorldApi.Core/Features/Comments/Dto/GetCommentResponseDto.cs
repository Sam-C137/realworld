using System.Text.Json;
using RealWorldApi.Core.Features.Profiles.Dto;

namespace RealWorldApi.Core.Features.Comments.Dto;

public class GetCommentResponseDto
{
    public Guid Id { get; set; }
    public string Body { get; set; } = null!;
    public JsonDocument? BodyJson { get; set; }
    public ProfileDto Author { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
using System.Text.Json;
using Mapster;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Articles.Dto;

public class GetArticleResponseDto
{
    public GetArticleResponseDetails Article { get; set; } = null!;
}

public class GetArticleResponseDetails
{
    public string Slug { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Body { get; set; } = null!;
    public JsonDocument? BodyJson { get; set; }
    public string[] TagList { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // public AuthorDto Author { get; set; } = null!;
}

public class GetArticleResponseMapper: IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Article, GetArticleResponseDto>()
            .Map(dest => dest.Article.Slug, src => src.Slug)
            .Map(dest => dest.Article.Title, src => src.Title)
            .Map(dest => dest.Article.Description, src => src.Description)
            .Map(dest => dest.Article.Body, src => src.Body)
            .Map(dest => dest.Article.BodyJson, src => src.BodyJson)
            .Map(dest => dest.Article.TagList, src => src.ArticleTags.Select(at => at.Tag.Name).ToArray())
            .Map(dest => dest.Article.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.Article.UpdatedAt, src => src.UpdatedAt);
             //.Map(dest => dest.Author, src => src.Author);
    }
}
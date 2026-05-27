using ErrorOr;
using RealWorldApi.Core.Features.Articles.Dto;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Articles.Services;

public interface IArticlesService
{
    public Task<ErrorOr<GetArticleResponseDto>> CreateArticle(CreateArticleRequestDto request);
    public Task<ErrorOr<GetArticleResponseDto>> GetArticle(string slug);
    public Task<ErrorOr<GetArticleResponseDto>> UpdateArticle(string slug, UpdateArticleRequestDto request);
    public Task<ErrorOr<GetArticleResponseDto>> DeleteArticle(string slug);
}
using ErrorOr;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Articles.Dto;

namespace RealWorldApi.Core.Features.Articles.Services;

public interface IArticlesService
{
    public Task<ErrorOr<GetArticleResponseDto>> CreateArticle(CreateArticleRequestDto request);
    public Task<ErrorOr<GetArticleResponseDto>> GetArticle(string slug);
    public Task<ErrorOr<PaginatedResponse<GetArticleResponseDto>>> GetArticles(GetArticlesRequestDto request);
    public Task<ErrorOr<PaginatedResponse<GetArticleResponseDto>>> GetFeed(Guid userId, GetArticlesRequestDto request);
    public Task<ErrorOr<GetArticleResponseDto>> UpdateArticle(string slug, UpdateArticleRequestDto request);
    public Task<ErrorOr<object>> DeleteArticle(string slug);
    public Task<ErrorOr<GetArticleResponseDto>> FavoriteArticle(string slug);
    public Task<ErrorOr<GetArticleResponseDto>> UnfavoriteArticle(string slug);
}
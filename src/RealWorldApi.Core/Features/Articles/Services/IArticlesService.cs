using ErrorOr;
using RealWorldApi.Core.Features.Articles.Dto;
using RealWorldApi.Infrastructure.Data.Models;

namespace RealWorldApi.Core.Features.Articles.Services;

public interface IArticlesService
{
    public Task<ErrorOr<Article>> CreateArticle(CreateArticleRequestDto request);
    public Task<ErrorOr<Article>> GetArticle(string slug);
    public Task<ErrorOr<Article>> UpdateArticle(string slug, UpdateArticleRequestDto request);
    public Task<ErrorOr<Article>> DeleteArticle(string slug);
}
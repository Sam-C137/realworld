using ErrorOr;
using RealWorldApi.Core.Abstractions;
using RealWorldApi.Core.Features.Comments.Dto;

namespace RealWorldApi.Core.Features.Comments.Services;

public interface ICommentsService
{
    public Task<ErrorOr<GetCommentResponseDto>> GetComment(Guid commentId);
    public Task<ErrorOr<CursorPaginatedResponse<GetCommentResponseDto>>> GetComments(string slug, GetCommentsRequestDto request);
    public Task<ErrorOr<GetCommentResponseDto>> CreateComment(string slug, CreateCommentRequestDto request);
    public Task<ErrorOr<object>> DeleteComment(Guid commentId);
}
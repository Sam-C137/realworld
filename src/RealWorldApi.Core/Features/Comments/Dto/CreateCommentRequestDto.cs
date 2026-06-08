using FluentValidation;
using RealWorldApi.Core.Features.Articles.Dto;

namespace RealWorldApi.Core.Features.Comments.Dto;

public class CreateCommentRequestDto
{
    public CreateCommentRequestDetails Comment { get; set; } = null!;
}

public record CreateCommentRequestDetails(string Body, string? BodyJson);

public class CreateCommentRequestValidator: AbstractValidator<CreateCommentRequestDto>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Comment)
            .NotNull()
            .WithMessage("Comment is required.");
        
        RuleFor(x => x.Comment.Body)
            .NotEmpty().WithMessage("Body is required.")
            .MinimumLength(3).WithMessage("Body must be at least 3 characters long.")
            .MaximumLength(1024).WithMessage("Body must be at most 1024 characters long.");
        
        RuleFor(x => x.Comment.BodyJson)
            .Must(CreateArticleRequestValidator.BeValidJsonOrNull)
            .WithMessage("BodyJson must be a valid JSON string.");
    }
}
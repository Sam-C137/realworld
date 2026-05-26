using FluentValidation;

namespace RealWorldApi.Core.Features.Articles.Dto;

public class UpdateArticleRequestDto
{
    public UpdateArticleRequestDetails Article { get; set; } = null!;
}

public record UpdateArticleRequestDetails(
    string? Title, 
    string? Description, 
    string? Body,
    string[]? TagList,
    string? BodyJson
);

public class UpdateArticleRequestValidator : AbstractValidator<UpdateArticleRequestDto>
{
    public UpdateArticleRequestValidator()
    {
        RuleFor(x => x.Article)
            .NotNull()
            .WithMessage("Article is required.");
        
        RuleFor(x => x.Article.Title)
            .MinimumLength(3).WithMessage("Title must be at least 3 characters long.")
            .MaximumLength(255).WithMessage("Title must be at most 255 characters long.");
        
        RuleFor(x => x.Article.Description)
            .MinimumLength(5).WithMessage("Description must be at least 5 characters long.")
            .MaximumLength(510).WithMessage("Description must be at most 510 characters long.");
        
        RuleFor(x => x.Article.TagList)
            .Cascade(CascadeMode.Stop)
            .Must(tags => tags is null || tags.Length <= 10)
            .WithMessage("TagList must contain at most 10 tags.")
            .Must(CreateArticleRequestValidator.BeUniqueList)
            .WithMessage("TagList must contain unique tags (case-insensitive).")
            .Must(tags => tags is null || tags.All(tag => !string.IsNullOrWhiteSpace(tag) && tag.Length >= 3))
            .WithMessage("TagList should not have an empty tag and all tags should be at least 3 characters.");
        
        RuleFor(x => x.Article.BodyJson)
            .Must(CreateArticleRequestValidator.BeValidJsonOrNull)
            .WithMessage("BodyJson must be a valid JSON string.");
    }
}
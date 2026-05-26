using System.Text.Json;
using FluentValidation;

namespace RealWorldApi.Core.Features.Articles.Dto;

public class CreateArticleRequestDto
{
    public CreateArticleRequestDetails Article { get; set; } = null!;
}

public record CreateArticleRequestDetails(
    string Title, 
    string Description, 
    string Body,
    string[] TagList,
    string? BodyJson
);

public class CreateArticleRequestValidator : AbstractValidator<CreateArticleRequestDto>
{
    public CreateArticleRequestValidator()
    {
        RuleFor(x => x.Article)
            .NotNull()
            .WithMessage("Article is required.");
        
        RuleFor(x => x.Article.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(3).WithMessage("Title must be at least 3 characters long.")
            .MaximumLength(255).WithMessage("Title must be at most 255 characters long.");
        
        RuleFor(x => x.Article.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MinimumLength(5).WithMessage("Description must be at least 5 characters long.")
            .MaximumLength(510).WithMessage("Description must be at most 510 characters long.");
        
        RuleFor(x => x.Article.Body)
            .NotEmpty().WithMessage("Body is required.");
        
        RuleFor(x => x.Article.TagList)
            .Must(tags => tags.Length <= 10)
            .WithMessage("TagList must contain at most 10 tags.")
            .Must(BeUniqueList)
            .WithMessage("TagList must contain unique tags (case-insensitive).")
            .Must(tags => tags.All(tag => !string.IsNullOrWhiteSpace(tag)))
            .WithMessage("TagList should not have an empty tag")
            .Must(tags =>  tags.All(tag => tag.Length >= 3))
            .WithMessage("Tag must be at least 3 characters long.")
            .Must(tags => tags.All(tag => tag.Length <= 255))
            .WithMessage("TagList should not have a tag longer than 255 characters.");
        
        RuleFor(x => x.Article.BodyJson)
            .Must(BeValidJsonOrNull)
            .WithMessage("BodyJson must be a valid JSON string.");
    }

    public static bool BeValidJsonOrNull(string? json)
    {
        if (json is null) return true;
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public static bool BeUniqueList(IEnumerable<string>? tags)
    {
        if (tags is null) return true;
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return tags.All(set.Add);
    }
}
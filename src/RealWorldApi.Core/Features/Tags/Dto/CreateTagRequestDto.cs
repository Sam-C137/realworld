using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RealWorldApi.Infrastructure.Data;

namespace RealWorldApi.Core.Features.Tags.Dto;

public class CreateTagRequestDto
{
    public CreateTagRequestDetails Tag { get; set; } = null!;
}

public record CreateTagRequestDetails(string Name);

public class CreateTagRequestValidator : AbstractValidator<CreateTagRequestDto>
{
    private AppDbContext _db;
    
    public CreateTagRequestValidator(AppDbContext db)
    {
        _db = db;
        
        RuleFor(x => x.Tag)
            .NotNull()
            .WithMessage("Tag is required.");
        
        RuleFor(x => x.Tag.Name)
            .NotEmpty()
            .WithMessage("Tag name is required.")
            .MinimumLength(3)
            .WithMessage("Tag name must be at least 3 characters long.")
            .MaximumLength(255)
            .WithMessage("Tag name must be at most 255 characters long.");
        
        RuleFor(x => x.Tag.Name)
            .MustAsync(BeUniqueCaseInsensitive)
            .WithMessage(x => $"Tag {x.Tag.Name} already exists.");
    }

    private async Task<bool> BeUniqueCaseInsensitive(string? username, CancellationToken token)
    {
        var existing = await _db
            .Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == username.ToLower(), token);
        return existing is null;
    }
}
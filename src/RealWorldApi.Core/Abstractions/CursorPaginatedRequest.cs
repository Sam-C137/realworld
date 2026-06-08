using FluentValidation;

namespace RealWorldApi.Core.Abstractions;

public class CursorPaginatedRequest
{
    public int Limit { get; set; } = 10;
    public string? Search { get; set; }
    public string? After { get; set; }
    public string? Before { get; set; }
}

public class OffsetPaginatedRequestValidator : AbstractValidator<CursorPaginatedRequest>
{
    public OffsetPaginatedRequestValidator()
    {
        RuleFor(x => x.Limit).GreaterThan(0)
            .WithMessage("Limit must be greater than 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("Limit must be less than or equal to 100.");

        RuleFor(x => x.Search).MaximumLength(100)
            .WithMessage("Search query cannot be longer than 100 characters.");

        RuleFor(x => x)
            .Cascade(CascadeMode.Stop)
            .Must(x => string.IsNullOrWhiteSpace(x.After) || string.IsNullOrWhiteSpace(x.Before))
            .WithMessage("Only one of After or Before can be set.");
    }
}

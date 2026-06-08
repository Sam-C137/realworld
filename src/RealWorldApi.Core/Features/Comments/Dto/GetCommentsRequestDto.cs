using FluentValidation;
using RealWorldApi.Core.Abstractions;

namespace RealWorldApi.Core.Features.Comments.Dto;

public class GetCommentsRequestDto: CursorPaginatedRequest;

public class GetCommentsRequestValidator : AbstractValidator<GetCommentsRequestDto>
{
    public GetCommentsRequestValidator()
    {
       Include(new OffsetPaginatedRequestValidator());
       
       RuleFor(x => x.Before)
           .Must(BeValidUuidOrNull)
           .WithMessage("Before must be a valid UUID or null.");
       
       RuleFor(x => x.After)
           .Must(BeValidUuidOrNull)
           .WithMessage("After must be a valid UUID or null.");
    }
    
    private static bool BeValidUuidOrNull(string? value)
    {
        return value is null || Guid.TryParse(value, out _);
    }
}
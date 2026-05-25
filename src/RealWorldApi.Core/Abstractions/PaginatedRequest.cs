using System.Linq.Expressions;
using FluentValidation;

namespace RealWorldApi.Core.Abstractions;

public class PaginatedRequest
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
    public string? Search { get; set; }
}

public interface ISortableRequest
{
    SortOrder Order { get; set; }
}

public class PaginatedSortableRequest : PaginatedRequest, ISortableRequest
{
    public SortOrder Order { get; set; } = SortOrder.Asc;
}

public enum SortOrder
{
    Asc,
    Desc
}

public class SortableRequestValidator<TRequest, TSortField> : AbstractValidator<TRequest>
    where TRequest : ISortableRequest
    where TSortField : struct, Enum
{
    private static readonly string SortOrderMessage =
        $"SortOrder must be one of: {string.Join(", ", Enum.GetNames<SortOrder>())}.";

    private static readonly string SortByMessage =
        $"SortBy must be one of: {string.Join(", ", Enum.GetNames<TSortField>())}.";

    public SortableRequestValidator(Expression<Func<TRequest, TSortField>> sortBySelector)
    {
        RuleFor(x => x.Order)
            .IsInEnum()
            .WithMessage(SortOrderMessage);

        RuleFor(sortBySelector)
            .IsInEnum()
            .WithMessage(SortByMessage);
    }
}

public class PaginatedRequestValidator: AbstractValidator<PaginatedRequest>
{
    public PaginatedRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0)
            .WithMessage("Page number must be greater than 0.");
        
        RuleFor(x => x.Limit).Must(limit => limit is -1 or > 0)
            .WithMessage("Limit must be greater than 0 or equal to -1 (fetch all).")
            .Must(limit => limit is -1 or <= 100)
            .WithMessage("Limit must be less than or equal to 100 or -1 for all results.");
        
        RuleFor(x => x.Search).MaximumLength(100)
            .WithMessage("Search query cannot be longer than 100 characters.");
    }
}
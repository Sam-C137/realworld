namespace RealWorldApi.Core.Abstractions;

public class PaginatedResponse<T>
{
    public required List<T> Data { get; init; }
    public required int Total { get; init; }
}

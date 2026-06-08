namespace RealWorldApi.Core.Abstractions;

public class CursorPaginatedResponse<T>
{
    public required List<T> Data { get; set; }
    public string? Next { get; set; }
    public string? Previous { get; set; }
}

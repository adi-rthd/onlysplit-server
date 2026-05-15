namespace OnlySplit.Shared.Responses;

public sealed record ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    public static ApiResponse<T> Ok(T? data, string message = "Request completed successfully.") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors?.ToArray() ?? Array.Empty<string>() };
}

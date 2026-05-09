namespace BengalTex.ERP.Shared.Common;

/// <summary>
/// Standardized API response envelope (generic).
/// All controller actions return this shape so the Angular HTTP interceptor
/// can rely on a uniform success/error contract.
/// </summary>
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public List<ApiError> Errors { get; init; } = new();
    public string? TraceId { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, List<ApiError>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new() };
}

/// <summary>
/// Non-generic response for endpoints that return only a status (no payload).
/// </summary>
public record ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public List<ApiError> Errors { get; init; } = new();
    public string? TraceId { get; init; }

    public static ApiResponse Ok(string? message = null) =>
        new() { Success = true, Message = message };

    public static ApiResponse Fail(string message, List<ApiError>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new() };
}

public record ApiError
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Field { get; init; }
}
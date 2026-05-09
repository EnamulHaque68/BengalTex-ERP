namespace BengalTex.ERP.Domain.Common;

/// <summary>
/// Result pattern for use cases. Avoids throwing exceptions for expected failures.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public string? ErrorCode { get; }
    public List<ValidationError> ValidationErrors { get; }

    protected Result(bool isSuccess, string? error, string? errorCode, List<ValidationError>? validationErrors)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
        ValidationErrors = validationErrors ?? new List<ValidationError>();
    }

    public static Result Success() => new(true, null, null, null);
    public static Result Failure(string error, string? errorCode = null) => new(false, error, errorCode, null);
    public static Result ValidationFailure(List<ValidationError> errors) =>
        new(false, "Validation failed.", "VALIDATION_ERROR", errors);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error, string? errorCode = null) => Result<T>.Failure(error, errorCode);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T? value, bool isSuccess, string? error, string? errorCode, List<ValidationError>? validationErrors)
        : base(isSuccess, error, errorCode, validationErrors)
    {
        Value = value;
    }

    public new static Result<T> Success(T value) => new(value, true, null, null, null);
    public new static Result<T> Failure(string error, string? errorCode = null) =>
        new(default, false, error, errorCode, null);
    public new static Result<T> ValidationFailure(List<ValidationError> errors) =>
        new(default, false, "Validation failed.", "VALIDATION_ERROR", errors);
}

public record ValidationError(string Field, string Message, string? Code = null);
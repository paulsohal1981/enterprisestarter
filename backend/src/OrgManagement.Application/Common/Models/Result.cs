namespace OrgManagement.Application.Common.Models;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public string[]? Errors { get; }

    protected Result(bool isSuccess, string? error, string[]? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors;
    }

    public static Result Success() => new(true, null);

    public static Result Failure(string error) => new(false, error);

    public static Result Failure(string[] errors) => new(false, errors.FirstOrDefault(), errors);

    public static Result<T> Success<T>(T value) => new(value, true, null);

    public static Result<T> Failure<T>(string error) => new(default, false, error);

    public static Result<T> Failure<T>(string[] errors) => new(default, false, errors.FirstOrDefault(), errors);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected internal Result(T? value, bool isSuccess, string? error, string[]? errors = null)
        : base(isSuccess, error, errors)
    {
        Value = value;
    }
}

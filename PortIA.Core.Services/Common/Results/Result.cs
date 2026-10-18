namespace PortIA.Core.Services.Common.Results;

public enum ResultStatus
{
    Success,
    ValidationError,
    NotFound,
    BusinessError
}

public class Result
{
    public bool IsSuccess { get; }
    public ResultStatus Status { get; }
    public string? Error { get; }
    public IDictionary<string, string[]>? ValidationErrors { get; }

    protected Result(ResultStatus status, string? error, IDictionary<string, string[]>? validationErrors)
    {
        IsSuccess = status == ResultStatus.Success;
        Status = status;
        Error = error;
        ValidationErrors = validationErrors;
    }

    public static Result Success() => new(ResultStatus.Success, null, null);

    public static Result NotFound(string message) => new(ResultStatus.NotFound, message, null);

    public static Result BusinessError(string message) => new(ResultStatus.BusinessError, message, null);

    public static Result ValidationError(IDictionary<string, string[]> errors) =>
        new(ResultStatus.ValidationError, null, errors);
}

public class Result<T>
{
    private readonly T? _value;

    public bool IsSuccess { get; }
    public ResultStatus Status { get; }
    public string? Error { get; }
    public IDictionary<string, string[]>? ValidationErrors { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("No se puede acceder a Value cuando el resultado no es exitoso.");

    private Result(T value)
    {
        IsSuccess = true;
        Status = ResultStatus.Success;
        _value = value;
    }

    private Result(ResultStatus status, string? error, IDictionary<string, string[]>? validationErrors)
    {
        IsSuccess = false;
        Status = status;
        Error = error;
        ValidationErrors = validationErrors;
    }

    public static Result<T> Success(T value) => new(value);

    public static Result<T> NotFound(string message) => new(ResultStatus.NotFound, message, null);

    public static Result<T> BusinessError(string message) => new(ResultStatus.BusinessError, message, null);

    public static Result<T> ValidationError(IDictionary<string, string[]> errors) =>
        new(ResultStatus.ValidationError, null, errors);
}

namespace STLViewer.Domain.Common;

/// <summary>
/// Represents the result of an operation that may succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public class Result<T>
{
    private readonly T? _value;
    private readonly string _error;

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value returned by the operation (only valid if IsSuccess is true).
    /// </summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access value of a failed result");

    /// <summary>
    /// Gets the error message (only valid if IsFailure is true).
    /// </summary>
    public string Error => IsFailure ? _error : throw new InvalidOperationException("Cannot access error of a successful result");

    private Result(T value)
    {
        _value = value;
        _error = string.Empty;
        IsSuccess = true;
    }

    private Result(string error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Ok(T value)
    {
        return new Result<T>(value);
    }

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Fail(string error)
    {
        return new Result<T>(error);
    }

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A successful result containing the value.</returns>
    public static implicit operator Result<T>(T value)
    {
        return Ok(value);
    }

    /// <summary>
    /// Maps the value of a successful result to a new type.
    /// </summary>
    /// <typeparam name="TOut">The type to map to.</typeparam>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new result with the mapped value, or the original error if failed.</returns>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        return IsSuccess ? Result<TOut>.Ok(mapper(Value)) : Result<TOut>.Fail(Error);
    }

    /// <summary>
    /// Binds the result to another operation that returns a result.
    /// </summary>
    /// <typeparam name="TOut">The type returned by the bound operation.</typeparam>
    /// <param name="binder">The binding function.</param>
    /// <returns>The result of the bound operation, or the original error if failed.</returns>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
    {
        return IsSuccess ? binder(Value) : Result<TOut>.Fail(Error);
    }

    public override string ToString()
    {
        return IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";
    }
}

/// <summary>
/// Represents the result of an operation that may succeed or fail without returning a value.
/// </summary>
public class Result
{
    private readonly string _error;

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message (only valid if IsFailure is true).
    /// </summary>
    public string Error => IsFailure ? _error : throw new InvalidOperationException("Cannot access error of a successful result");

    private Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Ok()
    {
        return new Result(true, string.Empty);
    }

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result Fail(string error)
    {
        return new Result(false, error);
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Ok<T>(T value)
    {
        return Result<T>.Ok(value);
    }

    public override string ToString()
    {
        return IsSuccess ? "Success" : $"Failure: {Error}";
    }
}

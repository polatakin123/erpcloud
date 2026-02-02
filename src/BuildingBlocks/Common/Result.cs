namespace ErpCloud.BuildingBlocks.Common;

/// <summary>
/// Represents the result of an operation with success/failure state and optional value/error.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
public class Result<TValue>
{
    private readonly TValue? _value;

    protected Result(TValue? value, bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Success result cannot have an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Failure result must have an error.");
        }

        _value = value;
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");

    public static Result<TValue> Success(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure(Error error) => new(default, false, error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);
    public static implicit operator Result<TValue>(Error error) => Failure(error);
}

/// <summary>
/// Represents the result of an operation without a value.
/// </summary>
public class Result : Result<object>
{
    protected Result(bool isSuccess, Error error) : base(null, isSuccess, error)
    {
    }

    public static Result Success() => new(true, Error.None);
    public static new Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

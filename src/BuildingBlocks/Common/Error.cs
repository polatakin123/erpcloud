namespace ErpCloud.BuildingBlocks.Common;

/// <summary>
/// Represents an error with a code and message.
/// </summary>
public sealed record Error
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public string Code { get; }
    public string Message { get; }

    public static implicit operator string(Error error) => error.Code;

    // Convenience factory methods
    public static Error NotFound(string code, string message) => new(code, message);
    public static Error Conflict(string code, string message) => new(code, message);
    public static Error Validation(string code, string message) => new(code, message);
    public static Error Unexpected(string code, string message) => new(code, message);
}

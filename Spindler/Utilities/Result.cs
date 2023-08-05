namespace Spindler.Utilities;

public abstract record Result<T>
{
    /// <summary>
    /// A class containing a successful Result
    /// </summary>
    public sealed record Ok(T Value) : Result<T> { };
    /// <summary>
    /// A Class containing an unsuccessful Result
    /// </summary>
    /// <param name="Message">The Result's Error Message</param>
    public sealed record Err(string Message) : Result<T> { };

    public static Result<T> Success(T value) => new Ok(value);
    public static Result<T> Error(string message) => new Err(message);
}
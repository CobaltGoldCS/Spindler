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

    public void HandleError(Action<Err> handler)
    {
        if (this is Ok)
        {
            return;
        }
        handler((Err)this);
    }

    public void HandleSuccess(Action<T> handler)
    {
        if (this is Err)
        {
            return;
        }
        handler(((Ok)this).Value);
    }
}

public static class Result
{
    public static Result<T> Success<T>(T value) => new Result<T>.Ok(value);
    public static Result<T> Error<T>(string message) => new Result<T>.Err(message);
}
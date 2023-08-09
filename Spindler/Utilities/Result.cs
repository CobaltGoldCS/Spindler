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
}

<<<<<<< HEAD
=======
    public static Result<T> Success(T value) => new Ok(value);
    public static Result<T> Error(string message) => new Err(message);
}

>>>>>>> d7e480da4153f13fb55eec4494b1922afe9aa416
public static class Result
{
    public static Result<T> Success<T>(T value) => new Result<T>.Ok(value);
    public static Result<T> Error<T>(string message) => new Result<T>.Err(message);
}
namespace Spindler.Utils;

public class Result<T, U>
{
    public class Error : Result<T, U>
    {
        public U message;
        public Error(U message)
        {
            this.message = message;
        }
    }
    public class Ok : Result<T, U>
    {
        public T value;
        public Ok(T value)
        {
            this.value = value;
        }
    }
    /// <summary>
    /// Will attempt to cast Result to Ok value
    /// </summary>
    /// <returns>Result.Ok or fails with default typecast exception</returns>
    public Ok AsOk() => (Ok)this;
    /// <summary>
    /// Will attempt to cast Result to Error value
    /// </summary>
    /// <returns>Result.Error or fails with default typecast exception</returns>
    public Error AsError() => (Error)this;

}
public static class Result
{
    public static bool IsOk<T, U>(Result<T,U> obj) => obj is Result<T,U>.Ok;
    public static bool IsError<T, U>(Result<T, U> obj) => obj is Result<T, U>.Error;
}

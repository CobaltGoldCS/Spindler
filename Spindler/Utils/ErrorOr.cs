namespace Spindler.Utils;

public class ErrorOr<T>
{
    public class Error : ErrorOr<T>
    {
        public string message;
        public Error(string message)
        {
            this.message = message;
        }
    }
    public class Ok : ErrorOr<T>
    {
        public T value;
        public Ok(T value)
        {
            this.value = value;
        }
    }
    /// <summary>
    /// Will attempt to cast ErrorOr to Ok value
    /// </summary>
    /// <returns>ErrorOr.Ok or fails with default typecast exception</returns>
    public Ok AsOk() => (Ok)this;
    /// <summary>
    /// Will attempt to cast ErrorOr to Error value
    /// </summary>
    /// <returns>ErrorOr.Error or fails with default typecast exception</returns>
    public Error AsError() => (Error)this;
}
public class ErrorOr
{
    public static bool IsOk<U>(ErrorOr<U> obj) => obj is ErrorOr<U>.Ok;
    public static bool IsError<U>(ErrorOr<U> obj) => obj is ErrorOr<U>.Error;
}

namespace Spindler.Utilities;

public class Result<Value, ErrorType>
{
    /// <summary>
    /// A Class Containing error information of a Result
    /// </summary>
    public class Error : Result<Value, ErrorType>, IValueContainer<ErrorType>
    {
        /// <summary>
        /// The Error Value
        /// </summary>
        public ErrorType Value { get; set; }
        public Error(ErrorType message)
        {
            Value = message;
        }

    }
    /// <summary>
    /// A class containing a successful Result
    /// </summary>
    public class Ok : Result<Value, ErrorType>, IValueContainer<Value>
    {
        /// <summary>
        /// The Ok Value
        /// </summary>
        public Value Value { get; set; }
        public Ok(Value value)
        {
            Value = value;
        }
    }
    /// <summary>
    /// Will attempt to cast Result to Ok value
    /// </summary>
    /// <exception cref="InvalidCastException"/>
    /// <returns>Result.Ok or fails with default typecast exception</returns>
    public Ok AsOk() => (Ok)this;
    /// <summary>
    /// Will attempt to cast Result to Error value
    /// </summary>
    /// <exception cref="InvalidCastException"/>
    /// <returns>Result.Error or fails with default typecast exception</returns>
    public Error AsError() => (Error)this;

}
public static class Result
{
    /// <summary>
    /// Checks if the Result is a <see cref="Result{T, U}.Ok"/>
    /// </summary>
    /// <typeparam name="T">The Ok Type</typeparam>
    /// <typeparam name="U">The Error Type</typeparam>
    /// <param name="obj">The object to test</param>
    /// <returns>If the obj is OK or not</returns>
    public static bool IsOk<T, U>(Result<T, U> obj) => obj is Result<T, U>.Ok;
    /// <summary>
    /// Checks if the Result is a <see cref="Result{T, U}.Error"/>
    /// </summary>
    /// <typeparam name="T">The Ok Type</typeparam>
    /// <typeparam name="U">The Error Type</typeparam>
    /// <param name="obj">The object to test</param>
    /// <returns>If the obj is an Error or not</returns>
    public static bool IsError<T, U>(Result<T, U> obj) => obj is Result<T, U>.Error;
}


public interface IValueContainer<T>
{
    T Value { get; protected set; }
}
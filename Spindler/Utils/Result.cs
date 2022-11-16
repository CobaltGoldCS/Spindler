namespace Spindler.Utils;

public class Result<T, U>
{
    /// <summary>
    /// A Class Containing error information of a Result
    /// </summary>
    public class Error : Result<T, U>
    {
        /// <summary>
        /// The Error Value
        /// </summary>
        public U value;
        public Error(U message)
        {
            this.value = message;
        }
    }
    /// <summary>
    /// A class containing a successful Result
    /// </summary>
    public class Ok : Result<T, U>
    {
        /// <summary>
        /// The Ok Value
        /// </summary>
        public T value;
        public Ok(T value)
        {
            this.value = value;
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
    /// Checks if the Result is a <code>Result.Ok</code>
    /// </summary>
    /// <typeparam name="T">The Ok Type</typeparam>
    /// <typeparam name="U">The Error Type</typeparam>
    /// <param name="obj">The object to test</param>
    /// <returns>If the obj is OK or not</returns>
    public static bool IsOk<T, U>(Result<T, U> obj) => obj is Result<T, U>.Ok;
    /// <summary>
    /// Checks if the Result is a <code>Result.Error</code>
    /// </summary>
    /// <typeparam name="T">The Ok Type</typeparam>
    /// <typeparam name="U">The Error Type</typeparam>
    /// <param name="obj">The object to test</param>
    /// <returns>If the obj is an Error or not</returns>
    public static bool IsError<T, U>(Result<T, U> obj) => obj is Result<T, U>.Error;
}

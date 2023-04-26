using System.Reflection.Metadata;

namespace Spindler.Utilities;


public interface IResult<Value>
{
}
public interface IValueContainer<T>
{
    T Value { get; protected set; }
}

public sealed record Invalid<ExpectedType>(Error value) : IResult<ExpectedType>, IValueContainer<Error>
{
    /// <summary>
    /// The Error Value
    /// </summary>
    public Error Value { get; set; } = value;

}
/// <summary>
/// A class containing a successful Result
/// </summary>
public sealed record Ok<ValueType>(ValueType value) : IResult<ValueType>, IValueContainer<ValueType>
{
    /// <summary>
    /// The Ok Value
    /// </summary>
    public ValueType Value { get; set; } = value;
}


public record Error
{
    private string Message;
    public string getMessage() => Message;

    public Error(string message)
    {
        Message = message;
    }
}
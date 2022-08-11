namespace Spindler.Utils
{
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

        public Ok AsOk() => this as Ok;
        public Error AsError() => this as Error;
    }
    public class ErrorOr
    {
        public static bool IsOk<U>(ErrorOr<U> obj) => obj is ErrorOr<U>.Ok;
        public static bool IsError<U>(ref ErrorOr<U> obj) => obj is ErrorOr<U>.Error;
    }
}

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
        public class Success : ErrorOr<T>
        {
            public T value;
            public Success(T value)
            {
                this.value = value;
            }
        }
        private ErrorOr()
        {
        }
    }
}

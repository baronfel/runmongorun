namespace Migrator
{
    public class Result<T>
    {
        public string Error { get; }
        public bool HasError { get; }
        public T Value { get; }

        private Result(T item)
        {
            Value = item;
            HasError = false;
        }

        private Result(string error)
        {
            Error = error;
            HasError = true;
        }

        public static Result<T> Pass(T value) => new Result<T>(value);
        public static Result<T> Fail(string error) => new Result<T>(error);
    }
}

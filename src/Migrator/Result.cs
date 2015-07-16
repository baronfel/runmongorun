using System;
using System.Threading.Tasks;

namespace Migrator
{
    public class Result<T>
    {
        public Exception Error { get; }
        public bool HasError { get; }
        public T Value { get; }

        private Result(T item)
        {
            Value = item;
            HasError = false;
        }

        private Result(Exception error, T value)
        {
            Error = error;
            HasError = true;
            Value = value;
        }

        public static Result<T> Pass(T value) => new Result<T>(value);
        public static Result<T> Fail(T value, Exception error) => new Result<T>(error, value);

        public static Result<T> Lift(Func<T> f, Func<T> defaulter)
        {
            try
            {
                return Pass(f());
            }
            catch (Exception ex)
            {
                return Fail(defaulter(), ex);
            }
        }

        public static async Task<Result<T>> LiftAsync(Func<Task<T>> f, Func<T> defaulter)
        {
            try
            {
                return Pass(await f());
            }
            catch (Exception ex)
            {
                return Fail(defaulter(), ex);
            }
        }
    }
}

namespace GGNet.Transformations
{
    public class Identity<T> : ITransformation<T>
    {
        public T Apply(T value) => value;

        public T Inverse(T value) => value;

        public static Identity<T> Instance = new Identity<T>();
    }
}

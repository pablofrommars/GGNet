namespace GGNet.Formats
{
    public class Standard<T> : IFormatter<T>
    {
        public string Format(T value) => value.ToString();

        public static Standard<T> Instance => new Standard<T>();
    }
}

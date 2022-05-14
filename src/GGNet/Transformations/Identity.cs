namespace GGNet.Transformations;

public sealed class Identity<T> : ITransformation<T>
{
	public static readonly Identity<T> Instance = new();

	public T Apply(T value) => value;

	public T Inverse(T value) => value;
}
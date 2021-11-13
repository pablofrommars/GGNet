namespace GGNet.Transformations;

public interface ITransformation<T>
{
	T Apply(T value);

	T Inverse(T value);
}
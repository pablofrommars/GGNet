namespace GGNet.Formats;

public interface IFormatter<T>
{
	string Format(T value);
}
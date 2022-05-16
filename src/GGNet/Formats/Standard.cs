namespace GGNet.Formats;

public sealed class Standard<T> : IFormatter<T>
	where T : notnull
{
	public static Standard<T> Instance => new();

	public string Format(T value) => value.ToString()!;
}
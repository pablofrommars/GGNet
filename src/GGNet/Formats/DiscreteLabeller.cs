namespace GGNet.Formats;

public sealed class DiscreteLabeller<T>(IDictionary<T, string>? labels = null) : IFormatter<T>
	where T : notnull
{
	private readonly IDictionary<T, string> labels = labels ?? new Dictionary<T, string>();

	public string this[T key]
	{
		get => labels[key];
		set => labels[key] = value;
	}

	public string Format(T value) => labels[value];
}

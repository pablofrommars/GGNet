namespace GGNet.Scales;

public interface IAestheticMapping
{
	bool Guide { get; }

	string? Name { get; }

	Guide Type { get; }
}

public interface IAestheticMapping<T, TValue> : IAestheticMapping
{
	void Train(T item);

	TValue Map(T item);

	IEnumerable<(TValue value, string label)> Labels { get; }
}
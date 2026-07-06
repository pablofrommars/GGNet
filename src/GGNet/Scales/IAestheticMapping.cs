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

	// Nullable by contract: discrete scales map unmatched keys to their na
	// value, which is null unless the palette provides one.
	TValue? Map(T item);

	IEnumerable<(TValue value, string label)> Labels { get; }
}

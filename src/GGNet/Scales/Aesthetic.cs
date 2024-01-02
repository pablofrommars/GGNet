namespace GGNet.Scales;

public sealed class Aesthetic<T, TKey, TValue>(
	Func<T, TKey> selector,
	Scale<TKey, TValue> scale,
	bool guide,
	string? name
) : IAestheticMapping<T, TValue>
{
	private readonly Func<T, TKey> selector = selector;
	private readonly Scale<TKey, TValue> scale = scale;

	public bool Guide { get; } = guide;

	public string? Name { get; } = name;

	public Guide Type => scale.Guide;

	public void Train(T item) => scale.Train(selector(item));

	public TValue Map(T item) => scale.Map(selector(item));

	public IEnumerable<(TValue value, string label)> Labels => scale.Labels;
}
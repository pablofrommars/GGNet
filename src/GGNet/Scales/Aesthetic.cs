using GGNet.Common;

namespace GGNet.Scales;

public sealed class Aesthetic<T, TKey, TValue> : IAestheticMapping<T, TValue>
{
	private readonly Func<T, TKey> selector;
	private readonly Scale<TKey, TValue> scale;

	public Aesthetic(Func<T, TKey> selector, Scale<TKey, TValue> scale, bool guide, string? name)
	{
		this.selector = selector;
		this.scale = scale;

		Guide = guide;
		Name = name;
	}

	public bool Guide { get; }

	public string? Name { get; }

	public Guide Type => scale.Guide;

	public void Train(T item) => scale.Train(selector(item));

	public TValue Map(T item) => scale.Map(selector(item));

	public IEnumerable<(TValue value, string label)> Labels => scale.Labels;
}
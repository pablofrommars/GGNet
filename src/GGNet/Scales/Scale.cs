using GGNet.Transformations;

namespace GGNet.Scales;

public abstract class Scale<TKey, TValue>(ITransformation<TKey>? transformation = null) : IScale
{
	protected readonly ITransformation<TKey> transformation = transformation ?? Transformations.Identity<TKey>.Instance;

    public abstract Guide Guide { get; }

    public IEnumerable<TValue> Breaks { get; protected set; } = Enumerable.Empty<TValue>();

    public IEnumerable<TValue> MinorBreaks { get; protected set; } = Enumerable.Empty<TValue>();

    public IEnumerable<(TValue value, string label)> Labels { get; protected set; } = Enumerable.Empty<(TValue value, string text)>();

    public IEnumerable<(TValue value, string title)> Titles { get; protected set; } = Enumerable.Empty<(TValue value, string text)>();

    public abstract void Train(TKey key);

	public abstract void Set(bool grid);

	public abstract TValue Map(TKey key);

	public abstract void Clear();
}
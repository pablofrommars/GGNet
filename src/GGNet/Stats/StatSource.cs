namespace GGNet;

public sealed class StatSource<T> : IReadOnlyList<T>, IStatSource, IStatDependency
{
	private readonly Func<List<T>> recompute;

	private List<T> items;

	internal StatSource(Func<List<T>> compute) : this(null, compute)
	{
	}

	internal StatSource(IStatSource? upstream, Func<List<T>> compute)
	{
		Upstream = upstream;
		recompute = compute;

		items = compute();
	}

	private IStatSource? Upstream { get; }

	IStatSource? IStatDependency.Upstream => Upstream;

	public int Count => items.Count;

	public T this[int i] => items[i];

	public void Recompute() => items = recompute();

	public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

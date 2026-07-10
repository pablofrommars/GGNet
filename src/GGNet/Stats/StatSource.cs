namespace GGNet;

public sealed class StatSource<T> : IReadOnlyList<T>, IStatSource
{
	private readonly Func<List<T>> compute;

	private List<T> items;

	internal StatSource(Func<List<T>> compute)
	{
		this.compute = compute;

		items = compute();
	}

	public int Count => items.Count;

	public T this[int i] => items[i];

	public void Recompute() => items = compute();

	public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

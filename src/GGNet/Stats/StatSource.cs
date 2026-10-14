namespace GGNet;

public sealed class StatSource<T> : IReadOnlyList<T>, IStatSource
{
	private readonly Func<List<T>> recompute;

	private List<T> items;

	internal StatSource(Func<List<T>> compute)
	{
		recompute = compute;

		items = compute();
	}

	public int Count => items.Count;

	public T this[int i] => items[i];

	public void Recompute() => items = recompute();

	public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

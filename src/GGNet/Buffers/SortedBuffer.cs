namespace GGNet.Buffers;

internal sealed class SortedBuffer<T>(IComparer<T>? comparer = null)
{
	private readonly RankedTree<T> items = new(comparer);

	public int Count => items.Count;

	public T this[int i]
	{
		get => items[i];
		set => items[i] = value;
	}

	public void Add(T item) => items.Add(item, duplicates: false);

	public void Add(IEnumerable<T> items)
	{
		foreach (var item in items)
		{
			Add(item);
		}
	}

	public int IndexOf(T item) => items.IndexOf(item);

	public void Clear() => items.Clear();
}

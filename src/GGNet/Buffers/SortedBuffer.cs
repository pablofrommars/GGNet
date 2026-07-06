namespace GGNet.Buffers;

// Sorted, distinct-by-comparer, index-addressable. BCL-backed: binary-search
// insert into a List<T> — the BCL offers no single type combining sorted,
// distinct and indexed. Items comparing equal to an existing element are
// dropped (first one wins).
internal sealed class SortedBuffer<T>(IComparer<T>? comparer = null)
{
	private readonly IComparer<T> comparer = comparer ?? Comparer<T>.Default;
	private readonly List<T> items = [];

	public int Count => items.Count;

	// The setter is for in-place updates that preserve the sort key (Area's
	// stacking rewrites y at a fixed x); replacing the key is the caller's bug.
	public T this[int i]
	{
		get => items[i];
		set => items[i] = value;
	}

	public void Add(T item)
	{
		var index = items.BinarySearch(item, comparer);

		if (index < 0)
		{
			items.Insert(~index, item);
		}
	}

	public void Add(IEnumerable<T> items)
	{
		foreach (var item in items)
		{
			Add(item);
		}
	}

	public int IndexOf(T item)
	{
		var index = items.BinarySearch(item, comparer);

		return index < 0 ? -1 : index;
	}

	public void Clear() => items.Clear();
}

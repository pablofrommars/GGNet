namespace GGNet.Buffers;

internal sealed class SortedBuffer<T>(IComparer<T>? comparer = null)
{
	private readonly IComparer<T> comparer = comparer ?? Comparer<T>.Default;
	private readonly List<T> items = [];

	public int Count => items.Count;

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

namespace GGNet.Buffers;

internal sealed class SampleBuffer<T>(IComparer<T>? comparer = null)
{
	private readonly IComparer<T> comparer = comparer ?? Comparer<T>.Default;
	private readonly List<T> items = [];

	public int Count => items.Count;

	public T this[int i] => items[i];

	public void Add(T item)
	{
		var index = items.BinarySearch(item, comparer);

		items.Insert(index < 0 ? ~index : index, item);
	}

	public void Clear() => items.Clear();
}

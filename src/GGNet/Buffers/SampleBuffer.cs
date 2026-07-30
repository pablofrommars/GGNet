namespace GGNet.Buffers;

// Sorted like SortedBuffer, but multiplicity-preserving: statistics over observations need
// [1, 1, 1, 10] to stay four samples. SortedBuffer's set-like dedup is right for its scale and
// facet consumers and stays as it is.
internal sealed class SampleBuffer<T>(IComparer<T>? comparer = null)
{
	private readonly IComparer<T> comparer = comparer ?? Comparer<T>.Default;
	private readonly List<T> items = [];

	public int Count => items.Count;

	public T this[int i] => items[i];

	public void Add(T item)
	{
		var index = items.BinarySearch(item, comparer);

		// A hit is an equal element, not the same element: inserting at that index keeps the
		// list sorted and keeps both copies.
		items.Insert(index < 0 ? ~index : index, item);
	}

	public void Clear() => items.Clear();
}

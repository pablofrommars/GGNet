namespace GGNet.Buffers;

internal sealed class SampleBuffer<T>(IComparer<T>? comparer = null)
{
	private readonly RankedTree<T> items = new(comparer);

	public int Count => items.Count;

	public T this[int i] => items[i];

	public void Add(T item) => items.Add(item, duplicates: true);

	public void Clear() => items.Clear();
}

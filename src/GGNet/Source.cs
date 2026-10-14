namespace GGNet;

public sealed class Source<T> : IReadOnlyList<T>
{
	private readonly List<T> list = [];

	public Source()
	{
	}

	public Source(IEnumerable<T> items)
	{
		list.AddRange(items);
	}

	public int Count => list.Count;

	public T this[int i]
	{
		get => list[i];
		set => list[i] = value;
	}

	public void Add(T item) => list.Add(item);

	public void Add(IEnumerable<T> items) => list.AddRange(items);

	public void Clear() => list.Clear();

	public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

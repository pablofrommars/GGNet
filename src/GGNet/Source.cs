namespace GGNet;

public sealed class Source<T> : IReadOnlyList<T>
{
	private readonly List<T> items = [];

	public Source()
	{
	}

	public Source(IEnumerable<T> items)
	{
		this.items.AddRange(items);
	}

	public int Count => items.Count;

	public T this[int i]
	{
		get => items[i];
		set => items[i] = value;
	}

	public void Add(T item) => items.Add(item);

	public void Add(IEnumerable<T> items) => this.items.AddRange(items);

	public void Clear() => items.Clear();

	public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

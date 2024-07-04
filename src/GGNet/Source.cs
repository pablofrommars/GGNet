namespace GGNet;

using Buffers;

public sealed class Source<T> : Buffer<T>, IEnumerable<T>
{
	public Source()
		: base()
	{
	}

	public Source(IEnumerable<T> items)
		: this()
	{
		Add(items);
	}

  public void AddRange(IEnumerable<T> items)
  {
    foreach (var item in items)
    {
      Add(item);
    }
  }

  public IEnumerator<T> GetEnumerator()
  {
    for (var i = 0; i < Count; i++)
    {
      yield return Get(i);
    }
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

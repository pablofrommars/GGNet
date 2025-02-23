namespace GGNet;

using Buffers;

public sealed class Source<T> : Buffer<T>, IReadOnlyList<T>
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

  public IEnumerator<T> GetEnumerator()
  {
    for (var i = 0; i < Count; i++)
    {
      yield return Get(i);
    }
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

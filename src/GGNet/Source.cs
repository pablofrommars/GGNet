namespace GGNet;

using Buffers;

public sealed class Source<T> : Buffer<T>
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
}

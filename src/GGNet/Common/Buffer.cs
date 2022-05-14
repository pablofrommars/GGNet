namespace GGNet.Common;

public class Buffer<T> : BufferBase<T>
{
	public Buffer(int pageCapacity = 32, int pagesIncrement = 4)
		: base(pageCapacity, pagesIncrement)
	{
	}

	public override void Add(T item) => Append(item);

	public void Add(Buffer<T> buffer)
	{
		for (var i = 0; i < buffer.Count; i++)
		{
			Add(buffer[i]);
		}
	}

	public override int IndexOf(T item)
	{
		var i = 0;

		for (var p = 0; p < page; p++)
		{
			for (var j = 0; j < pageCapacity; j++)
			{
				if (comparer.Compare(pages[p][j], item) == 0)
				{
					return i;
				}

				i++;
			}
		}

		return -1;
	}
}
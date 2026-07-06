using GGNet.Buffers;

namespace GGNet.Headless.Tests;

public class SortedBufferTest
{
	[Fact]
	public void AddAscending()
	{
		var sut = new SortedBuffer<int>();

		sut.Add([1, 2, 3, 4, 5]);

		Assert.Equal(5, sut.Count);
		Assert.Equal([1, 2, 3, 4, 5], Items(sut));
	}

	[Fact]
	public void ReAddExistingDedupes()
	{
		var sut = new SortedBuffer<int>();

		// A second geom re-training a discrete scale replays the same keys in order.
		sut.Add([1, 2, 3, 4, 5]);
		sut.Add([1, 2, 3, 4, 5]);

		Assert.Equal(5, sut.Count);
		Assert.Equal([1, 2, 3, 4, 5], Items(sut));
	}

	[Fact]
	public void AddOutOfOrder()
	{
		var sut = new SortedBuffer<int>();

		sut.Add([4, 1, 5, 3, 2, 3, 1]);

		Assert.Equal(5, sut.Count);
		Assert.Equal([1, 2, 3, 4, 5], Items(sut));
	}

	[Fact]
	public void AddAcrossPages()
	{
		var sut = new SortedBuffer<int>(pageCapacity: 4, pagesIncrement: 1);

		sut.Add([2, 4, 6, 8, 10, 12, 14, 16, 18]);
		sut.Add([1, 3, 5, 7, 9, 11, 13, 15, 17, 19]);
		sut.Add([2, 4, 6, 8, 10, 12, 14, 16, 18]);

		Assert.Equal(19, sut.Count);
		Assert.Equal(Enumerable.Range(1, 19).ToArray(), Items(sut));
	}

	[Fact]
	public void IndexOf()
	{
		var sut = new SortedBuffer<int>(pageCapacity: 4, pagesIncrement: 1);

		sut.Add([1, 2, 3, 4, 5, 6, 7, 8]);

		for (var i = 0; i < 8; i++)
		{
			Assert.Equal(i, sut.IndexOf(i + 1));
		}

		Assert.Equal(-1, sut.IndexOf(9));
	}

	[Fact]
	public void IndexOfMissDoesNotCorruptState()
	{
		var sut = new SortedBuffer<int>(pageCapacity: 4, pagesIncrement: 1);

		// Fill the last page exactly, then miss: IndexOf must not advance the page cursor.
		sut.Add([1, 2, 3, 4]);

		Assert.Equal(-1, sut.IndexOf(9));

		sut.Add(5);

		Assert.Equal(5, sut.Count);
		Assert.Equal([1, 2, 3, 4, 5], Items(sut));
	}

	[Fact]
	public void AddDescending()
	{
		var sut = new SortedBuffer<int>(pageCapacity: 4, pagesIncrement: 1);

		sut.Add([9, 8, 7, 6, 5, 4, 3, 2, 1]);

		Assert.Equal(9, sut.Count);
		Assert.Equal(Enumerable.Range(1, 9).ToArray(), Items(sut));
	}

	[Fact]
	public void CustomComparerDedupesOnCompareEqual()
	{
		// Shapes.Path sorts points by x only: a second point with the same x is dropped.
		var sut = new SortedBuffer<(double x, double y)>(comparer: Comparer<(double x, double y)>.Create((a, b) => a.x.CompareTo(b.x)));

		sut.Add((1.0, 10.0));
		sut.Add((2.0, 20.0));
		sut.Add((1.0, 99.0));

		Assert.Equal(2, sut.Count);
		Assert.Equal(10.0, sut[0].y);
	}

	[Fact]
	public void ClearAndReuse()
	{
		var sut = new SortedBuffer<int>(pageCapacity: 4, pagesIncrement: 1);

		sut.Add([3, 1, 2, 5, 4]);
		sut.Clear();

		Assert.Equal(0, sut.Count);

		sut.Add([9, 7, 8]);

		Assert.Equal(3, sut.Count);
		Assert.Equal([7, 8, 9], Items(sut));
		Assert.Equal(1, sut.IndexOf(8));
	}

	private static int[] Items(SortedBuffer<int> buffer)
	{
		var items = new int[buffer.Count];

		for (var i = 0; i < buffer.Count; i++)
		{
			items[i] = buffer[i];
		}

		return items;
	}
}

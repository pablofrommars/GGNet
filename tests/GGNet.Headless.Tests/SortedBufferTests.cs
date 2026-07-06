namespace GGNet.Headless.Tests;

public class SortedBufferTests
{
	[Fact]
	public void AddAscending()
	{
		// Arrange

		var sut = new SortedBuffer<int>();

		// Act

		sut.Add([1, 2, 3, 4, 5]);

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(5);
		Items(sut).Should().Equal(1, 2, 3, 4, 5);
	}

	[Fact]
	public void ReAddExistingDedupes()
	{
		// Arrange

		var sut = new SortedBuffer<int>();

		// Act

		// A second geom re-training a discrete scale replays the same keys in order.
		sut.Add([1, 2, 3, 4, 5]);
		sut.Add([1, 2, 3, 4, 5]);

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(5);
		Items(sut).Should().Equal(1, 2, 3, 4, 5);
	}

	[Fact]
	public void AddOutOfOrder()
	{
		// Arrange

		var sut = new SortedBuffer<int>();

		// Act

		sut.Add([4, 1, 5, 3, 2, 3, 1]);

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(5);
		Items(sut).Should().Equal(1, 2, 3, 4, 5);
	}

	[Fact]
	public void AddAcrossPages()
	{
		// Arrange

		var sut = new SortedBuffer<int>(pageCapacity: 4, pagesIncrement: 1);

		// Act

		sut.Add([2, 4, 6, 8, 10, 12, 14, 16, 18]);
		sut.Add([1, 3, 5, 7, 9, 11, 13, 15, 17, 19]);
		sut.Add([2, 4, 6, 8, 10, 12, 14, 16, 18]);

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(19);
		Items(sut).Should().Equal(Enumerable.Range(1, 19));
	}

	[Fact]
	public void IndexOf()
	{
		// Arrange

		var sut = new SortedBuffer<int>(pageCapacity: 4, pagesIncrement: 1);

		// Act

		sut.Add([1, 2, 3, 4, 5, 6, 7, 8]);

		// Assert

		using var _ = new AssertionScope();

		for (var i = 0; i < 8; i++)
		{
			sut.IndexOf(i + 1).Should().Be(i);
		}

		sut.IndexOf(9).Should().Be(-1);
	}

	[Fact]
	public void IndexOfMissDoesNotCorruptState()
	{
		// Arrange

		// Fill the last page exactly, then miss: IndexOf must not advance the page cursor.
		var sut = new SortedBuffer<int>(pageCapacity: 4, pagesIncrement: 1);
		sut.Add([1, 2, 3, 4]);

		// Act

		var miss = sut.IndexOf(9);
		sut.Add(5);

		// Assert

		using var _ = new AssertionScope();

		miss.Should().Be(-1);
		sut.Count.Should().Be(5);
		Items(sut).Should().Equal(1, 2, 3, 4, 5);
	}

	[Fact]
	public void AddDescending()
	{
		// Arrange

		var sut = new SortedBuffer<int>(pageCapacity: 4, pagesIncrement: 1);

		// Act

		sut.Add([9, 8, 7, 6, 5, 4, 3, 2, 1]);

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(9);
		Items(sut).Should().Equal(Enumerable.Range(1, 9));
	}

	[Fact]
	public void CustomComparerDedupesOnCompareEqual()
	{
		// Arrange

		// Shapes.Path sorts points by x only: a second point with the same x is dropped.
		var sut = new SortedBuffer<(double x, double y)>(comparer: Comparer<(double x, double y)>.Create((a, b) => a.x.CompareTo(b.x)));

		// Act

		sut.Add((1.0, 10.0));
		sut.Add((2.0, 20.0));
		sut.Add((1.0, 99.0));

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(2);
		sut[0].y.Should().Be(10.0);
	}

	[Fact]
	public void ClearAndReuse()
	{
		// Arrange

		var sut = new SortedBuffer<int>(pageCapacity: 4, pagesIncrement: 1);
		sut.Add([3, 1, 2, 5, 4]);

		// Act

		sut.Clear();
		sut.Add([9, 7, 8]);

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(3);
		Items(sut).Should().Equal(7, 8, 9);
		sut.IndexOf(8).Should().Be(1);
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

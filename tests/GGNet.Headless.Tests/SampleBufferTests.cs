namespace GGNet.Headless.Tests;

// The multiplicity-preserving twin of SortedBuffer, used for statistical samples.
// SortedBufferTests pins the opposite (set-like) contract for the scale/facet consumers.
public class SampleBufferTests
{
	[Fact]
	public void AddKeepsDuplicates()
	{
		// Arrange

		var sut = new SampleBuffer<int>();

		// Act

		sut.Add(1);
		sut.Add(1);
		sut.Add(1);
		sut.Add(10);

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(4);
		Items(sut).Should().Equal(1, 1, 1, 10);
	}

	[Fact]
	public void AddOutOfOrderSorts()
	{
		// Arrange

		var sut = new SampleBuffer<int>();

		// Act

		sut.Add(10);
		sut.Add(1);
		sut.Add(5);
		sut.Add(1);
		sut.Add(7);

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(5);
		Items(sut).Should().Equal(1, 1, 5, 7, 10);
	}

	[Fact]
	public void AddDescendingSorts()
	{
		// Arrange

		var sut = new SampleBuffer<int>();

		// Act

		foreach (var item in new[] { 9, 8, 8, 7, 6, 6, 6, 5 })
		{
			sut.Add(item);
		}

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(8);
		Items(sut).Should().Equal(5, 6, 6, 6, 7, 8, 8, 9);
	}

	[Fact]
	public void CustomComparerKeepsCompareEqualItems()
	{
		// Arrange

		var sut = new SampleBuffer<(double x, double y)>(comparer: Comparer<(double x, double y)>.Create((a, b) => a.x.CompareTo(b.x)));

		// Act

		sut.Add((1.0, 10.0));
		sut.Add((2.0, 20.0));
		sut.Add((1.0, 99.0));

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(3);
		sut[2].x.Should().Be(2.0);
	}

	[Fact]
	public void ClearAndReuse()
	{
		// Arrange

		var sut = new SampleBuffer<int>();
		sut.Add(3);
		sut.Add(3);

		// Act

		sut.Clear();
		sut.Add(9);
		sut.Add(7);

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(2);
		Items(sut).Should().Equal(7, 9);
	}

	private static int[] Items(SampleBuffer<int> buffer)
	{
		var items = new int[buffer.Count];

		for (var i = 0; i < buffer.Count; i++)
		{
			items[i] = buffer[i];
		}

		return items;
	}
}

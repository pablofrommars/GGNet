namespace GGNet.Headless.Tests;

public class BufferTests
{
	[Fact]
	public void AddAndIndexAcrossPages()
	{
		// Arrange

		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);

		// Act

		for (var i = 0; i < 11; i++)
		{
			sut.Add(i * 10);
		}

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(11);

		for (var i = 0; i < 11; i++)
		{
			sut[i].Should().Be(i * 10);
		}
	}

	[Fact]
	public void IndexerSet()
	{
		// Arrange

		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);
		sut.Add([1, 2, 3, 4, 5, 6]);

		// Act

		sut[0] = 10;
		sut[5] = 60;

		// Assert

		using var _ = new AssertionScope();

		sut[0].Should().Be(10);
		sut[5].Should().Be(60);
	}

	[Fact]
	public void IndexOfOnPartialPage()
	{
		// Arrange

		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);

		// Act

		sut.Add([1, 2, 3]);

		// Assert

		using var _ = new AssertionScope();

		sut.IndexOf(1).Should().Be(0);
		sut.IndexOf(3).Should().Be(2);
		sut.IndexOf(4).Should().Be(-1);
	}

	[Fact]
	public void IndexOfAcrossPages()
	{
		// Arrange

		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);

		// Act

		for (var i = 0; i < 10; i++)
		{
			sut.Add(i);
		}

		// Assert

		using var _ = new AssertionScope();

		sut.IndexOf(0).Should().Be(0);
		sut.IndexOf(5).Should().Be(5);
		sut.IndexOf(9).Should().Be(9);
		sut.IndexOf(10).Should().Be(-1);
	}

	[Fact]
	public void IndexOfOnExactlyFullPage()
	{
		// Arrange

		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);

		// Act

		sut.Add([1, 2, 3, 4]);

		// Assert

		using var _ = new AssertionScope();

		sut.IndexOf(4).Should().Be(3);
		sut.IndexOf(5).Should().Be(-1);
	}

	[Fact]
	public void AddBuffer()
	{
		// Arrange

		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);
		var other = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);

		sut.Add([1, 2, 3]);
		other.Add([4, 5, 6, 7, 8]);

		// Act

		sut.Add(other);

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(8);
		sut[7].Should().Be(8);
	}

	[Fact]
	public void Clear()
	{
		// Arrange

		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);
		sut.Add([1, 2, 3, 4, 5, 6]);

		// Act

		sut.Clear();

		// Assert

		sut.Count.Should().Be(0);
	}

	[Fact]
	public void ClearAndReuse()
	{
		// Arrange

		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);
		sut.Add([1, 2, 3, 4, 5, 6]);

		// Act

		sut.Clear();
		sut.Add([7, 8]);

		// Assert

		using var _ = new AssertionScope();

		sut.Count.Should().Be(2);
		sut[0].Should().Be(7);
		sut.IndexOf(8).Should().Be(1);
	}
}

using GGNet.Buffers;

namespace GGNet.Headless.Tests;

public class BufferTest
{
	[Fact]
	public void AddAndIndexAcrossPages()
	{
		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);

		for (var i = 0; i < 11; i++)
		{
			sut.Add(i * 10);
		}

		Assert.Equal(11, sut.Count);

		for (var i = 0; i < 11; i++)
		{
			Assert.Equal(i * 10, sut[i]);
		}
	}

	[Fact]
	public void IndexerSet()
	{
		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);

		sut.Add([1, 2, 3, 4, 5, 6]);

		sut[0] = 10;
		sut[5] = 60;

		Assert.Equal(10, sut[0]);
		Assert.Equal(60, sut[5]);
	}

	[Fact]
	public void IndexOfOnPartialPage()
	{
		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);

		sut.Add([1, 2, 3]);

		Assert.Equal(0, sut.IndexOf(1));
		Assert.Equal(2, sut.IndexOf(3));
		Assert.Equal(-1, sut.IndexOf(4));
	}

	[Fact]
	public void IndexOfAcrossPages()
	{
		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);

		for (var i = 0; i < 10; i++)
		{
			sut.Add(i);
		}

		Assert.Equal(0, sut.IndexOf(0));
		Assert.Equal(5, sut.IndexOf(5));
		Assert.Equal(9, sut.IndexOf(9));
		Assert.Equal(-1, sut.IndexOf(10));
	}

	[Fact]
	public void IndexOfOnExactlyFullPage()
	{
		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);

		sut.Add([1, 2, 3, 4]);

		Assert.Equal(3, sut.IndexOf(4));
		Assert.Equal(-1, sut.IndexOf(5));
	}

	[Fact]
	public void AddBuffer()
	{
		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);
		var other = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);

		sut.Add([1, 2, 3]);
		other.Add([4, 5, 6, 7, 8]);

		sut.Add(other);

		Assert.Equal(8, sut.Count);
		Assert.Equal(8, sut[7]);
	}

	[Fact]
	public void ClearAndReuse()
	{
		var sut = new Buffer<int>(pageCapacity: 4, pagesIncrement: 1);

		sut.Add([1, 2, 3, 4, 5, 6]);
		sut.Clear();

		Assert.Equal(0, sut.Count);

		sut.Add([7, 8]);

		Assert.Equal(2, sut.Count);
		Assert.Equal(7, sut[0]);
		Assert.Equal(1, sut.IndexOf(8));
	}
}

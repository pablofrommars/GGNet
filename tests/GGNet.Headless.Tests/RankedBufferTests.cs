using System.Collections.Concurrent;

namespace GGNet.Headless.Tests;

public class RankedBufferTests
{
	private static readonly double[] specialKeys = [double.NaN, BitConverter.Int64BitsToDouble(unchecked((long)0xfff8000000000001)), -0.0, 0.0, double.PositiveInfinity, double.NegativeInfinity];

	[Theory]
	[InlineData(17, false)]
	[InlineData(431, false)]
	[InlineData(992, true)]
	[InlineData(2319, true)]
	public void MixedUniqueOperationsMatchListBinarySearch(int seed, bool reverse)
	{
		// Arrange

		var random = new Random(seed);
		var comparer = Comparer<(double Key, int Payload)>.Create((a, b) => reverse ? b.Key.CompareTo(a.Key) : a.Key.CompareTo(b.Key));
		var expected = new List<(double Key, int Payload)>();
		var actual = new SortedBuffer<(double Key, int Payload)>(comparer);
		var observations = new List<(long[] Expected, long[] Actual, int[] Ranks)>();

		// Act

		for (var step = 0; step < 500; step++)
		{
			var operation = random.Next(20);
			if (operation == 0)
			{
				expected.Clear();
				actual.Clear();
			}
			else if (operation == 1 && expected.Count > 0)
			{
				var rank = random.Next(expected.Count);
				var item = expected[rank] with { Payload = -step };
				expected[rank] = item;
				actual[rank] = item;
			}
			else
			{
				var item = (Key(random), step);
				var rank = expected.BinarySearch(item, comparer);
				if (rank < 0)
				{
					expected.Insert(~rank, item);
				}

				actual.Add(item);
			}

			observations.Add((Bits(expected), Bits(Enumerable.Range(0, actual.Count).Select(i => actual[i])), expected.Select(actual.IndexOf).ToArray()));
		}

		// Assert

		using var _ = new AssertionScope();
		foreach (var (expectedItems, actualItems, ranks) in observations)
		{
			actualItems.Should().Equal(expectedItems);
			ranks.Should().Equal(Enumerable.Range(0, expectedItems.Length / 2));
		}

		actual.IndexOf((1000.0, 0)).Should().Be(-1);
	}

	[Theory]
	[InlineData(17)]
	[InlineData(431)]
	[InlineData(992)]
	public void SamplePayloadTieOrderMatchesListBinarySearch(int seed)
	{
		// Arrange

		var random = new Random(seed);
		var comparer = Comparer<(double Key, int Payload)>.Create((a, b) => a.Key.CompareTo(b.Key));
		var expected = new List<(double Key, int Payload)>();
		var actual = new SampleBuffer<(double Key, int Payload)>(comparer);
		var observations = new List<(long[] Expected, long[] Actual)>();

		// Act

		for (var step = 0; step < 700; step++)
		{
			if (random.Next(30) == 0)
			{
				expected.Clear();
				actual.Clear();
			}

			var item = (Key(random), step);
			var rank = expected.BinarySearch(item, comparer);
			expected.Insert(rank < 0 ? ~rank : rank, item);
			actual.Add(item);
			observations.Add((Bits(expected), Bits(Enumerable.Range(0, actual.Count).Select(i => actual[i]))));
		}

		// Assert

		using var _ = new AssertionScope();
		foreach (var (expectedItems, actualItems) in observations)
		{
			actualItems.Should().Equal(expectedItems);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ComparerFailuresMatchBinarySearchWithoutChangingPopulatedBuffer(bool samples)
	{
		// Arrange

		var fail = false;
		var comparer = Comparer<int>.Create((a, b) => fail ? throw new ArgumentException("Comparison failed") : a.CompareTo(b));
		var actual = new RankedTree<int>(comparer);
		var expected = new List<int>();
		foreach (var value in new[] { 4, 2, 6, 1, 5, 3 })
		{
			actual.Add(value, samples);
			var rank = expected.BinarySearch(value, comparer);
			expected.Insert(~rank, value);
		}

		fail = true;

		// Act

		var expectedFailure = Record.Exception(() => expected.BinarySearch(0, comparer));
		var addFailure = Record.Exception(() => actual.Add(0, samples));
		var rankFailure = Record.Exception(() => actual.IndexOf(0));

		// Assert

		using var _ = new AssertionScope();
		expectedFailure.Should().BeOfType<InvalidOperationException>();
		addFailure.Should().BeOfType<InvalidOperationException>().Which.InnerException.Should().BeOfType<ArgumentException>();
		rankFailure.Should().BeOfType<InvalidOperationException>().Which.InnerException.Should().BeOfType<ArgumentException>();
		Enumerable.Range(0, actual.Count).Select(i => actual[i]).Should().Equal(expected);
	}

	[Theory]
	[InlineData(-1, false)]
	[InlineData(3, false)]
	[InlineData(int.MaxValue, false)]
	[InlineData(-1, true)]
	[InlineData(3, true)]
	[InlineData(int.MaxValue, true)]
	public void InvalidRanksKeepListExceptionCategory(int rank, bool promoted)
	{
		// Arrange

		var actual = new SortedBuffer<int>();
		actual.Add(promoted ? [3, 1, 2] : [1, 2, 3]);
		var expected = new List<int> { 1, 2, 3 };
		var middle = actual[1];

		// Act

		var expectedGet = Record.Exception(() => expected[rank]);
		var expectedSet = Record.Exception(() => expected[rank] = 4);
		var actualGet = Record.Exception(() => actual[rank]);
		var actualSet = Record.Exception(() => actual[rank] = 4);

		// Assert

		using var _ = new AssertionScope();
		middle.Should().Be(2);
		actualGet.Should().BeOfType(expectedGet!.GetType());
		actualSet.Should().BeOfType(expectedSet!.GetType());
		Enumerable.Range(0, actual.Count).Select(i => actual[i]).Should().Equal(expected);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void PromotedNeighborWalksReadAndWriteTheCorrectPayload(bool reverse)
	{
		// Arrange

		var comparer = Comparer<(int Key, int Payload)>.Create((a, b) => a.Key.CompareTo(b.Key));
		var actual = new SortedBuffer<(int Key, int Payload)>(comparer);
		var expected = Enumerable.Range(0, 16).Select(i => (Key: i, Payload: i * 10)).ToArray();
		actual.Add(expected.Reverse());
		var centers = reverse ? Enumerable.Range(2, 12).Reverse() : Enumerable.Range(2, 12);
		// Interior ranks avoid endpoint shortcuts; each case forces one distance-two direction.
		var ranks = centers.SelectMany(i => reverse ? new[] { i, i + 1, i - 1, i } : [i, i - 1, i + 1, i]).ToArray();
		int[] writeRanks = [1, 3, 5, 7, 9, 11, 13, 14, 12, 10, 8, 6, 4, 2];
		if (reverse)
		{
			Array.Reverse(writeRanks);
		}
		var reads = new List<(int Key, int Payload)>();
		var writes = 0;

		// Act

		foreach (var rank in ranks)
		{
			reads.Add(actual[rank]);
		}
		foreach (var rank in writeRanks)
		{
			var item = (rank, -++writes);
			actual[rank] = item;
			expected[rank] = item;
		}

		// Assert

		using var _ = new AssertionScope();
		reads.Should().Equal(ranks.Select(rank => (rank, rank * 10)));
		Enumerable.Range(0, actual.Count).Select(i => actual[i]).Should().Equal(expected);
		expected.Select(actual.IndexOf).Should().Equal(Enumerable.Range(0, expected.Length));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ConcurrentReadersKeepRankAndNodeConsistent(bool promoted)
	{
		// Arrange

		var actual = new SortedBuffer<int>();
		actual.Add(promoted ? Enumerable.Range(0, 2048).Reverse() : Enumerable.Range(0, 2048));
		var failures = new ConcurrentQueue<(int Rank, int Value)>();

		// Act

		Parallel.For(0, 20000, i =>
		{
			var rank = (i * 971) % actual.Count;
			var value = actual[rank];
			if (value != rank)
			{
				failures.Enqueue((rank, value));
			}
		});

		// Assert

		failures.Should().BeEmpty();
	}

	[Fact]
	public void ComparerEqualStringsKeepTheFirstRepresentative()
	{
		// Arrange

		var actual = new SortedBuffer<string>(StringComparer.OrdinalIgnoreCase);

		// Act

		actual.Add(["beta", "ALPHA", "Beta", "alpha", "gamma"]);

		// Assert

		Enumerable.Range(0, actual.Count).Select(i => actual[i]).Should().Equal("ALPHA", "beta", "gamma");
	}

	private static double Key(Random random)
	{
		var key = random.Next(16);
		return key < specialKeys.Length ? specialKeys[key] : random.Next(-30, 31);
	}

	private static long[] Bits(IEnumerable<(double Key, int Payload)> values)
		=> values.SelectMany(value => new[] { BitConverter.DoubleToInt64Bits(value.Key), (long)value.Payload }).ToArray();
}

namespace GGNet.Headless.Tests;

public class StatTests
{
	[Fact]
	public void BinCountsAndEdges()
	{
		// Arrange

		double[] values = [0.0, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5];

		// Act

		var bins = Stat.Bin(values, v => v, bins: 4);

		// Assert

		using var _ = new AssertionScope();

		bins.Should().HaveCount(4);

		// Range [0, 3.5], width 0.875; the max lands in the last bin.
		bins[0].Min.Should().Be(0.0);
		bins[3].Max.Should().BeApproximately(3.5, 1e-12);
		bins.Sum(b => b.Count).Should().Be(8);
		bins[0].Mid.Should().BeApproximately(0.4375, 1e-12);
	}

	[Fact]
	public void BinDensityIntegratesToOne()
	{
		// Arrange

		var rng = new Random(7);
		var values = Enumerable.Range(0, 500).Select(_ => rng.NextDouble() * 10).ToArray();

		// Act

		var bins = Stat.Bin(values, v => v, bins: 20);

		// Assert

		bins.Sum(b => b.Density * (b.Max - b.Min)).Should().BeApproximately(1.0, 1e-9);
	}

	[Fact]
	public void BinDegenerateRangeYieldsOneNominalBin()
	{
		// Arrange

		double[] values = [2.0, 2.0, 2.0];

		// Act

		var bins = Stat.Bin(values, v => v, bins: 5);

		// Assert

		using var _ = new AssertionScope();

		bins.Sum(b => b.Count).Should().Be(3);
		bins.Count(b => b.Count > 0).Should().Be(1);
	}

	[Fact]
	public void GroupedBinsUseEachGroupsOwnRange()
	{
		// Arrange

		// Group "a" spans [0, 1], group "b" spans [100, 101]: shared-range
		// binning would put each group in a single corner bin.
		(string g, double v)[] items = [("a", 0.0), ("a", 1.0), ("b", 100.0), ("b", 101.0)];

		// Act

		var bins = Stat.Bin(items, i => i.v, i => i.g, bins: 2);

		// Assert

		using var _ = new AssertionScope();

		bins.Should().HaveCount(4);

		var a = bins.Where(b => b.Group == "a").ToArray();
		var b2 = bins.Where(b => b.Group == "b").ToArray();

		a[0].Min.Should().Be(0.0);
		a[^1].Max.Should().BeApproximately(1.0, 1e-12);
		b2[0].Min.Should().Be(100.0);
		b2[^1].Max.Should().BeApproximately(101.0, 1e-12);

		// Groups emit in key order.
		bins.Select(b => b.Group).Should().Equal("a", "a", "b", "b");
	}

	[Fact]
	public void StatSourceRecomputesOverLiveData()
	{
		// Arrange

		var values = new List<double> { 1.0, 2.0 };
		var bins = Stat.Bin(values, v => v, bins: 2);

		var before = bins.Sum(b => b.Count);

		// Act

		values.Add(3.0);
		((IStatSource)bins).Recompute();

		// Assert

		using var _ = new AssertionScope();

		before.Should().Be(2);
		bins.Sum(b => b.Count).Should().Be(3);
	}
}

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
	public void DensityMatchesAnalyticReference()
	{
		// Arrange

		// Two points at ±1 with bandwidth 1: f(0) = φ(1), f(1) = (φ(0) + φ(2)) / 2.
		double[] values = [-1.0, 1.0];

		// Act

		// Grid −4..4 with 9 points lands exactly on 0 and 1.
		var density = Stat.Density(values, v => v, bandwidth: 1.0, n: 9, from: -4, to: 4);

		// Assert

		using var _ = new AssertionScope();

		density.Should().HaveCount(9);
		density[4].At.Should().Be(0.0);
		density[4].Density.Should().BeApproximately(0.2419707245191434, 1e-12);
		density[5].At.Should().Be(1.0);
		density[5].Density.Should().BeApproximately(0.22646662345731037, 1e-12);
	}

	[Fact]
	public void DensityIntegratesToOne()
	{
		// Arrange

		var rng = new Random(11);
		var values = Enumerable.Range(0, 200).Select(_ => rng.NextDouble() * 4 - 2).ToArray();

		// Act

		var density = Stat.Density(values, v => v, n: 512);

		// Assert

		var step = density[1].At - density[0].At;

		density.Sum(d => d.Density * step).Should().BeApproximately(1.0, 1e-2);
	}

	[Fact]
	public void Nrd0MatchesSilvermansRule()
	{
		// Arrange

		double[] values = [1.0, 2.0, 3.0, 4.0, 5.0];

		// Act

		var h = Stat.Nrd0(values);

		// Assert

		// sd = √2.5, IQR (type 7) = 2 → min(1.5811, 1.4925) · 0.9 · 5^(−1/5).
		h.Should().BeApproximately(0.9736, 1e-3);
	}

	[Fact]
	public void CountOrdersByKey()
	{
		// Arrange

		string[] items = ["b", "a", "b", "c", "b", "a"];

		// Act

		var counts = Stat.Count(items, s => s);

		// Assert

		using var _ = new AssertionScope();

		counts.Select(c => c.Key).Should().Equal("a", "b", "c");
		counts.Select(c => c.N).Should().Equal(2, 3, 1);
	}

	[Fact]
	public void SummaryComputesMeanAndSpread()
	{
		// Arrange

		(double x, double y)[] items = [(1, 2.0), (1, 4.0), (2, 10.0)];

		// Act

		var summary = Stat.Summary(items, i => i.x, i => i.y);

		// Assert

		using var _ = new AssertionScope();

		summary.Should().HaveCount(2);

		// x=1: mean 3, sample sd √2.
		summary[0].Center.Should().Be(3.0);
		summary[0].Lower.Should().BeApproximately(3.0 - Math.Sqrt(2.0), 1e-12);
		summary[0].Upper.Should().BeApproximately(3.0 + Math.Sqrt(2.0), 1e-12);

		// Single observation: zero spread.
		summary[1].Center.Should().Be(10.0);
		summary[1].Lower.Should().Be(10.0);
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

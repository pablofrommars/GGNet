using GGNet.Scales;

namespace GGNet.Headless.Tests;

// Geoms train the position scales through Map, so _min/_max arrive in transformed space, but
// explicit Limits are authored in data units. Commit used to hand the raw limits straight to
// SetRange, so Scale_X_Log10(limits: (1, 1000)) built a range of 1..1000 against data living
// in 0..3 and the marks collapsed into a sliver of the axis.
public class TransformedLimitsTests
{
	[Fact]
	public void Log10LimitsCommitInLogSpace()
	{
		// Arrange

		var sut = new Log10(limits: (1.0, 1000.0));
		sut.Shape(sut.Map(1.0), sut.Map(1000.0));

		// Act

		sut.Commit(grid: false);

		// Assert

		// log10(1) = 0 … log10(1000) = 3, then the default 5% expansion on a span of 3.
		using var _ = new AssertionScope();

		sut.Range.min.Should().BeApproximately(-0.15, 1e-9);
		sut.Range.max.Should().BeApproximately(3.15, 1e-9);
	}

	[Fact]
	public void SqrtLimitsCommitInSqrtSpace()
	{
		// Arrange

		var sut = new Extended(Transformations.Sqrt.Instance, limits: (0.0, 100.0));
		sut.Shape(sut.Map(0.0), sut.Map(100.0));

		// Act

		sut.Commit(grid: false);

		// Assert

		// sqrt(0) = 0 … sqrt(100) = 10, then 5% of the span of 10.
		using var _ = new AssertionScope();

		sut.Range.min.Should().BeApproximately(-0.5, 1e-9);
		sut.Range.max.Should().BeApproximately(10.5, 1e-9);
	}

	[Fact]
	public void UntransformedLimitsAreUnaffected()
	{
		// Arrange

		var sut = new Extended(limits: (5.0, 10.0));
		sut.Shape(0.0, 100.0);

		// Act

		sut.Commit(grid: false);

		// Assert

		// Identity transformation: data space is scale space, so nothing moves.
		using var _ = new AssertionScope();

		sut.Range.min.Should().BeApproximately(4.75, 1e-9);
		sut.Range.max.Should().BeApproximately(10.25, 1e-9);
	}

	[Fact]
	public void LimitedDataSpansTheAxis()
	{
		// Arrange

		var sut = new Log10(limits: (1.0, 1000.0));
		sut.Shape(sut.Map(1.0), sut.Map(1000.0));
		sut.Commit(grid: false);

		// Act

		var lower = sut.Coord(sut.Map(1.0));
		var upper = sut.Coord(sut.Map(1000.0));

		// Assert

		// The limited data fills the axis bar the 5% expansion at each end; it used to occupy
		// the bottom thousandth.
		using var _ = new AssertionScope();

		lower.Should().BeApproximately(0.0454545454, 1e-9);
		upper.Should().BeApproximately(0.9545454545, 1e-9);
	}

	private sealed record Point(double X, double Y);

	private static readonly Point[] decades =
	[
		new(1.0, 1.0),
		new(10.0, 2.0),
		new(100.0, 3.0),
		new(1000.0, 4.0)
	];

	[Fact]
	public async Task Log10LimitsProduceSaneBreakLabels()
	{
		// Arrange

		var plot = PlotContext.Build(decades, i => i.X, i => i.Y)
			.Scale_X_Log10(limits: (1.0, 1000.0))
			.Geom_Point()
			.Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		svg.Should().Contain(">1e3<");
		// Untransformed limits pushed the range to 1..1000 in log space, labeling 10^171.
		svg.Should().NotContain("1e171");
	}

	[Fact]
	public async Task XLimOnALogScaleIsHonoredInDataUnits()
	{
		// Arrange

		var plot = PlotContext.Build(decades, i => i.X, i => i.Y)
			.Scale_X_Log10()
			.XLim(1.0, 1000.0)
			.Geom_Point()
			.Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		svg.Should().Contain(">1e3<");
	}
}

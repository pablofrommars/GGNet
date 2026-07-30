using GGNet.Scales;

namespace GGNet.Headless.Tests;

// The runtime view window (implementation-blocks Block 3): a durable
// interaction-bucket override. It survives the per-pass Reset(), takes the
// committed range verbatim (no expansion — zoom shows the exact window), and
// clearing it restores the spec-driven range exactly.
public class ViewRangeTests
{
	[Fact]
	public void ViewRangeOverridesTrainedRangeWithoutExpansion()
	{
		// Arrange

		var sut = new Extended();
		sut.Shape(0.0, 100.0);
		sut.ViewRange = (20.0, 30.0);

		// Act

		sut.Commit(grid: false);

		// Assert

		using var _ = new AssertionScope();

		sut.Range.min.Should().Be(20.0);
		sut.Range.max.Should().Be(30.0);
	}

	[Fact]
	public void ViewRangeSurvivesClear()
	{
		// Arrange

		var sut = new Extended(limits: (5.0, 10.0));
		sut.Shape(0.0, 100.0);
		sut.ViewRange = (20.0, 30.0);

		// Act

		sut.Clear();
		sut.Commit(grid: false);

		// Assert

		using var _ = new AssertionScope();

		sut.Range.min.Should().Be(20.0);
		sut.Range.max.Should().Be(30.0);
	}

	[Fact]
	public void ClearedViewRangeRestoresTheSpecRange()
	{
		// Arrange

		var sut = new Extended(limits: (5.0, 10.0));
		sut.Shape(0.0, 100.0);
		sut.ViewRange = (20.0, 30.0);
		sut.Commit(grid: false);

		// Act

		sut.ViewRange = null;
		sut.Commit(grid: false);

		// Assert

		// Back to the author's Limits with the usual 5% expansion.
		using var _ = new AssertionScope();

		sut.Range.min.Should().BeApproximately(4.75, 1e-9);
		sut.Range.max.Should().BeApproximately(10.25, 1e-9);
	}

	[Fact]
	public void InvertedViewRangeNormalizes()
	{
		// Arrange

		var sut = new Extended();
		sut.Shape(0.0, 100.0);
		sut.ViewRange = (30.0, 20.0);

		// Act

		sut.Commit(grid: false);

		// Assert

		using var _ = new AssertionScope();

		sut.Range.min.Should().Be(20.0);
		sut.Range.max.Should().Be(30.0);
	}

	[Fact]
	public void ViewRangeRecomputesBreaksForTheWindow()
	{
		// Arrange

		var sut = new Extended();
		sut.Shape(0.0, 100.0);
		sut.ViewRange = (20.0, 30.0);

		// Act

		sut.Commit(grid: true);

		// Assert

		sut.Breaks.Should().NotBeEmpty()
			.And.OnlyContain(b => 20.0 <= b && b <= 30.0);
	}

	[Fact]
	public void DiscreteViewRangeSnapsAndKeepsCategoryPositions()
	{
		// Arrange

		var sut = new DiscretePosition<int>();
		sut.Train(10);
		sut.Train(20);
		sut.Train(30);
		sut.Train(40);
		sut.Train(50);

		// Act

		// A window over the middle categories (indexes 1..3).
		sut.ViewRange = (0.5, 3.5);
		sut.Commit(grid: true);

		// Assert

		// The range is the exact window; category positions stay index-based.
		using var _ = new AssertionScope();

		sut.Range.min.Should().Be(0.5);
		sut.Range.max.Should().Be(3.5);
		sut.Map(20).Should().Be(1.0);
	}

	private sealed record P(double X, double Y);

	private static readonly P[] data =
	[
		new(1.0, 2.0),
		new(2.0, 3.5),
		new(3.0, 2.8),
		new(4.0, 4.2)
	];

	private static PlotContext<P, double, double> PointPlot()
		=> PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Point()
			.Style();

	[Fact]
	public async Task RenderPassesAreIdempotent()
	{
		// Arrange

		var context = PointPlot();

		// Act

		var first = await context.AsStringAsync();
		var second = await context.AsStringAsync();

		// Assert

		// The property PlotContext.Render claims by construction, pinned:
		// a second pass over the same context yields identical output.
		second.Should().Be(first);
	}

	[Fact]
	public async Task ViewRangeSurvivesFullRenderPassAndWindowsTheOutput()
	{
		// Arrange

		var context = PointPlot();

		var baseline = await context.AsStringAsync();

		// Act

		// Scales exist after the first pass; the window must survive the
		// Reset() the next pass runs.
		context.Positions.X.Scales[0].ViewRange = (2.0, 3.0);

		var windowed = await context.AsStringAsync();

		context.Positions.X.Scales[0].ViewRange = null;

		var restored = await context.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		XDocument.Parse(windowed).Should().NotBeNull();
		windowed.Should().NotBe(baseline);
		restored.Should().Be(baseline);
	}
}

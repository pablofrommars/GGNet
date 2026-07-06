using GGNet.Components;
using GGNet.Coords;

namespace GGNet.Headless.Tests;

// Grid geometry as unit tests: what used to be SVG-regex assertions against
// rendered markup is now plain math against composed primitives.
public class GridCompositionTests
{
	private static readonly Zone area = new() { X = 10, Y = 20, Width = 200, Height = 100 };

	private static CartesianCoordinateSystem Cartesian()
	{
		var sut = new CartesianCoordinateSystem(Style.Default());

		sut.Measure(area);

		return sut;
	}

	private static PolarCoordinateSystem Polar(PolarRingType rings = PolarRingType.Polygon)
	{
		var sut = new PolarCoordinateSystem(new PolarOptions(), Style.Default(init: s => s.Polar.Rings = rings));

		sut.Measure(area);

		return sut;
	}

	private static GridInputs Inputs(
		IReadOnlyList<double>? xBreaks = null,
		IReadOnlyList<double>? xMinor = null,
		IReadOnlyList<(double f, string label)>? xLabels = null,
		IReadOnlyList<(double f, string title)>? xTitles = null,
		IReadOnlyList<double>? yBreaks = null,
		IReadOnlyList<double>? yMinor = null,
		IReadOnlyList<(double f, string label)>? yLabels = null)
		=> new(
			XAxis: true,
			YAxis: true,
			XBreaks: xBreaks ?? [],
			XMinorBreaks: xMinor ?? [],
			XLabels: xLabels ?? [],
			XTitles: xTitles ?? [],
			YBreaks: yBreaks ?? [],
			YMinorBreaks: yMinor ?? [],
			YLabels: yLabels ?? [],
			XLabelY: 130,
			XTitleY: 140,
			YLabelX: 8);

	[Fact]
	public void CartesianBreaksSpanTheArea()
	{
		// Arrange

		var sut = Cartesian();

		// Act

		var grid = sut.ComposeGrid(Inputs(xBreaks: [0.0, 0.5, 1.0], yBreaks: [0.25]));

		// Assert

		using var _ = new AssertionScope();

		grid.XLines.Should().Equal(
			new GridLine("x-break", 10, 10, 20, 120),
			new GridLine("x-break", 110, 110, 20, 120),
			new GridLine("x-break", 210, 210, 20, 120));

		grid.YLines.Should().Equal(
			new GridLine("y-break", 10, 210, 95, 95));
	}

	[Fact]
	public void CartesianMinorBreaksFollowMajors()
	{
		// Arrange

		var sut = Cartesian();

		// Act

		var grid = sut.ComposeGrid(Inputs(xBreaks: [0.5], xMinor: [0.25, 0.75]));

		// Assert

		grid.XLines.Should().Equal(
			new GridLine("x-break", 110, 110, 20, 120),
			new GridLine("x-minor-break", 60, 60, 20, 120),
			new GridLine("x-minor-break", 160, 160, 20, 120));
	}

	[Fact]
	public void CartesianDropsLabelsOutsideTheArea()
	{
		// Arrange

		var sut = Cartesian();

		// Act

		var grid = sut.ComposeGrid(Inputs(xLabels: [(0.0, "left-edge"), (0.5, "center"), (1.0, "right-edge")]));

		// Assert

		using var _ = new AssertionScope();

		grid.XLabels.Should().HaveCount(1);
		grid.XLabels[0].Text.Should().Be("center");
		grid.XLabels[0].X.Should().Be(110);
		grid.XLabels[0].Y.Should().Be(130);
		grid.XLabels[0].Clip.Should().Be(GridClip.Plot);
	}

	[Fact]
	public void CartesianBreakTitlesAnchorMiddle()
	{
		// Arrange

		var sut = Cartesian();

		// Act

		var grid = sut.ComposeGrid(Inputs(xTitles: [(0.5, "title")]));

		// Assert

		using var _ = new AssertionScope();

		grid.XLabels.Should().HaveCount(1);
		grid.XLabels[0].Class.Should().Be("x-break-title");
		grid.XLabels[0].Anchor.Should().Be("middle");
		grid.XLabels[0].Y.Should().Be(140);
	}

	[Fact]
	public void PolarSpokesRadiateFromCenter()
	{
		// Arrange

		var sut = Polar();

		// Act

		var grid = sut.ComposeGrid(Inputs(xBreaks: [0.0, 0.25, 0.5, 0.75]));

		// Assert

		using var _ = new AssertionScope();

		grid.XLines.Should().HaveCount(4);

		// Default start is 12 o'clock, clockwise: up, right, down, left.
		grid.XLines[0].X2.Should().BeApproximately(sut.CenterX, 1e-9);
		grid.XLines[0].Y2.Should().BeApproximately(sut.CenterY - sut.Radius, 1e-9);

		grid.XLines[1].X2.Should().BeApproximately(sut.CenterX + sut.Radius, 1e-9);
		grid.XLines[1].Y2.Should().BeApproximately(sut.CenterY, 1e-9);

		grid.XLines[2].X2.Should().BeApproximately(sut.CenterX, 1e-9);
		grid.XLines[2].Y2.Should().BeApproximately(sut.CenterY + sut.Radius, 1e-9);

		grid.XLines[3].X2.Should().BeApproximately(sut.CenterX - sut.Radius, 1e-9);
		grid.XLines[3].Y2.Should().BeApproximately(sut.CenterY, 1e-9);
	}

	[Fact]
	public void PolarPolygonRingsFollowTheSpokes()
	{
		// Arrange

		var sut = Polar(PolarRingType.Polygon);

		// Act

		var grid = sut.ComposeGrid(Inputs(xBreaks: [0.0, 0.25, 0.5, 0.75], yBreaks: [0.5], yMinor: [0.25]));

		// Assert

		using var _ = new AssertionScope();

		grid.Rings.Should().BeEmpty();
		grid.RingPaths.Should().HaveCount(2);

		grid.RingPaths[0].Class.Should().Be("y-break");
		grid.RingPaths[0].Points.Should().HaveCount(4);
		grid.RingPaths[0].Points[1].x.Should().BeApproximately(sut.CenterX + 0.5 * sut.Radius, 1e-9);

		grid.RingPaths[1].Class.Should().Be("y-minor-break");
	}

	[Fact]
	public void PolarCircleRingsScaleWithFraction()
	{
		// Arrange

		var sut = Polar(PolarRingType.Circle);

		// Act

		var grid = sut.ComposeGrid(Inputs(xBreaks: [0.0, 0.25, 0.5, 0.75], yBreaks: [0.5, 1.0]));

		// Assert

		using var _ = new AssertionScope();

		grid.RingPaths.Should().BeEmpty();

		grid.Rings.Should().Equal(
			new GridRing("y-break", sut.CenterX, sut.CenterY, 0.5 * sut.Radius),
			new GridRing("y-break", sut.CenterX, sut.CenterY, sut.Radius));
	}

	[Fact]
	public void PolarRingsOutsideUnitRangeAreDropped()
	{
		// Arrange

		var sut = Polar(PolarRingType.Circle);

		// Act

		var grid = sut.ComposeGrid(Inputs(yBreaks: [-0.25, 0.5, 1.25]));

		// Assert

		using var _ = new AssertionScope();

		grid.Rings.Should().HaveCount(1);
		grid.Rings[0].Radius.Should().Be(0.5 * sut.Radius);
	}

	[Fact]
	public void PolarAngularLabelsAnchorByQuadrant()
	{
		// Arrange

		var sut = Polar();

		// Act

		var grid = sut.ComposeGrid(Inputs(xLabels: [(0.0, "up"), (0.25, "right"), (0.5, "down"), (0.75, "left")]));

		// Assert

		using var _ = new AssertionScope();

		grid.XLabels.Should().HaveCount(4);

		grid.XLabels[0].Anchor.Should().Be("middle");
		grid.XLabels[1].Anchor.Should().Be("start");
		grid.XLabels[2].Anchor.Should().Be("middle");
		grid.XLabels[3].Anchor.Should().Be("end");

		grid.XLabels[1].X.Should().BeGreaterThan(sut.CenterX + sut.Radius);
		grid.XLabels[3].X.Should().BeLessThan(sut.CenterX - sut.Radius);
	}

	[Fact]
	public void PolarRadialLabelsSitOnTheUpAxis()
	{
		// Arrange

		var sut = Polar();

		// Act

		var grid = sut.ComposeGrid(Inputs(yLabels: [(0.0, "0"), (0.5, "5"), (1.25, "off-scale")]));

		// Assert

		using var _ = new AssertionScope();

		grid.YLabels.Should().HaveCount(2);

		grid.YLabels[0].Text.Should().Be("0");
		grid.YLabels[0].X.Should().BeApproximately(sut.CenterX + 4, 1e-9);
		grid.YLabels[0].Y.Should().BeApproximately(sut.CenterY - 2, 1e-9);

		grid.YLabels[1].Y.Should().BeApproximately(sut.CenterY - 0.5 * sut.Radius - 2, 1e-9);
		grid.YLabels[1].Anchor.Should().Be("start");
	}
}

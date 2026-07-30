using GGNet.Components;
using GGNet.Scales;

// Alias: GGNet.Scales.Log10 (the scale) collides with the transformation.
using Log10 = GGNet.Transformations.Log10;

namespace GGNet.Headless.Tests;

// The interactivity inverse seam (implementation-blocks Block 1): every forward
// map has a pure inverse, so pixel gestures can recover data-space windows.
public class InverseProjectionTests
{
	private static CartesianCoordinateSystem Cartesian()
	{
		var sut = new CartesianCoordinateSystem(Style.Default());

		sut.Measure(new Zone { X = 40, Y = 20, Width = 640, Height = 480 });

		return sut;
	}

	[Theory]
	[InlineData(0.0, 0.0)]
	[InlineData(0.3, 0.7)]
	[InlineData(1.0, 1.0)]
	public void CartesianUnprojectInvertsProject(double cx, double cy)
	{
		// Arrange

		var sut = Cartesian();

		// Act

		var (px, py) = sut.Project(cx, cy);
		var (rx, ry) = sut.Unproject(px, py);

		// Assert

		using var _ = new AssertionScope();

		rx.Should().BeApproximately(cx, 1e-12);
		ry.Should().BeApproximately(cy, 1e-12);
	}

	[Fact]
	public void CartesianUnprojectMapsPixelsToFractions()
	{
		// Arrange

		var sut = Cartesian();

		// Act

		var (cx, cy) = sut.Unproject(40.0 + 160.0, 20.0 + 120.0);

		// Assert

		// A quarter across, a quarter down — and y is flipped: down is smaller.
		using var _ = new AssertionScope();

		cx.Should().BeApproximately(0.25, 1e-12);
		cy.Should().BeApproximately(0.75, 1e-12);
	}

	[Fact]
	public void PolarUnprojectThrows()
	{
		// Arrange

		var sut = new PolarCoordinateSystem(new PolarOptions(), Style.Default());

		// Act

		Action act = () => _ = sut.Unproject(0.0, 0.0);

		// Assert

		act.Should().Throw<GGNetUserException>();
	}

	[Theory]
	[InlineData(1.0)]
	[InlineData(4.2)]
	[InlineData(9.0)]
	public void ExtendedInvertInvertsCoord(double value)
	{
		// Arrange

		var sut = new Extended();
		sut.Shape(1.0, 9.0);
		sut.Commit(grid: false);

		// Act

		var roundTripped = sut.Invert(sut.Coord(value));

		// Assert

		roundTripped.Should().BeApproximately(value, 1e-9);
	}

	[Fact]
	public void Log10InvertRecoversDataValue()
	{
		// Arrange

		// Geoms shape in transformed space: data 10..1000 arrives as log10 = 1..3.
		var sut = new Extended(transformation: Log10.Instance);
		sut.Shape(1.0, 3.0);
		sut.Commit(grid: false);

		// Act

		var fraction = sut.Coord(sut.Map(100.0));
		var data = Log10.Instance.Inverse(sut.Invert(fraction));

		// Assert

		data.Should().BeApproximately(100.0, 1e-9);
	}

	[Fact]
	public void PixelRoundTripsToDataThroughCoordAndScale()
	{
		// Arrange

		var coord = Cartesian();

		var scale = new Extended();
		scale.Shape(0.0, 10.0);
		scale.Commit(grid: false);

		// Act

		// The gesture path: data → fraction → pixel, then pixel → fraction → data.
		var (px, _) = coord.Project(scale.Coord(scale.Map(7.0)), 0.0);
		var (cx, _) = coord.Unproject(px, 0.0);
		var data = scale.Invert(cx);

		// Assert

		data.Should().BeApproximately(7.0, 1e-9);
	}
}

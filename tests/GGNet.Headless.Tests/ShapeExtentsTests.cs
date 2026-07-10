using GGNet.Shapes;

namespace GGNet.Headless.Tests;

// The fit-from-shapes walker (A+): visible-y extents per Shape variant,
// including boundary interpolation for segment-based shapes.
public class ShapeExtentsTests
{
	private static readonly (double min, double max) window = (2.0, 4.0);

	[Fact]
	public void RectangleKeepsTheBaseline()
	{
		// Arrange

		// A bar from the zero baseline, partially inside the window.
		var bar = new Rectangle { X = 3.5, Y = 0.0, Width = 1.0, Height = 7.5, Aesthetic = new() };

		// Act

		var (min, max) = ShapeExtents.VisibleY([bar], window);

		// Assert

		using var _ = new AssertionScope();

		min.Should().Be(0.0);
		max.Should().Be(7.5);
	}

	[Fact]
	public void SegmentInterpolatesAtTheWindowBoundaries()
	{
		// Arrange

		// y = x from 0 to 10: inside [2, 4] the segment spans y 2..4 exactly.
		var line = new Line { X1 = 0.0, Y1 = 0.0, X2 = 10.0, Y2 = 10.0, Aesthetic = new() };

		// Act

		var (min, max) = ShapeExtents.VisibleY([line], window);

		// Assert

		using var _ = new AssertionScope();

		min.Should().BeApproximately(2.0, 1e-9);
		max.Should().BeApproximately(4.0, 1e-9);
	}

	[Fact]
	public void PointsOutsideTheWindowAreIgnored()
	{
		// Arrange

		var inside = new Circle { X = 3.0, Y = 5.0, Aesthetic = new() };
		var outside = new Circle { X = 9.0, Y = 100.0, Aesthetic = new() };

		// Act

		var (min, max) = ShapeExtents.VisibleY([inside, outside], window);

		// Assert

		using var _ = new AssertionScope();

		min.Should().Be(5.0);
		max.Should().Be(5.0);
	}

	[Fact]
	public void AreaContributesBothBands()
	{
		// Arrange

		var area = new Area { Aesthetic = new() };
		area.Points.Add((2.0, 1.0, 3.0));
		area.Points.Add((4.0, 2.0, 6.0));

		// Act

		var (min, max) = ShapeExtents.VisibleY([area], window);

		// Assert

		using var _ = new AssertionScope();

		min.Should().Be(1.0);
		max.Should().Be(6.0);
	}

	[Fact]
	public void ReferenceLinesParticipateAndUnboundedAnnotationsDoNot()
	{
		// Arrange

		var hline = new HLine { Y = 42.0, Label = "target", Line = new(), Text = new() };
		var vline = new VLine { X = 3.0, Label = "event", Line = new(), Text = new() };

		// Act

		var (min, max) = ShapeExtents.VisibleY([hline, vline], window);

		// Assert

		using var _ = new AssertionScope();

		min.Should().Be(42.0);
		max.Should().Be(42.0);
	}
}

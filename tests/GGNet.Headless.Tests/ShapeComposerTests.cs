using GGNet.Components;
using GGNet.Geoms;
using GGNet.Scene;
using GGNet.Shapes;
using GGNet.Transformations;

namespace GGNet.Headless.Tests;

// Shape composition as unit tests: projection, path assembly and annotation
// label placement asserted directly on screen primitives.
public class ShapeComposerTests
{
	private sealed class TestCoord : ICoord
	{
		public double ToX(double value) => value * 10.0;

		public (double min, double max) XRange => (0, 10);

		public ITransformation<double> XTransformation => Identity<double>.Instance;

		public double ToY(double value) => 100.0 - value * 10.0;

		public (double min, double max) YRange => (0, 10);

		public ITransformation<double> YTransformation => Identity<double>.Instance;
	}

	private sealed class FakeGeom : IGeom
	{
		public List<Shape> Layer { get; } = [];

		public void Train()
		{
		}

		public void Legend()
		{
		}

		public void Shape()
		{
		}

		public void Clear() => Layer.Clear();
	}

	private static readonly Zone zone = new() { X = 0, Y = 0, Width = 100, Height = 100 };

	private static List<ScreenPrimitive> Compose(params Shape[] shapes)
	{
		var geom = new FakeGeom();

		foreach (var shape in shapes)
		{
			geom.Layer.Add(shape);
		}

		return ShapeComposer.Compose([geom], new TestCoord(), zone);
	}

	[Fact]
	public void CircleProjects()
	{
		// Arrange

		var circle = new Circle { X = 2, Y = 3, Aesthetic = new() { Radius = 5, Fill = "#123456" } };

		// Act

		var scene = Compose(circle);

		// Assert

		using var _ = new AssertionScope();

		scene.Should().HaveCount(1);

		// Unions box as the union wrapper and expose no case casts: access is
		// pattern matching only — BeOfType/reflection see ScreenPrimitive.
		var screen = scene[0] switch { ScreenCircle c => c, var other => throw new InvalidOperationException($"expected circle, got {other}") };

		screen.X.Should().Be(20);
		screen.Y.Should().Be(70);
		screen.Radius.Should().Be(5);
	}

	[Fact]
	public void PathRestartsAtGaps()
	{
		// Arrange

		// A NaN y is a piecewise gap: the path lifts the pen and restarts with M.
		var path = new Shapes.Path { Aesthetic = new() };

		path.Points.Add((1.0, 1.0));
		path.Points.Add((2.0, double.NaN));
		path.Points.Add((3.0, 2.0));

		// Act

		var scene = Compose(path);

		// Assert

		var stroke = scene[0] switch { ScreenStroke v => v, var other => throw new InvalidOperationException($"expected stroke, got {other}") };

		stroke.D.Should().Be(" M 10 90 M 30 80");
	}

	[Fact]
	public void AreaOutlineClosesBackward()
	{
		// Arrange

		var area = new Shapes.Area { Aesthetic = new() };

		area.Points.Add((1.0, 0.5, 2.0));
		area.Points.Add((2.0, 1.0, 3.0));

		// Act

		var scene = Compose(area);

		// Assert

		// Forward along ymax, backward along ymin, closed.
		var fill = scene[0] switch { ScreenFill v => v, var other => throw new InvalidOperationException($"expected fill, got {other}") };

		fill.D.Should().Be("M 10 80 L 20 70 L 20 90 L 10 95 Z");
	}

	[Fact]
	public void VLineLabelRotatesByAnchor()
	{
		// Arrange

		var vline = new VLine
		{
			X = 4,
			Label = "mark",
			Line = new(),
			Text = new() { Anchor = GGNet.Anchor.End }
		};

		// Act

		var scene = Compose(vline);

		// Assert

		using var _ = new AssertionScope();

		scene.Should().HaveCount(2);

		var rule = scene[0] switch { ScreenRule v => v, var other => throw new InvalidOperationException($"expected rule, got {other}") };

		rule.X1.Should().Be(40);

		var label = scene[1] switch { ScreenLabel v => v, var other => throw new InvalidOperationException($"expected label, got {other}") };

		label.Transform.Should().Be("translate(43px, 2.5px) rotate(90deg)");
		label.Anchor.Should().Be("start");
	}

	[Fact]
	public void LayerOrderIsPreserved()
	{
		// Arrange

		var first = new Circle { X = 1, Y = 1, Aesthetic = new() };
		var second = new Line { X1 = 0, Y1 = 0, X2 = 1, Y2 = 1, Aesthetic = new() };

		// Act

		var scene = Compose(first, second);

		// Assert

		using var _ = new AssertionScope();

		(scene[0] is ScreenCircle).Should().BeTrue();
		(scene[1] is ScreenLine).Should().BeTrue();
	}
}

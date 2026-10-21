namespace GGNet.Headless.Tests;

public class AreaExtentTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task LogAreaIncludesTheBaselineInTransformedCoordinates(bool polar)
	{
		// Arrange

		var context = PlotContext.Build(new[] { (X: 1.0, Y: 100.0), (X: 2.0, Y: 101.0) }, p => p.X, p => p.Y)
			.Scale_Y_Log10().Geom_Area();
		if (polar)
		{
			context.Coord_Polar();
		}

		// Act

		var svg = await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		context.Positions.Y.Scales[0].Coord(0).Should().BeInRange(0, 1);
		svg.Should().NotContain("NaN").And.NotContain("Infinity");
	}

	[Theory]
	[InlineData(10, 20)]
	[InlineData(10, 10)]
	[InlineData(20, 10)]
	public async Task UnequalGridsPreserveEndpointsAndInterpolateFromTheStartingOrdinate(double first, double last)
	{
		// Arrange

		var source = new[]
		{
			(X: 0.0, Y: first, Group: "A"), (X: 2.0, Y: last, Group: "A"),
			(X: 0.0, Y: 1.0, Group: "B"), (X: 1.0, Y: 1.0, Group: "B"), (X: 2.0, Y: 1.0, Group: "B")
		};
		var context = PlotContext.Build(source, p => p.X, p => p.Y)
			.Scale_Fill_Discrete(p => p.Group, ["red", "blue"], guide: false)
			.Geom_Area(position: PositionAdjustment.Stack).Style();

		// Act

		var svg = await context.AsStringAsync();
		var areas = context.Panels[0].Geoms[0].Layer.Select(s => (Shapes.Area)s.Value!).ToArray();

		// Assert

		using var _ = new AssertionScope();
		XDocument.Parse(svg).Should().NotBeNull();
		areas[0].Points[0].ymax.Should().Be(first);
		areas[0].Points[1].ymax.Should().Be((first + last) / 2);
		areas[0].Points[2].ymax.Should().Be(last);
		areas[1].Points[1].ymax.Should().Be((first + last) / 2 + 1);
		areas[1].Points[2].ymax.Should().Be(last + 1);
	}

	[Fact]
	public async Task InterpolatedStackPeakTrainsTheAxis()
	{
		// Arrange

		var source = new[]
		{
			(X: 0.0, Y: 0.0, Group: "A"), (X: 1.0, Y: 100.0, Group: "A"), (X: 2.0, Y: 0.0, Group: "A"),
			(X: 0.0, Y: 10.0, Group: "B"), (X: 2.0, Y: 10.0, Group: "B")
		};
		var context = PlotContext.Build(source, p => p.X, p => p.Y)
			.Scale_Fill_Discrete(p => p.Group, ["red", "blue"], guide: false)
			.Geom_Area(position: PositionAdjustment.Stack).Style();

		// Act

		await context.AsStringAsync();

		// Assert

		context.Positions.Y.Scales[0].Coord(110).Should().BeInRange(0, 1);
	}

	[Theory]
	[InlineData(100, 101)]
	[InlineData(-101, -100)]
	[InlineData(-100, 100)]
	public async Task IdentityAreaIncludesItsBaselineAndAgreesWithAutoFit(double first, double last)
	{
		// Arrange

		var context = PlotContext.Build(new[] { (X: 0.0, Y: first), (X: 1.0, Y: last) }, p => p.X, p => p.Y)
			.Geom_Area().Style();

		// Act

		await context.AsStringAsync();
		var range = context.Positions.Y.Scales[0].Range;
		context.FitYToXView();
		await context.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		range.min.Should().BeLessThanOrEqualTo(Math.Min(0, first));
		range.max.Should().BeGreaterThanOrEqualTo(Math.Max(0, last));
		context.Positions.Y.Scales[0].Range.Should().Be(range);
	}

	[Fact]
	public async Task ExplicitAreaWindowStillCropsTheBaseline()
	{
		// Arrange

		var context = PlotContext.Build(new[] { (X: 0.0, Y: 100.0), (X: 1.0, Y: 101.0) }, p => p.X, p => p.Y)
			.Geom_Area().Style();
		context.SetYView(99, 102);

		// Act

		await context.AsStringAsync();

		// Assert

		context.Positions.Y.Scales[0].Range.Should().Be((99, 102));
	}
}

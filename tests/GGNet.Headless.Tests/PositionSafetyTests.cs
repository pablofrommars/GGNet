using GGNet.Scales;

namespace GGNet.Headless.Tests;

public class PositionSafetyTests
{
	private static readonly double[] logZeroData = [0.0, 1.0, 10.0];
	private static readonly double[] positiveData = [1.0, 10.0];

	[Theory]
	[InlineData("line")]
	[InlineData("area")]
	[InlineData("bar")]
	[InlineData("text")]
	public async Task LogZeroIsRejectedEvenByGeomsWithoutMissingValueGuards(string geom)
	{
		// Arrange

		var context = PlotContext.Build(logZeroData, x => x, x => x).Scale_X_Log10();
		switch (geom)
		{
			case "line":
				context.Geom_Line();
				break;
			case "area":
				context.Geom_Area();
				break;
			case "bar":
				context.Geom_Bar();
				break;
			case "text":
				context.Geom_Text(text: _ => "label");
				break;
		}

		// Act

		var act = async () => await context.Style().AsStringAsync();

		// Assert

		await act.Should().ThrowAsync<GGNetUserException>().WithMessage("*positive finite*");
	}

	[Fact]
	public void InvalidLogLimitsAndViewsAreRejected()
	{
		// Arrange

		var scale = new Log10(limits: (0, 10));
		var context = PlotContext.Build(positiveData, x => x, x => x).Scale_X_Log10().Geom_Point().Style();
		context.SetXView(1, 10);

		// Act

		var commit = () => scale.Commit(grid: true);
		var setView = () => context.SetXView(0, 10);

		// Assert

		using var _ = new AssertionScope();
		commit.Should().Throw<GGNetUserException>();
		setView.Should().Throw<GGNetUserException>();
		context.Positions.X.View.Should().Be((0.0, 1.0));
	}

	[Fact]
	public async Task ExtremePositiveLogDataKeepsFiniteGeometryAndLabels()
	{
		// Arrange

		var context = PlotContext.Build(new[] { double.Epsilon, 1.0, double.MaxValue }, x => x, x => x)
			.Scale_X_Log10().Scale_Y_Log10().Geom_Point().Style();

		// Act

		var svg = await context.AsStringAsync();

		// Assert

		svg.Should().NotContain("NaN").And.NotContain("Infinity");
	}

	[Fact]
	public void SwitchingToAnExtremeViewClearsOldMinorTicks()
	{
		// Arrange

		var scale = new Extended { ViewRange = (0, 10) };
		scale.Commit(grid: true);

		// Act

		scale.ViewRange = (-1e308, 1e308);
		scale.Commit(grid: true);

		// Assert

		scale.MinorBreaks.Should().BeEmpty();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public async Task InvalidLogDataFailsBeforeGridComposition(double value)
	{
		// Arrange

		var context = PlotContext.Build(new[] { 1.0, value, 10.0 }, x => x, x => x)
			.Scale_X_Log10().Geom_Point().Style();

		// Act

		var act = async () => await context.AsStringAsync();

		// Assert

		await act.Should().ThrowAsync<GGNetUserException>().WithMessage("*positive finite*");
	}

	[Fact]
	public async Task MissingLogPointsRetainValidSurvivors()
	{
		// Arrange

		var context = PlotContext.Build(new[] { 1.0, double.NaN, 10.0 }, x => x, x => x)
			.Scale_X_Log10().Geom_Point().Style();

		// Act

		var svg = await context.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		svg.Should().NotContain("NaN").And.NotContain("Infinity");
		XDocument.Parse(svg).Descendants().Count(e => e.Name.LocalName == "circle").Should().Be(2);
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void InvalidLimitsAndViewsCannotEnterTickGeneration(double value)
	{
		// Arrange

		var scale = new Extended(limits: (value, 10));
		var view = new Extended { ViewRange = (value, 10) };

		// Act

		var commitLimit = () => scale.Commit(grid: true);
		var commitView = () => view.Commit(grid: true);

		// Assert

		using var _ = new AssertionScope();
		commitLimit.Should().Throw<GGNetUserException>();
		commitView.Should().Throw<GGNetUserException>();
	}

	[Theory]
	[InlineData(1e20)]
	[InlineData(-1e20)]
	[InlineData(double.MaxValue)]
	[InlineData(double.MinValue)]
	public void LargeSingletonHasFiniteDistinctBoundsAndTicks(double value)
	{
		// Arrange

		var scale = new Extended();
		scale.Shape(value, value);

		// Act

		scale.Commit(grid: true);

		// Assert

		using var _ = new AssertionScope();
		scale.Range.min.Should().BeLessThan(scale.Range.max);
		double.IsFinite(scale.Range.min).Should().BeTrue();
		double.IsFinite(scale.Range.max).Should().BeTrue();
		scale.Coord(value).Should().BeInRange(0, 1);
		scale.Breaks.Should().NotBeEmpty().And.OnlyContain(x => double.IsFinite(x));
	}

	[Theory]
	[InlineData(-1e308, 1e308)]
	[InlineData(double.MinValue, double.MaxValue)]
	[InlineData(1e20, 1.0000000000000002e20)]
	[InlineData(0, double.Epsilon)]
	public void ExtremeViewHasFiniteTicksAndCorrectForwardAndInverseEndpoints(double min, double max)
	{
		// Arrange

		var scale = new Extended { ViewRange = (min, max) };

		// Act

		scale.Commit(grid: true);

		// Assert

		using var _ = new AssertionScope();
		scale.Coord(min).Should().Be(0);
		scale.Coord(max).Should().Be(1);
		scale.Invert(0).Should().Be(min);
		scale.Invert(1).Should().Be(max);
		double.IsFinite(scale.Invert(0.5)).Should().BeTrue();
		scale.Breaks.Should().NotBeEmpty().And.OnlyContain(x => double.IsFinite(x));
	}

	[Fact]
	public async Task ExtremeFiniteDataExportsAndAutoFitsWithoutNonfiniteSvg()
	{
		// Arrange

		var context = PlotContext.Build(new[] { -1e308, 0.0, 1e308 }, x => x, y => y)
			.Geom_Point().Style();

		// Act

		var svg = await context.AsStringAsync();
		context.FitYToXView();
		var fitted = await context.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		svg.Should().NotContain("NaN").And.NotContain("Infinity");
		fitted.Should().NotContain("NaN").And.NotContain("Infinity");
		context.Positions.X.Scales[0].Coord(0).Should().Be(0.5);
	}

	[Theory]
	[InlineData(0.05)]
	[InlineData(0.5)]
	[InlineData(0.95)]
	public void ExtremeInverseRoundTripsInteriorCoordinates(double fraction)
	{
		// Arrange

		var scale = new Extended { ViewRange = (-double.MaxValue, double.MaxValue) };
		scale.Commit(grid: false);

		// Act

		var value = scale.Invert(fraction);

		// Assert

		using var _ = new AssertionScope();
		double.IsFinite(value).Should().BeTrue();
		scale.Coord(value).Should().BeApproximately(fraction, 1e-15);
	}
}

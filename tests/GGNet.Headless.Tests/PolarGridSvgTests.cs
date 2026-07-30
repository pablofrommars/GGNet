namespace GGNet.Headless.Tests;

public class PolarGridSvgTests
{
	public enum Metric
	{
		Speed,
		Power,
		Range,
		Weight,
		Cost
	}

	public sealed record Item(Metric Metric, double Value);

	private static readonly Item[] items =
	[
		new(Metric.Speed, 3.0),
		new(Metric.Power, 4.0),
		new(Metric.Range, 2.0),
		new(Metric.Weight, 5.0),
		new(Metric.Cost, 1.0)
	];

	[Fact]
	public async Task RenderPolarPointPlot()
	{
		// Arrange

		var plot = PlotContext.Build(items, i => i.Metric, i => i.Value)
		  .Geom_Point()
		  .Coord_Polar()
		  .Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		Regex.Count(svg, "class=\"x-break\"").Should().Be(5);
		Regex.Count(svg, "<path class=\"y-break\"").Should().BeGreaterThanOrEqualTo(1);
		Regex.Count(svg, "class=\"x-break-label\"").Should().Be(5);

		svg.Should().NotContain("class=\"x-break-title\"");
		svg.Should().NotContain("class=\"x-title\"");
	}

	[Fact]
	public async Task RenderCircleRings()
	{
		// Arrange

		var plot = PlotContext.Build(items, i => i.Metric, i => i.Value)
		  .Geom_Point()
		  .Coord_Polar()
		  .Style(style: Style.Default(init: s => s.Polar.Rings = PolarRingType.Circle));

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		Regex.Count(svg, "<circle class=\"y-break\"").Should().BeGreaterThanOrEqualTo(1);
		svg.Should().NotContain("<path class=\"y-break\"");
	}
}

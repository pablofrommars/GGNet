namespace GGNet.Headless.Tests;

public class RadarSvgTests
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

	private static readonly Item[] widget =
	[
		new(Metric.Speed, 3.0),
		new(Metric.Power, 4.0),
		new(Metric.Range, 2.0),
		new(Metric.Weight, 5.0),
		new(Metric.Cost, 1.0)
	];

	private static readonly Item[] gadget =
	[
		new(Metric.Speed, 4.5),
		new(Metric.Power, 2.0),
		new(Metric.Range, 4.0),
		new(Metric.Weight, 2.5),
		new(Metric.Cost, 3.5)
	];

	[Fact]
	public async Task RenderTwoSeries()
	{
		// Arrange

		var plot = PlotContext.Build(widget, i => i.Metric, i => i.Value)
		  .Geom_Radar(tooltip: i => b => b.AddContent(0, i.Value))
		  .Geom_Radar(gadget, i => i.Metric, i => i.Value, fill: "#fc9d23", tooltip: i => b => b.AddContent(0, i.Value))
		  .Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		var polygons = Regex.Matches(svg, "<path d=\"([^\"]*)\"");

		using var _ = new AssertionScope();

		polygons.Count.Should().Be(2);

		foreach (Match polygon in polygons)
		{
			polygon.Groups[1].Value.Trim().Should().EndWith("Z");
		}

		Regex.Count(svg, "fill=\"transparent\"").Should().Be(10);
		Regex.Count(svg, "class=\"x-break\"").Should().Be(5);
		Regex.Count(svg, "class=\"x-break-label\"").Should().Be(5);
		Regex.Count(svg, "<path class=\"y-break\"").Should().BeGreaterThanOrEqualTo(1);
	}

	[Fact]
	public async Task ZeroBasedRadialScale()
	{
		// Arrange

		// All values sit well above zero; the radial scale must still start at 0,
		// so a "0" break label renders at the web center.
		var plot = PlotContext.Build(widget, i => i.Metric, i => i.Value)
		  .Geom_Radar()
		  .Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		svg.Should().MatchRegex("class=\"y-break-label\"[^>]*>0</text>");
	}
}

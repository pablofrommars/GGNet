namespace GGNet.Headless.Tests;

// The render pipeline is idempotent: rendering the same context twice must
// yield identical output. This guards the historical duplicate-panels bug
// (Render(true) twice) and legend-element accumulation across passes.
public class RenderPipelineTests
{
	private sealed record XY(double X, double Y, double Group);

	private static readonly XY[] xy =
	[
		new(1.0, 2.0, 1),
		new(2.0, 3.5, 1),
		new(3.0, 2.8, 2),
		new(4.0, 4.2, 2),
		new(5.0, 3.1, 2)
	];

	[Fact]
	public async Task RenderTwiceIdentical()
	{
		// Arrange

		var plot = PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Point()
			.Style();

		// Act

		var one = await plot.AsStringAsync();
		var two = await plot.AsStringAsync();

		// Assert

		two.Should().Be(one);
	}

	[Fact]
	public async Task RenderTwiceWithLegendIdentical()
	{
		// Arrange

		var plot = PlotContext.Build(xy, i => i.X, i => i.Y)
			.Scale_Color_Discrete(i => i.Group, ["#111111", "#222222"], name: "group")
			.Geom_Point()
			.Style();

		// Act

		var one = await plot.AsStringAsync();
		var two = await plot.AsStringAsync();

		// Assert

		two.Should().Be(one);
	}

	[Fact]
	public async Task RenderTwiceFacetedIdentical()
	{
		// Arrange

		var plot = PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Point()
			.Facet_Wrap(i => i.Group)
			.Style();

		// Act

		var one = await plot.AsStringAsync();
		var two = await plot.AsStringAsync();

		// Assert

		two.Should().Be(one);
	}
}

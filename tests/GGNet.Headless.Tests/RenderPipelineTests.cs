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
	public async Task RenderTwiceThroughStatSourceIdentical()
	{
		// Arrange

		var plot = PlotContext.Build(Stat.Bin(xy, i => i.Y, bins: 4), b => b.Mid, b => b.Count)
			.Geom_Bar()
			.Style();

		// Act

		var one = await plot.AsStringAsync();
		var two = await plot.AsStringAsync();

		// Assert

		two.Should().Be(one);
	}

	[Fact]
	public async Task StatSourceRebinsOnRefresh()
	{
		// Arrange

		var live = new List<XY> { new(1, 2.0, 1), new(2, 3.5, 1), new(3, 2.8, 2) };

		var plot = PlotContext.Build(Stat.Bin(live, i => i.Y, bins: 3), b => b.Mid, b => b.Count)
			.Geom_Bar()
			.Style();

		var before = await plot.AsStringAsync();

		// Act

		live.Add(new(4, 9.0, 2));

		var after = await plot.AsStringAsync();

		// Assert

		after.Should().NotBe(before);
	}

	// An unmapped (constant-fill) ribbon caches its single area shape; the cache must not
	// outlive Clear(), or the second pass renders no ribbon at all.
	[Fact]
	public async Task RenderTwiceUnmappedRibbonIdentical()
	{
		// Arrange

		var plot = PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Ribbon(ymin: i => i.Y - 0.5, ymax: i => i.Y + 0.5)
			.Style();

		// Act

		var one = await plot.AsStringAsync();
		var two = await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		one.Should().Contain("fill=\"#23d0fc\"");
		two.Should().Contain("fill=\"#23d0fc\"");
		two.Should().Be(one);
	}

	[Fact]
	public async Task RenderTwiceMappedRibbonIdentical()
	{
		// Arrange

		var plot = PlotContext.Build(xy, i => i.X, i => i.Y)
			.Scale_Fill_Discrete(i => i.Group, ["#111111", "#222222"], name: "group")
			.Geom_Ribbon(ymin: i => i.Y - 0.5, ymax: i => i.Y + 0.5)
			.Style();

		// Act

		var one = await plot.AsStringAsync();
		var two = await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		one.Should().Contain("fill=\"#111111\"").And.Contain("fill=\"#222222\"");
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

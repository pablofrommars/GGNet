namespace GGNet.Headless.Tests;

// A source that temporarily produces no rows (streaming, filtering) yields no facets and so no
// panels. That is an empty plot, not a failure — but the faceted legend path used to index
// Panels[0] unconditionally on every gridded pass.
public class EmptyFacetTests
{
	private sealed record Reading(double X, double Y, string Tank);

	private static readonly Reading[] populated =
	[
		new(1.0, 2.0, "a"),
		new(2.0, 3.0, "a"),
		new(3.0, 4.0, "b")
	];

	[Fact]
	public async Task EmptyFacetedSourceRendersAnEmptyPlot()
	{
		// Arrange

		var plot = PlotContext.Build(Array.Empty<Reading>(), i => i.X, i => i.Y)
			.Geom_Point()
			.Facet_Wrap(i => i.Tank)
			.Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		svg.Should().Contain("<svg");
		XDocument.Parse(svg).Descendants()
			.Should().NotContain(element => element.Name.LocalName == "circle");
	}

	[Fact]
	public async Task EmptyFacetedSourceWithALegendRenders()
	{
		// Arrange

		// The legend path is the one that indexed Panels[0].
		var plot = PlotContext.Build(Array.Empty<Reading>(), i => i.X, i => i.Y)
			.Scale_Color_Discrete(i => i.Tank, ["#111111", "#222222"], name: "tank")
			.Geom_Point()
			.Facet_Wrap(i => i.Tank)
			.Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		svg.Should().Contain("<svg");
	}

	[Fact]
	public async Task RefillingAnEmptyFacetedSourceRendersPanels()
	{
		// Arrange

		var live = new List<Reading>();

		var plot = PlotContext.Build(live, i => i.X, i => i.Y)
			.Geom_Point()
			.Facet_Wrap(i => i.Tank)
			.Style();

		var empty = await plot.AsStringAsync();

		// Act

		live.AddRange(populated);

		var filled = await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		empty.Should().Contain("<svg");
		filled.Should().Contain("<circle");
	}

	[Theory]
	[InlineData(0, null)]
	[InlineData(-1, null)]
	[InlineData(null, 0)]
	[InlineData(null, -2)]
	public void NonPositiveFacetDimensionsThrow(int? nrows, int? ncolumns)
	{
		// Arrange

		var context = PlotContext.Build(populated, i => i.X, i => i.Y);

		// Act

		Action act = () => context.Facet_Wrap(i => i.Tank, nrows: nrows, ncolumns: ncolumns);

		// Assert

		act.Should().Throw<GGNetUserException>();
	}

	[Fact]
	public async Task PositiveFacetDimensionsAreAccepted()
	{
		// Arrange

		var plot = PlotContext.Build(populated, i => i.X, i => i.Y)
			.Geom_Point()
			.Facet_Wrap(i => i.Tank, nrows: 2, ncolumns: 1)
			.Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		svg.Should().Contain("<circle");
	}
}

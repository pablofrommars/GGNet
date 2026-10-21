using GGNet.Scales;

namespace GGNet.Headless.Tests;

public class AestheticInheritanceTests
{
	private sealed record Datum(double X, double Y, string Group);

	private static readonly Datum[] source =
	[
		new(1, 2, "A"), new(2, 4, "A"), new(3, 3, "A"),
		new(1, 3, "B"), new(2, 2, "B"), new(3, 5, "B")
	];

	[Theory]
	[InlineData("Area", true)]
	[InlineData("Bar", true)]
	[InlineData("Boxplot", true)]
	[InlineData("ErrorBar", true)]
	[InlineData("Hex", true)]
	[InlineData("Map", true)]
	[InlineData("Point", true)]
	[InlineData("Radar", true)]
	[InlineData("Ribbon", true)]
	[InlineData("RidgeLine", true)]
	[InlineData("Text", true)]
	[InlineData("Tile", true)]
	[InlineData("Violin", true)]
	public async Task SourcelessContextOptOutMatchesPanelAndSuppressesMappedPaint(string family, bool hasLegend)
	{
		// Arrange

		var inherited = CreateContext();
		var optedOut = CreateContext();
		var panel = CreateContext();
		AddContextLayer(inherited, family, inherit: true);
		AddContextLayer(optedOut, family, inherit: false);
		AddPanelLayer(panel, family);

		// Act

		var inheritedSvg = await inherited.Style().AsStringAsync();
		var optedOutSvg = await optedOut.Style().AsStringAsync();
		var panelSvg = await panel.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		Paints(inherited).Distinct().Should().BeEquivalentTo(["red", "blue"]);
		Paints(optedOut).Should().NotBeEmpty().And.OnlyContain(p => p == "green");
		Paints(panel).Should().Equal(Paints(optedOut));
		inheritedSvg.Contains("legend-label", StringComparison.Ordinal).Should().Be(hasLegend);
		optedOutSvg.Should().NotContain("legend-label");
		panelSvg.Should().NotContain("legend-label");
	}

	[Theory]
	[InlineData("Area")]
	[InlineData("Radar")]
	[InlineData("Ribbon")]
	[InlineData("RidgeLine")]
	[InlineData("Violin")]
	public async Task InheritanceControlsSeriesGeometry(string family)
	{
		// Arrange

		var inherited = CreateContext();
		var optedOut = CreateContext();
		AddContextLayer(inherited, family, inherit: true);
		AddContextLayer(optedOut, family, inherit: false);

		// Act

		await inherited.Style().AsStringAsync();
		await optedOut.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		inherited.Panels[0].Geoms[0].Layer.Should().HaveCount(2);
		optedOut.Panels[0].Geoms[0].Layer.Should().ContainSingle();
	}

	[Fact]
	public async Task PointDefaultsInheritColorAndSizeWhileOptOutRetainsSourceAndPositions()
	{
		// Arrange

		var inherited = CreateContext().Geom_Point();
		var optedOut = CreateContext().Geom_Point(size: 7, color: "green", inherit: false);

		// Act

		await inherited.Style().AsStringAsync();
		var svg = await optedOut.Style().AsStringAsync();
		var mapped = Circles(inherited);
		var constant = Circles(optedOut);

		// Assert

		using var _ = new AssertionScope();
		mapped.Select(p => p.Aesthetic.Fill).Distinct().Should().BeEquivalentTo(["red", "blue"]);
		mapped.Select(p => p.Aesthetic.Radius).Distinct().Should().HaveCountGreaterThan(1);
		constant.Should().HaveCount(source.Length);
		constant.Select(p => (p.X, p.Y)).Should().Equal(source.Select(p => (p.X, p.Y)));
		constant.Should().OnlyContain(p => p.Aesthetic.Radius == 7 && p.Aesthetic.Fill == "green");
		svg.Should().NotContain("legend-label");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ExplicitPointMappingsWinWithEitherInheritanceSetting(bool inherit)
	{
		// Arrange

		var color = new ConstantMapping<string>("purple");
		var size = new ConstantMapping<double>(11);
		var context = CreateContext().Geom_Point(colorBy: color, sizeBy: size, inherit: inherit);

		// Act

		await context.Style().AsStringAsync();

		// Assert

		Circles(context).Should().HaveCount(source.Length)
			.And.OnlyContain(p => p.Aesthetic.Fill == "purple" && p.Aesthetic.Radius == 11);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UnusedContinuousFillDoesNotCreateLegendOrRequireTraining(bool panel)
	{
		// Arrange

		var context = PlotContext.Build(source, p => p.X, p => p.Y)
			.Scale_Fill_Continuous(p => p.Y, ["#ff0000", "#0000ff"]);
		if (panel)
		{
			context.Default_Panel().Geom_Area(fill: "green", inherit: false);
		}
		else
		{
			context.Geom_Area(fill: "green", inherit: false);
		}

		// Act

		var svg = await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		Paints(context).Should().Equal("green");
		svg.Should().NotContain("legend-label").And.NotContain("NaN").And.NotContain("Infinity");
	}

	private static PlotContext<Datum, double, double> CreateContext()
	{
		var context = PlotContext.Build(source, p => p.X, p => p.Y)
			.Scale_Color_Discrete(p => p.Group, ["red", "blue"])
			.Scale_Fill_Discrete(p => p.Group, ["red", "blue"])
			.Scale_Size_Continuous(p => p.Y, range: (2, 10));
		return context;
	}

	private sealed class ConstantMapping<TValue>(TValue value) : IAestheticMapping<Datum, TValue>
	{
		public bool Guide => false;

		public string? Name => null;

		public Guide Type => GGNet.Guide.None;

		public IEnumerable<(TValue value, string label)> Labels => [];

		public void Train(Datum item) { }

		public TValue Map(Datum item) => value;
	}

	private static Shapes.Circle[] Circles(PlotContext<Datum, double, double> context)
		=> context.Panels[0].Geoms[0].Layer.Select(p => p.Value).OfType<Shapes.Circle>().ToArray();

	private static IEnumerable<string?> Paints(PlotContext<Datum, double, double> context)
		=> context.Panels[0].Geoms[0].Layer.Select(p => p.Value switch
		{
			Shapes.Area area => area.Aesthetic.Fill,
			Shapes.Circle circle => circle.Aesthetic.Fill,
			Shapes.Rectangle rectangle => rectangle.Aesthetic.Fill,
			Shapes.Polygon polygon => polygon.Aesthetic.Fill,
			Shapes.MultiPolygon polygon => polygon.Aesthetic.Fill,
			Shapes.Text text => text.Aesthetic.Color,
			_ => null
		}).Where(p => p is not null);

	private static Geospacial.Polygon[] Polygons(Datum item)
		=> [new() { Longitude = [item.X, item.X + 0.5, item.X], Latitude = [item.Y, item.Y, item.Y + 0.5] }];

	private static void AddContextLayer(PlotContext<Datum, double, double> context, string family, bool inherit)
	{
		switch (family)
		{
			case "Area":
				context.Geom_Area(fill: "green", inherit: inherit);
				break;
			case "Bar":
				context.Geom_Bar(fill: "green", inherit: inherit);
				break;
			case "Boxplot":
				context.Geom_Boxplot(y: _ => 1.0, fill: "green", inherit: inherit);
				break;
			case "ErrorBar":
				context.Geom_ErrorBar(ymin: p => p.Y - 1, ymax: p => p.Y + 1, color: "green", inherit: inherit);
				break;
			case "Hex":
				context.Geom_Hex(dx: _ => 0.2, dy: _ => 0.2, fill: "green", inherit: inherit);
				break;
			case "Map":
				context.Geom_Map(Polygons, fill: "green", inherit: inherit);
				break;
			case "Point":
				context.Geom_Point(color: "green", inherit: inherit);
				break;
			case "Radar":
				context.Geom_Radar(fill: "green", inherit: inherit);
				break;
			case "Ribbon":
				context.Geom_Ribbon(ymin: p => p.Y - 1, ymax: p => p.Y + 1, fill: "green", inherit: inherit);
				break;
			case "RidgeLine":
				context.Geom_RidgeLine(y: _ => 1.0, height: p => p.Y, fill: "green", inherit: inherit);
				break;
			case "Text":
				context.Geom_Text(text: p => p.Group, color: "green", inherit: inherit);
				break;
			case "Tile":
				context.Geom_Tile(p => p.X, p => p.Y, _ => 0.2, _ => 0.2, fill: "green", inherit: inherit);
				break;
			case "Violin":
				context.Geom_Violin(x: _ => 1.0, width: p => p.Y / 10, fill: "green", inherit: inherit);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(family));
		}
	}

	private static void AddPanelLayer(PlotContext<Datum, double, double> context, string family)
	{
		var panel = context.Default_Panel();
		switch (family)
		{
			case "Area":
				panel.Geom_Area(fill: "green", inherit: false);
				break;
			case "Bar":
				panel.Geom_Bar(fill: "green", inherit: false);
				break;
			case "Boxplot":
				panel.Geom_Boxplot(y: _ => 1.0, fill: "green", inherit: false);
				break;
			case "ErrorBar":
				panel.Geom_ErrorBar(ymin: p => p.Y - 1, ymax: p => p.Y + 1, color: "green", inherit: false);
				break;
			case "Hex":
				panel.Geom_Hex(dx: _ => 0.2, dy: _ => 0.2, fill: "green", inherit: false);
				break;
			case "Map":
				panel.Geom_Map(Polygons, fill: "green", inherit: false);
				break;
			case "Point":
				panel.Geom_Point(color: "green", inherit: false);
				break;
			case "Radar":
				panel.Geom_Radar(fill: "green", inherit: false);
				break;
			case "Ribbon":
				panel.Geom_Ribbon(ymin: p => p.Y - 1, ymax: p => p.Y + 1, fill: "green", inherit: false);
				break;
			case "RidgeLine":
				panel.Geom_RidgeLine(y: _ => 1.0, height: p => p.Y, fill: "green", inherit: false);
				break;
			case "Text":
				panel.Geom_Text(text: p => p.Group, color: "green", inherit: false);
				break;
			case "Tile":
				panel.Geom_Tile(p => p.X, p => p.Y, _ => 0.2, _ => 0.2, fill: "green", inherit: false);
				break;
			case "Violin":
				panel.Geom_Violin(x: _ => 1.0, width: p => p.Y / 10, fill: "green", inherit: false);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(family));
		}
	}
}

using GGNet.Scales;

namespace GGNet.Headless.Tests;

public class TextBoxplotMappingTests
{
	private sealed record Datum(double X, double Y, string Group);

	private static readonly Datum[] source =
	[
		new(1, 1, "A"), new(2, 1, "A"), new(3, 1, "A"),
		new(4, 1, "B"), new(5, 1, "B"), new(6, 1, "B")
	];

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task StandaloneTextTrainsAllColorsRegardlessOfGuide(bool guide)
	{
		// Arrange

		var context = PlotContext.Build(source, p => p.X, p => p.Y)
			.Scale_Color_Discrete(p => p.Group, ["red", "blue"], guide: guide)
			.Geom_Text(text: p => p.Group, color: "green");

		// Act

		var svg = await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		Texts(context).Select(p => (p.X, p.Y, p.Value, p.Aesthetic.Color)).Should()
			.Equal(source.Select(p => (p.X, p.Y, (string?)p.Group, p.Group == "A" ? "red" : "blue")));
		LegendLabels(svg).Should().Equal(guide ? ["A", "B"] : []);
		context.Legends.Should().HaveCount(guide ? 1 : 0);
	}

	[Fact]
	public async Task TextTrainsCategoriesMissingFromThePointLayerSharingItsMapping()
	{
		// Arrange

		var context = PlotContext.Build(source, p => p.X, p => p.Y)
			.Scale_Color_Discrete(p => p.Group, ["red", "blue"]);
		context.Geom_Point(source[..3], p => p.X, p => p.Y, colorBy: context.Aesthetics.Color)
			.Geom_Text(text: p => p.Group);

		// Act

		var svg = await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		Texts(context, 1).Select(p => p.Aesthetic.Color).Should().Equal("red", "red", "red", "blue", "blue", "blue");
		context.Panels[0].Geoms[0].Layer.Should().HaveCount(3);
		context.Legends.Should().ContainSingle();
		LegendLabels(svg).Should().Equal("A", "B");
		context.Legends[0].Items.Should().OnlyContain(p => p.elements.Count == 2);
	}

	[Fact]
	public async Task TextRetrainsChangedSourceAndRebuildsItsLegend()
	{
		// Arrange

		var rows = source.ToList();
		var context = PlotContext.Build(rows, p => p.X, p => p.Y)
			.Scale_Color_Discrete(p => p.Group, ["red", "blue"])
			.Geom_Text(text: p => p.Group);
		await context.Style().AsStringAsync();
		rows.Clear();
		rows.Add(new(9, 2, "C"));

		// Act

		var svg = await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		Texts(context).Should().ContainSingle().Which.Should().Match<Shapes.Text>(p =>
			p.X == 9 && p.Y == 2 && p.Value == "C" && p.Aesthetic.Color == "red");
		LegendLabels(svg).Should().Equal("C");
		context.Legends.Should().ContainSingle().Which.Items.Should().ContainSingle();
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task ExplicitCustomMappingWinsAndReceivesTraining(bool boxplot, bool inherit)
	{
		// Arrange

		var mapping = new RecordingMapping();
		var context = PlotContext.Build(source, p => p.X, p => p.Y)
			.Scale_Color_Discrete(p => p.Group, ["red", "blue"])
			.Scale_Fill_Discrete(p => p.Group, ["red", "blue"]);
		if (boxplot)
		{
			context.Geom_Boxplot(fillBy: mapping, fill: "green", inherit: inherit);
		}
		else
		{
			context.Geom_Text(text: p => p.Group, colorBy: mapping, color: "green", inherit: inherit);
		}

		// Act

		var svg = await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		mapping.Trained.Should().Equal(source);
		context.Legends.Should().ContainSingle().Which.Aes.Should().BeSameAs(mapping);
		LegendLabels(svg).Should().Equal("custom");
		Paints(context, boxplot).Should().NotBeEmpty().And.OnlyContain(p => p == "purple");
	}

	[Fact]
	public async Task MissingMappedTextColorStillSkipsThatLabelInsteadOfUsingConstantColor()
	{
		// Arrange

		var mapping = new RecordingMapping(skipB: true);
		var context = PlotContext.Build(source, p => p.X, p => p.Y)
			.Geom_Text(text: p => p.Group, colorBy: mapping, color: "green");

		// Act

		await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		mapping.Trained.Should().Equal(source);
		Texts(context).Should().HaveCount(3).And.OnlyContain(p => p.Value == "A" && p.Aesthetic.Color == "purple");
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task IdentityAndConstantPaintKeepTheirNoLegendBehavior(bool boxplot, bool identity)
	{
		// Arrange

		var context = PlotContext.Build(source, p => p.X, p => p.Y);
		if (identity)
		{
			context.Scale_Color_Identity(_ => "purple").Scale_Fill_Identity(_ => "purple");
		}
		if (boxplot)
		{
			context.Geom_Boxplot(fill: "green");
		}
		else
		{
			context.Geom_Text(text: p => p.Group, color: "green");
		}

		// Act

		var svg = await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		Paints(context, boxplot).Should().NotBeEmpty().And.OnlyContain(p => p == (identity ? "purple" : "green"));
		context.Legends.Should().BeEmpty();
		LegendLabels(svg).Should().BeEmpty();
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task BoxplotLegendMatchesPaintAndDoesNotChangeDataGeometry(bool guide)
	{
		// Arrange

		var context = BoxplotContext(guide);
		var control = BoxplotContext(false);
		await control.Style().AsStringAsync();

		// Act

		var svg = await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		context.Panels[0].Geoms[0].Layer.Should().Equal(control.Panels[0].Geoms[0].Layer);
		Paints(context, true).Should().Equal("red", "blue");
		LegendLabels(svg).Should().Equal(guide ? ["A", "B"] : []);
		if (guide)
		{
			Swatches(context).Should().Equal(
				new Elements.Rectangle { Fill = "red", FillOpacity = 0.4, Stroke = "red", StrokeOpacity = 1, StrokeWidth = 2.5 },
				new Elements.Rectangle { Fill = "blue", FillOpacity = 0.4, Stroke = "blue", StrokeOpacity = 1, StrokeWidth = 2.5 });
		}
		else
		{
			context.Legends.Should().BeEmpty();
		}
	}

	[Fact]
	public async Task TextSwatchUsesTheTextOpacity()
	{
		// Arrange

		var context = PlotContext.Build(source, p => p.X, p => p.Y)
			.Scale_Color_Discrete(p => p.Group, ["red", "blue"])
			.Geom_Text(text: p => p.Group);
		await context.Style().AsStringAsync();
		var geom = (Geoms.Text.Text<Datum, double, double, string>)context.Panels[0].Geoms[0];
		geom.Aesthetic = geom.Aesthetic with { Opacity = 0.35 };

		// Act

		await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		Texts(context).Should().OnlyContain(p => p.Aesthetic.Opacity == 0.35);
		Swatches(context).Should().Equal(
			new Elements.Rectangle { Fill = "red", FillOpacity = 0.35 },
			new Elements.Rectangle { Fill = "blue", FillOpacity = 0.35 });
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SharedMappingsCreateOneLegendWithOneLabelPerCategory(bool boxplot)
	{
		// Arrange

		var context = PlotContext.Build(source, p => p.X, p => p.Y)
			.Scale_Color_Discrete(p => p.Group, ["red", "blue"])
			.Scale_Fill_Discrete(p => p.Group, ["red", "blue"]);
		if (boxplot)
		{
			context.Geom_Boxplot().Geom_Bar();
		}
		else
		{
			context.Geom_Text(text: p => p.Group).Geom_Point();
		}

		// Act

		var svg = await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		context.Legends.Should().ContainSingle();
		context.Legends[0].Items.Should().HaveCount(2).And.OnlyContain(p => p.elements.Count == 2);
		LegendLabels(svg).Should().Equal("A", "B");
	}

	[Fact]
	public async Task ContinuousBoxplotFillCreatesAColorBarWithMatchingStopsAndOpacity()
	{
		// Arrange

		var context = PlotContext.Build(source, p => p.X, p => p.Y)
			.Scale_Fill_Continuous(p => p.X, ["#ff0000", "#0000ff"])
			.Geom_Boxplot(fill: "green", fillOpacity: 0.4, strokeWidth: 2.5);
		var control = PlotContext.Build(source, p => p.X, p => p.Y)
			.Scale_Fill_Continuous(p => p.X, ["#ff0000", "#0000ff"])
			.Geom_Bar(fillOpacity: 0.4);
		var controlSvg = await control.Style().AsStringAsync();
		var controlStops = XDocument.Parse(controlSvg).Descendants().Where(p => p.Name.LocalName == "stop");

		// Act

		var svg = await context.Style().AsStringAsync();
		var stops = XDocument.Parse(svg).Descendants().Where(p => p.Name.LocalName == "stop").ToArray();

		// Assert

		using var _ = new AssertionScope();
		context.Legends.Should().ContainSingle().Which.Aes.Type.Should().Be(Guide.ColorBar);
		var labels = context.Aesthetics.Fill!.Labels.ToArray();
		LegendLabels(svg).Should().Equal(labels.Select(p => p.label));
		stops.Select(p => p.ToString()).Should().Equal(controlStops.Select(p => p.ToString()));
		stops.Select(p => (string?)p.Attribute("stop-color")).Distinct().Should().BeEquivalentTo(["#ff0000", "#0000ff"]);
		stops.Should().OnlyContain(p => (string?)p.Attribute("stop-opacity") == "0.4");
		Paints(context, true).Should().Equal("#ff0000", "#0000ff");
		Swatches(context).Should().OnlyContain(p => p.FillOpacity == 0.4 && p.Stroke == p.Fill && p.StrokeWidth == 2.5);
		svg.Should().NotContain("NaN").And.NotContain("Infinity");
	}

	private static PlotContext<Datum, double, double> BoxplotContext(bool guide)
		=> PlotContext.Build(source, p => p.X, p => p.Y)
			.Scale_Fill_Discrete(p => p.Group, ["red", "blue"], guide: guide)
			.Geom_Boxplot(fill: "green", fillOpacity: 0.4, strokeWidth: 2.5);

	private static Shapes.Text[] Texts(PlotContext<Datum, double, double> context, int layer = 0)
		=> context.Panels[0].Geoms[layer].Layer.Select(p => p.Value).OfType<Shapes.Text>().ToArray();

	private static IEnumerable<string> Paints(PlotContext<Datum, double, double> context, bool boxplot)
		=> boxplot
			? context.Panels[0].Geoms[0].Layer.Select(p => p.Value).OfType<Shapes.Rectangle>().Select(p => p.Aesthetic.Fill)
			: Texts(context).Select(p => p.Aesthetic.Color);

	private static IEnumerable<string> LegendLabels(string svg)
		=> XDocument.Parse(svg).Descendants().Where(p => (string?)p.Attribute("class") == "legend-label").Select(p => p.Value);

	private static IEnumerable<Elements.Rectangle> Swatches(PlotContext<Datum, double, double> context)
		=> context.Legends.SelectMany(p => p.Items).SelectMany(p => p.elements).Select(p => p.Value.Value).OfType<Elements.Rectangle>();

	private sealed class RecordingMapping(bool skipB = false) : IAestheticMapping<Datum, string>
	{
		public List<Datum> Trained { get; } = [];

		public bool Guide => true;

		public string? Name => "custom mapping";

		public Guide Type => GGNet.Guide.Items;

		public IEnumerable<(string value, string label)> Labels => [("purple", "custom")];

		public void Train(Datum item) => Trained.Add(item);

		public string Map(Datum item) => !Trained.Contains(item)
			? throw new InvalidOperationException("Mapping must be trained before mapping a row.")
			: skipB && item.Group == "B" ? string.Empty : "purple";
	}
}

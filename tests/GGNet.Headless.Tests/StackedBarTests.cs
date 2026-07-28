namespace GGNet.Headless.Tests;

// Stack() kept a single signed accumulator and passed the value straight through as a rectangle
// height, so a negative segment produced a negative SVG height (not drawn) and the y extent was
// trained over the running total only. It also trained x over x ± delta, twice the x ± delta/2
// the rectangles actually span.
public class StackedBarTests
{
	private sealed record Flow(double Period, string Kind, double Value);

	private static readonly Flow[] mixed =
	[
		new(1.0, "in", 3.0), new(1.0, "out", -2.0),
		new(2.0, "in", 5.0), new(2.0, "out", -4.0),
		new(3.0, "in", 2.0), new(3.0, "out", -6.0)
	];

	private static async Task<(double x, double y, double width, double height)[]> Bars(IPlotContext plot)
	{
		var svg = await plot.AsStringAsync();

		return [.. XDocument.Parse(svg).Descendants()
			.Where(element => element.Name.LocalName == "rect" && element.Attribute("fill") is not null)
			.Select(element => (
				Value(element, "x"),
				Value(element, "y"),
				Value(element, "width"),
				Value(element, "height")))];
	}

	private static async Task<(double x, double width)> Panel(IPlotContext plot)
	{
		var svg = await plot.AsStringAsync();

		var panel = XDocument.Parse(svg).Descendants()
			.First(element => element.Name.LocalName == "rect" && element.Attribute("class")?.Value == "panel");

		return (Value(panel, "x"), Value(panel, "width"));
	}

	private static double Value(XElement element, string name)
		=> double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);

	[Fact]
	public async Task MixedSignSegmentsAllRenderWithNonNegativeHeight()
	{
		// Arrange

		var plot = PlotContext.Build(mixed, i => i.Period, i => i.Value)
			.Scale_Fill_Discrete(i => i.Kind, ["#111111", "#222222"], guide: false)
			.Geom_Bar()
			.Style();

		// Act

		var bars = await Bars(plot);

		// Assert

		using var _ = new AssertionScope();

		bars.Should().HaveCount(6);
		bars.Should().OnlyContain(bar => bar.height >= 0.0);
		bars.Should().OnlyContain(bar => bar.width > 0.0);
	}

	[Fact]
	public async Task MixedSignStackStaysInsideTheTrainedExtent()
	{
		// Arrange

		var plot = PlotContext.Build(mixed, i => i.Period, i => i.Value)
			.Scale_Fill_Discrete(i => i.Kind, ["#111111", "#222222"], guide: false)
			.Geom_Bar()
			.Style();

		// Act

		var bars = await Bars(plot);
		var (panelX, panelWidth) = await Panel(plot);

		// Assert

		// Both the +5 top and the −6 bottom are trained, so no segment overflows the panel.
		using var _ = new AssertionScope();

		bars.Should().OnlyContain(bar => bar.y >= -1e-9);
		bars.Should().OnlyContain(bar => bar.y + bar.height <= 556.0 + 1e-9);
		bars.Should().OnlyContain(bar => bar.x >= panelX - 1e-9);
		bars.Should().OnlyContain(bar => bar.x + bar.width <= panelX + panelWidth + 1e-9);
	}

	private sealed record Single(double At, double Value);

	[Fact]
	public async Task StackedExtentEqualsTheDrawnWidth()
	{
		// Arrange

		// One bar: delta is the raw width (0.8). The bar spans 0.8 and the trained extent is
		// the same 0.8 plus the scale's 5% expansion at each end, so the bar fills
		// 0.8 / (0.8 * 1.1) of the panel. Training x ± delta gave it 0.8 / 1.76 instead.
		var plot = PlotContext.Build([new Single(1.0, 4.0)], i => i.At, i => i.Value)
			.Geom_Bar()
			.Style();

		// Act

		var bars = await Bars(plot);
		var (_, panelWidth) = await Panel(plot);

		// Assert

		using var _ = new AssertionScope();

		bars.Should().HaveCount(1);
		(bars[0].width / panelWidth).Should().BeApproximately(1.0 / 1.1, 1e-9);
	}

	[Fact]
	public async Task AllPositiveStackIsUnaffectedByTheSignSplit()
	{
		// Arrange

		var positive = mixed.Select(i => i with { Value = Math.Abs(i.Value) }).ToArray();

		var plot = PlotContext.Build(positive, i => i.Period, i => i.Value)
			.Scale_Fill_Discrete(i => i.Kind, ["#111111", "#222222"], guide: false)
			.Geom_Bar()
			.Style();

		// Act

		var bars = await Bars(plot);

		// Assert

		// Period 1 stacks 3 + 2; nothing is drawn below the baseline.
		using var _ = new AssertionScope();

		bars.Should().HaveCount(6);
		bars.Should().OnlyContain(bar => bar.height >= 0.0);
	}
}

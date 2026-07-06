namespace GGNet.Headless.Tests;

// Golden gallery: one snapshot per geom. First recording is characterization
// (pins current behavior); thereafter regression. Snapshots live in Gallery/
// as .verified.svg — open them in a browser to review.
public class GalleryTests
{
	private sealed record XY(double X, double Y);

	private static readonly XY[] xy =
	[
		new(1, 2.0),
		new(2, 3.5),
		new(3, 2.8),
		new(4, 4.2),
		new(5, 3.1)
	];

	private sealed record Grouped(double Group, double Value);

	private static readonly Grouped[] grouped =
	[
		new(1, 2.1), new(1, 2.4), new(1, 2.2), new(1, 3.0), new(1, 2.7), new(1, 2.5),
		new(2, 3.8), new(2, 4.1), new(2, 3.5), new(2, 4.4), new(2, 3.9), new(2, 4.0)
	];

	private sealed record DensityPoint(double Group, double At, double Density);

	private static readonly DensityPoint[] profile =
	[
		.. from g in new[] { (mu: 2.5, x: 1.0), (mu: 4.0, x: 2.0) }
		   from step in Enumerable.Range(0, 13)
		   let y = g.mu - 1.5 + step * 0.25
		   select new DensityPoint(g.x, y, Math.Exp(-Math.Pow(y - g.mu, 2) / 0.5))
	];

	private sealed record Candle(double T, double Open, double High, double Low, double Close, double Volume);

	private static readonly Candle[] candles =
	[
		new(1, 10.0, 12.0, 9.5, 11.5, 1000),
		new(2, 11.5, 13.0, 11.0, 12.5, 1400),
		new(3, 12.5, 12.8, 10.5, 10.8, 900),
		new(4, 10.8, 11.5, 10.2, 11.2, 1200),
		new(5, 11.2, 14.0, 11.0, 13.8, 1800)
	];

	public enum Metric { Speed, Power, Range, Weight, Cost }

	private sealed record Rated(Metric Metric, double Value);

	private static readonly Rated[] rated =
	[
		new(Metric.Speed, 3.0),
		new(Metric.Power, 4.0),
		new(Metric.Range, 2.0),
		new(Metric.Weight, 5.0),
		new(Metric.Cost, 1.0)
	];

	private sealed record Region(Geospacial.Polygon[] Shapes);

	private static readonly Region[] regions =
	[
		new([
			new() { Longitude = [0.0, 2.0, 1.0], Latitude = [0.0, 0.0, 2.0] },
			new() { Longitude = [3.0, 5.0, 5.0, 3.0], Latitude = [1.0, 1.0, 3.0, 3.0] }
		])
	];

	private static async Task VerifyPlot(IPlotContext plot)
	{
		var svg = await plot.AsStringAsync();

		// Static export must be pure, well-formed SVG — not an HTML fragment.
		XDocument.Parse(svg);

		await Verifier.Verify(svg, extension: "svg");
	}

	[Fact]
	public Task Point()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Point().Style());

	[Fact]
	public Task Line()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Line().Style());

	[Fact]
	public Task Bar()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Bar().Style());

	private sealed record Reading(double Value, double Tank);

	private static readonly Reading[] readings =
	[
		.. from tank in new[] { 1.0, 2.0 }
		   from i in Enumerable.Range(0, 40)
		   let center = tank == 1.0 ? 3.0 : 6.0
		   select new Reading(center + Math.Sin(i * 2.399) * (1.0 + i % 3), tank)
	];

	[Fact]
	public Task Histogram()
		// The stats acid test: Stat.Bin + Geom_Bar composes a histogram —
		// no Histogram geom exists.
		=> VerifyPlot(PlotContext.Build(Stat.Bin(readings, r => r.Value, bins: 12), b => b.Mid, b => b.Count)
			.Geom_Bar(width: 1.0)
			.Style());

	[Fact]
	public Task HistogramFaceted()
		// Per-facet statistics are grouped statistics: bin per group, facet
		// the output on the same key.
		=> VerifyPlot(PlotContext.Build(Stat.Bin(readings, r => r.Value, r => r.Tank, bins: 10), b => b.Mid, b => b.Count)
			.Geom_Bar(width: 1.0)
			.Facet_Wrap(b => b.Group)
			.Style());

	[Fact]
	public Task DensityArea()
		=> VerifyPlot(PlotContext.Build(Stat.Density(readings, r => r.Value, n: 128), d => d.At, d => d.Density)
			.Geom_Area()
			.Style());

	[Fact]
	public Task ViolinFromDensity()
		// Stat.Density is the producer Geom_Violin's precomputed-profile
		// contract always wanted.
		=> VerifyPlot(PlotContext.Build(Stat.Density(readings, r => r.Value, r => r.Tank, n: 64), d => d.Group, d => d.At)
			.Geom_Violin(width: d => d.Density)
			.Style());

	[Fact]
	public Task SummaryErrorBar()
		=> VerifyPlot(PlotContext.Build(Stat.Summary(readings, r => r.Tank, r => r.Value), s => s.X, s => s.Center)
			.Geom_ErrorBar(ymin: s => s.Lower, ymax: s => s.Upper)
			.Style());

	[Fact]
	public Task BarFlipped()
		// Pins Flip() output ahead of the coord-strategy absorption (session D).
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Bar().Flip().Style());

	private sealed record Grouped2(double Pos, double Value, double Series);

	private static readonly Grouped2[] dodged =
	[
		new(1, 2.0, 1), new(1, 3.0, 2),
		new(2, 4.0, 1), new(2, 1.0, 2),
		new(3, 3.0, 1), new(3, 2.0, 2)
	];

	[Fact]
	public Task BarFlippedDodged()
		// Flipped Dodge threw NotImplementedException before the flip absorption;
		// this pins the capability that fell out of the unified emission path.
		=> VerifyPlot(PlotContext.Build(dodged, i => i.Pos, i => i.Value)
			.Scale_Fill_Discrete(i => i.Series, ["#23d0fc", "#fc9d23"])
			.Geom_Bar(position: PositionAdjustment.Dodge)
			.Flip()
			.Style());

	[Fact]
	public Task Area()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Area().Style());

	[Fact]
	public Task Ribbon()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Ribbon(ymin: i => i.Y - 0.5, ymax: i => i.Y + 0.5).Style());

	[Fact]
	public Task ErrorBar()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_ErrorBar(ymin: i => i.Y - 0.4, ymax: i => i.Y + 0.4).Style());

	[Fact]
	public Task Segment()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Segment(i => i.X, i => i.X + 0.5, i => i.Y, i => i.Y + 0.3).Style());

	[Fact]
	public Task Text()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Text(text: i => $"p{i.X}").Style());

	[Fact]
	public Task Tile()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Tile(i => i.X, i => i.Y, i => 0.9, i => 0.8).Style());

	[Fact]
	public Task Hex()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Hex(dx: i => 0.5, dy: i => 0.4).Style());

	[Fact]
	public Task Boxplot()
		// Boxplot is horizontal: x carries the measurements, y the category.
		=> VerifyPlot(PlotContext.Build(grouped, i => i.Value, i => i.Group).Geom_Boxplot().Style());

	[Fact]
	public Task Violin()
		// Violin consumes a precomputed profile: width is the density at each y.
		=> VerifyPlot(PlotContext.Build(profile, i => i.Group, i => i.At)
			.Geom_Violin(width: i => i.Density).Style());

	[Fact]
	public Task RidgeLine()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_RidgeLine(height: i => 0.8).Style());

	[Fact]
	public Task Candlestick()
		=> VerifyPlot(PlotContext.Build(candles, i => i.T, i => i.Close)
			.Geom_Candlestick(i => i.T, i => i.Open, i => i.High, i => i.Low, i => i.Close).Style());

	[Fact]
	public Task OHLC()
		=> VerifyPlot(PlotContext.Build(candles, i => i.T, i => i.Close)
			.Geom_OHLC(i => i.T, i => i.Open, i => i.High, i => i.Low, i => i.Close).Style());

	[Fact]
	public Task Volume()
		=> VerifyPlot(PlotContext.Build(candles, i => i.T, i => i.Volume)
			.Geom_Volume(volume: i => i.Volume).Style());

	[Fact]
	public Task VLine()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Line()
			.Geom_VLine(new[] { 2.5 }, v => v, v => "mark").Style());

	[Fact]
	public Task HLine()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Line()
			.Geom_HLine(new[] { 3.0 }, v => v, v => "level").Style());

	[Fact]
	public Task ABLine()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Point()
			.Geom_ABLine(new[] { (a: 0.5, b: 1.0) }, i => i.a, i => i.b, i => "trend").Style());

	[Fact]
	public Task Radar()
		=> VerifyPlot(PlotContext.Build(rated, i => i.Metric, i => i.Value).Geom_Radar().Style());

	[Fact]
	public Task Map()
		=> VerifyPlot(PlotContext.Build(regions)
			.Geom_Map(r => r.Shapes).Style());
}

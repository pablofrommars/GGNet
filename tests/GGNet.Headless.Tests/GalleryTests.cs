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

	// The inner ring is supplied with the same winding as the exterior: only the Hole flag
	// distinguishes them, which is exactly what composition has to honor.
	private static readonly Region[] holed =
	[
		new([
			new() { Longitude = [0.0, 10.0, 10.0, 0.0], Latitude = [0.0, 0.0, 10.0, 10.0] },
			new() { Longitude = [3.0, 7.0, 7.0, 3.0], Latitude = [3.0, 3.0, 7.0, 7.0], Hole = true }
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
	public async Task SelfContained()
	{
		// Self-contained export embeds the theme css; pinned separately so the
		// default lean output stays byte-stable.
		var svg = await PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Point().Style()
			.AsStringAsync(selfContained: true);

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
	public Task LegendLineVertical()
		// Pins the vertical-legend line-swatch path (LegendVHLine), where the
		// x1=Legend.Y axis typo lived unexercised until this snapshot.
		=> VerifyPlot(PlotContext.Build(dodged, i => i.Pos, i => i.Value)
			.Scale_Color_Discrete(i => i.Series, ["#23d0fc", "#fc9d23"])
			.Geom_Line()
			.Style());

	[Fact]
	public Task LegendBarHorizontal()
		// Pins the horizontal-legend rectangle-swatch path (LegendHRect), where
		// stroke was bound to StrokeOpacity.
		=> VerifyPlot(PlotContext.Build(dodged, i => i.Pos, i => i.Value)
			.Scale_Fill_Discrete(i => i.Series, ["#23d0fc", "#fc9d23"])
			.Geom_Bar(position: PositionAdjustment.Dodge)
			.Style(legend: Position.Top));

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
	public Task DateTimeAxis()
		// The datetime path renders nothing pinned otherwise: its scale derives a minute grid
		// that the pipeline reads before Commit, which is where a regression slipped through.
		=> VerifyPlot(PlotContext.Build(timed, i => i.At, i => i.Value)
			.Geom_Line()
			.Geom_Point()
			.Style());

	[Fact]
	public Task Map()
		=> VerifyPlot(PlotContext.Build(regions)
			.Geom_Map(r => r.Shapes).Style());

	[Fact]
	public Task MapWithHole()
		// Hole rings are wound against their exterior at compose time, so the cut-out
		// renders under SVG's default nonzero fill regardless of the source's winding.
		=> VerifyPlot(PlotContext.Build(holed)
			.Geom_Map(r => r.Shapes).Style());

	[Fact]
	public Task BarStacked()
		// Stack is Geom_Bar's default position; the fill scale supplies the series.
		=> VerifyPlot(PlotContext.Build(dodged, i => i.Pos, i => i.Value)
			.Scale_Fill_Discrete(i => i.Series, ["#23d0fc", "#fc9d23"])
			.Geom_Bar()
			.Style());

	[Fact]
	public Task AreaStacked()
		=> VerifyPlot(PlotContext.Build(dodged, i => i.Pos, i => i.Value)
			.Scale_Fill_Discrete(i => i.Series, ["#23d0fc", "#fc9d23"])
			.Geom_Area(position: PositionAdjustment.Stack)
			.Style());

	[Fact]
	public Task GroupedScatter()
		=> VerifyPlot(PlotContext.Build(dodged, i => i.Pos, i => i.Value)
			.Scale_Color_Discrete(i => i.Series, ["#23d0fc", "#fc9d23"])
			.Geom_Point()
			.Style());

	[Fact]
	public Task Bubble()
		// range is the radius in pixels; the default (0, 1) renders sub-pixel bubbles.
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y)
			.Scale_Size_Continuous(i => i.Y, range: (3, 9))
			.Geom_Point()
			.Style());

	[Fact]
	public Task Lollipop()
		// A lollipop is a stem segment plus a point tip — two layers, one source.
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Segment(i => i.X, i => i.X, i => 0.0, i => i.Y)
			.Geom_Point()
			.Style());

	[Fact]
	public Task ConnectedScatter()
		=> VerifyPlot(PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Line()
			.Geom_Point()
			.Style());

	private sealed record Change(string Name, double Before, double After);

	private static readonly Change[] changes =
	[
		new("A", 2.0, 3.5),
		new("B", 4.0, 2.5),
		new("C", 3.0, 3.2)
	];

	[Fact]
	public Task Slope()
		=> VerifyPlot(PlotContext.Build(changes, c => 0.0, c => c.Before)
			.Geom_Segment(c => 0.0, c => 1.0, c => c.Before, c => c.After)
			.Geom_Point()
			.Geom_Point(x: c => 1.0, y: c => c.After)
			.Geom_Text(x: c => 1.1, y: c => c.After, text: c => c.Name, anchor: Anchor.Start)
			.Style());

	private sealed record Day(double Week, double Weekday, double Value);

	private static readonly Day[] days =
	[
		.. from w in Enumerable.Range(0, 8)
		   from d in Enumerable.Range(0, 7)
		   select new Day(w, d, Math.Sin(w * 1.7 + d) * Math.Cos(d * 0.9))
	];

	[Fact]
	public Task CalendarHeatmap()
		// Composable recipe: the caller computes the week/weekday grid; GGNet draws tiles.
		=> VerifyPlot(PlotContext.Build(days, d => d.Week, d => d.Weekday)
			.Scale_Fill_Continuous(d => d.Value, ["#132b43", "#56b1f7"])
			.Geom_Tile(d => d.Week, d => d.Weekday, d => 0.95, d => 0.95)
			.Style());

	private sealed record Cell(double Row, double Column, double R);

	private static readonly Cell[] correlations =
	[
		.. from r in Enumerable.Range(0, 3)
		   from c in Enumerable.Range(0, 3)
		   select new Cell(r, c, r == c ? 1.0 : Math.Round(Math.Cos((r + 1) * (c + 1)), 2))
	];

	[Fact]
	public Task Correlogram()
		// Composable recipe: the caller computes the pairwise matrix; GGNet draws tiles.
		=> VerifyPlot(PlotContext.Build(correlations, c => c.Column, c => c.Row)
			.Scale_Fill_Continuous(c => c.R, ["#b2182b", "#f7f7f7", "#2166ac"])
			.Geom_Tile(c => c.Column, c => c.Row, c => 0.95, c => 0.95)
			.Style());

	private sealed record Timed(LocalDateTime At, double Value);

	// A LocalDateTime axis: samples 30 minutes apart across a midnight boundary, so the
	// golden pins the minute-sampled index grid, the half-hour breaks and the date titles.
	private static readonly Timed[] timed =
	[
		.. from step in Enumerable.Range(0, 6)
		   let at = new LocalDateTime(2026, 7, 1, 22, 30).PlusMinutes(30 * step)
		   select new Timed(at, 2.0 + (step % 3) * 0.6)
	];

	private sealed record Site(double Longitude, double Latitude, double Value);

	private static readonly Site[] sites =
	[
		new(0.8, 0.6, 2.0),
		new(4.0, 2.0, 5.0),
		new(4.6, 1.4, 3.0)
	];

	[Fact]
	public Task BubbleMap()
		// Sites are the primary source so the size scale can train on them;
		// the map polygons come in as a secondary layer source.
		=> VerifyPlot(PlotContext.Build(sites, s => s.Longitude, s => s.Latitude)
			.Scale_Size_Continuous(s => s.Value, range: (4, 12))
			.Geom_Map(regions, r => r.Shapes)
			.Geom_Point()
			.Style());
}

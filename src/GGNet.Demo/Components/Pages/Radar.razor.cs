using GGNet.Formats;

namespace GGNet.Demo.Components.Pages;

public partial class Radar
{
	// GGNet.RenderMode: named here (not in markup) to avoid the ambiguity with
	// Microsoft.AspNetCore.Components.Web.RenderMode imported by _Imports.razor.
	private readonly RenderMode renderMode = RenderMode.Interactive;

	// The maturity ladder: 0 unrated, 1 emerging, 2 developing, 3 strong, 4 investor-ready.
	private const double Ladder = 4.0;

	// An unrated axis still needs a visible vertex, so it sits just off the centre.
	private const double Unrated = 0.35;

	private const string Accent = "#3b76d8";

	private enum Axis
	{
		Market,
		Product,
		Team,
		Financial,
		Legal,
		Pitch
	}

	private sealed record Dimension(Axis Axis, double Score, string Status, bool Rated, bool Verified);

	// Radar draws one closed polygon; the outline is six explicit chords so an
	// edge touching an unverified axis can be dashed on its own.
	private sealed record Edge(Axis From, Axis To, double FromScore, double ToScore, bool Verified);

	private static readonly Dimension[] dimensions =
	[
		new(Axis.Market, 2.0, "Developing", Rated: true, Verified: true),
		new(Axis.Product, 3.0, "Strong signal", Rated: true, Verified: true),
		new(Axis.Team, 3.0, "Strong signal", Rated: true, Verified: true),
		new(Axis.Financial, 1.0, "Emerging", Rated: true, Verified: true),
		new(Axis.Legal, Unrated, "Needs data room", Rated: false, Verified: false),
		new(Axis.Pitch, 2.0, "Developing", Rated: true, Verified: false)
	];

	private static readonly Edge[] edges = BuildEdges();

	private PlotContext<Dimension, Axis, double> context = default!;

	protected override void OnInitialized()
	{
		context = PlotContext.Build(dimensions, d => d.Axis, d => d.Score)
			// The polar angular scale needs the full turn: n categories at i/n
			// with the extra slot closing the wrap. Setting the scale explicitly
			// opts out of the coord system's default expansion, so restate it.
			.Scale_X_Discrete(expand: (0.0, 0.0, 0.0, 1.0), formatter: AxisTitles.Instance, titles: AxisStatus.Instance)
			.Scale_Y_Continuous(limits: (0.0, Ladder), expand: (0.0, 0.0, 0.0, 0.0), formatter: Unlabelled.Instance, includeMinorBreaks: false)
			.Geom_Radar(fill: Accent, fillOpacity: 0.10, strokeWidth: 0.0)
			.Geom_Segment(
				edges.Where(e => e.Verified),
				e => e.From, e => e.To, e => e.FromScore, e => e.ToScore,
				color: Accent, strokeWidth: 3.0, lineType: LineType.Solid)
			.Geom_Segment(
				edges.Where(e => !e.Verified),
				e => e.From, e => e.To, e => e.FromScore, e => e.ToScore,
				color: Accent, strokeWidth: 3.0, lineType: LineType.Dashed)
			.Geom_Point(
				dimensions.Where(d => d.Rated),
				d => d.Axis, d => d.Score,
				tooltip: d => builder => builder.AddContent(0, $"{AxisTitles.Instance.Format(d.Axis)}: {d.Status}"),
				size: 6.0, color: Accent)
			.Geom_Point(
				dimensions.Where(d => !d.Rated),
				d => d.Axis, d => d.Score,
				tooltip: d => builder => builder.AddContent(0, $"{AxisTitles.Instance.Format(d.Axis)}: {d.Status}"),
				size: 4.0, color: "#9ca3af")
			.Style(Style.Default(init: style =>
			{
				style.Axis.Text.X = style.Axis.Text.X with { FontSize = 1.2 };
				style.Polar.LabelMargin = 12.0;
			}));
	}

	private static Edge[] BuildEdges()
	{
		var edges = new Edge[dimensions.Length];

		for (var i = 0; i < dimensions.Length; i++)
		{
			var from = dimensions[i];
			var to = dimensions[(i + 1) % dimensions.Length];

			edges[i] = new(from.Axis, to.Axis, from.Score, to.Score, from.Verified && to.Verified);
		}

		return edges;
	}

	private sealed class AxisTitles : IFormatter<Axis>
	{
		public static readonly AxisTitles Instance = new();

		public string Format(Axis value) => value switch
		{
			Axis.Market => "Market & traction",
			Axis.Product => "Product & technology",
			Axis.Team => "Team & organisation",
			Axis.Financial => "Financial readiness",
			Axis.Legal => "Legal structure",
			Axis.Pitch => "Pitch & narrative",
			_ => value.ToString()
		};
	}

	// The status line stacked under each axis title, looked up from the
	// dimension the axis rates.
	private sealed class AxisStatus : IFormatter<Axis>
	{
		public static readonly AxisStatus Instance = new();

		public string Format(Axis value)
		  => Array.Find(dimensions, d => d.Axis == value)?.Status ?? string.Empty;
	}

	// Polar draws radial break labels up the twelve-o'clock spoke; this design
	// keeps the rings but not their numbers.
	private sealed class Unlabelled : IFormatter<double>
	{
		public static readonly Unlabelled Instance = new();

		public string Format(double value) => string.Empty;
	}
}

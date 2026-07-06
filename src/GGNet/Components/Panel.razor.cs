using GGNet.Scales;
using GGNet.Transformations;

namespace GGNet.Components;

using Rendering;

public partial class Panel<T, TX, TY> : ComponentBase, ICoord, IPanel
  where TX : struct
  where TY : struct
{
	[CascadingParameter]
	public required Plot<T, TX, TY> Plot { get; init; }

	[Parameter]
	public required Data.Panel<T, TX, TY> Data { get; init; }

	[Parameter]
	public double X { get; set; }

	[Parameter]
	public double Y { get; set; }

	[Parameter]
	public double Width { get; set; }

	[Parameter]
	public double Height { get; set; }

	[Parameter]
	public bool First { get; set; }

	[Parameter]
	public bool Last { get; set; }

	private IChildRenderModeHandler? renderModeHandler;
	private IChildRenderModeHandler? areaRenderModeHandler;

	// default!: assigned in OnInitialized/OnParametersSet before first render;
	// tooltipComponent and clip follow the same Blazor lifecycle guarantee.
	private Position<TX> xscale = default!;
	private Position<TY> yscale = default!;

	private Coords.ICoordinateSystem coord = default!;
	private Coords.GridComposition grid = new();
	private GridStamp? gridStamp;

	private Zone xStrip;
	private Zone yStrip;

	internal Zone yAxisText;
	private Zone yAxisTitle;

	private Zone xAxisText;
	private Zone xAxisTitle;

	internal Zone Area;

	protected Tooltip tooltipComponent = default!;
	public ITooltip? Tooltip => tooltipComponent;

	protected string clip = default!;

	protected bool firstRender = true;

	private readonly RenderFragment _renderTitle;
	private readonly RenderFragment _renderSubTitle;
	private readonly RenderFragment _renderStrip;
	private readonly RenderFragment _renderGrid;
	private readonly RenderFragment _renderCaption;
	private readonly RenderFragment _renderXLab;
	private readonly RenderFragment _renderYLab;

	public Panel()
	{
		_renderTitle = RenderTitle;
		_renderSubTitle = RenderSubTitle;
		_renderStrip = RenderStrip;
		_renderGrid = RenderGrid;
		_renderCaption = RenderCaption;
		_renderXLab = RenderXLab;
		_renderYLab = RenderYLab;
	}

	protected override void OnInitialized()
	{
		renderModeHandler = Plot.RenderModeHandler?.Child();
		areaRenderModeHandler = Plot.RenderModeHandler?.Child();

		Data.Register(this);

		clip = Plot.Id + "-" + Data.Id;

		xscale = Data.X;
		yscale = Data.Y;

		coord = Data.Data.MakeCoordinateSystem();
	}

	protected override void OnParametersSet()
	{
		if (Data.Registered)
		{
			return;
		}

		Data.Register(this);

		clip = Plot.Id + "-" + Data.Id;

		xscale = Data.X;
		yscale = Data.Y;

		coord = Data.Data.MakeCoordinateSystem();

		gridStamp = null;

		Refresh(RenderTarget.All);
	}

	protected void Render(bool firstRender)
	{
		// Recompose layout and grid only when their inputs changed: the stamp is
		// a structural snapshot, so a match proves the stored zones and grid are
		// exact. Streaming refreshes with pinned scales skip straight to shapes.
		var stamp = GridStamp.Capture(new Zone { X = X, Y = Y, Width = Width, Height = Height }, xscale, yscale, Data.Data.Axis);

		if (firstRender || !stamp.Matches(gridStamp))
		{
			gridStamp = stamp;

			ComposePanel();
		}

		if (!firstRender)
		{
			areaRenderModeHandler?.Refresh(RenderTarget.Data);
		}
	}

	private void ComposePanel()
	{
		var zones = Layout.PanelLayout.Compute(new(
			Outer: new Zone { X = X, Y = Y, Width = Width, Height = Height },
			XStripText: Data.Strip.x,
			YStripText: Data.Strip.y,
			XAxis: Data.Axis.x,
			YAxis: Data.Axis.y,
			CarveAxes: coord.CarvesAxisBands,
			AxisWidth: Data.Data.Axis.width,
			AxisHeight: Data.Data.Axis.height,
			YLabWidth: Data.YLab.width,
			XLabHeight: Data.XLab.height,
			HasXTitles: Data.X.Titles.Any(),
			HasYTitles: Data.Y.Titles.Any(),
			Style: Data.Data.Style!));

		Area = zones.Area;
		xStrip = zones.XStrip;
		yStrip = zones.YStrip;
		yAxisText = zones.YAxisText;
		yAxisTitle = zones.YAxisTitle;
		xAxisText = zones.XAxisText;
		xAxisTitle = zones.XAxisTitle;

		coord.Measure(Area);

		grid = coord.ComposeGrid(new(
			XAxis: Data.Axis.x,
			YAxis: Data.Axis.y,
			XBreaks: [.. xscale.Breaks.Select(xscale.Coord)],
			XMinorBreaks: [.. xscale.MinorBreaks.Select(xscale.Coord)],
			XLabels: [.. xscale.Labels.Select(l => (xscale.Coord(l.value), l.label))],
			XTitles: [.. xscale.Titles.Select(t => (xscale.Coord(t.value), t.title))],
			YBreaks: [.. yscale.Breaks.Select(yscale.Coord)],
			YMinorBreaks: [.. yscale.MinorBreaks.Select(yscale.Coord)],
			YLabels: [.. yscale.Labels.Select(l => (yscale.Coord(l.value), l.label))],
			XLabelY: xAxisText.Y,
			XTitleY: xAxisTitle.Y,
			YLabelX: yAxisText.X));
	}

	public void Refresh(RenderTarget target) => renderModeHandler?.Refresh(target);

	protected override bool ShouldRender() => renderModeHandler?.ShouldRender(RenderTarget.Data) ?? true;

	public double ToX(double value) => Area.X + xscale.Coord(value) * Area.Width;

	public (double min, double max) XRange => xscale.Range;

	public ITransformation<double> XTransformation => xscale.RangeTransformation;

	public double ToY(double value) => Area.Y + (1 - yscale.Coord(value)) * Area.Height;

	public (double min, double max) YRange => yscale.Range;

	public ITransformation<double> YTransformation => yscale.RangeTransformation;

	public (double x, double y) Project(double x, double y)
	  => coord.Project(xscale.Coord(x), yscale.Coord(y));

	private static string RingPath(IReadOnlyList<(double x, double y)> points)
	{
		var sb = new StringBuilder();

		for (var i = 0; i < points.Count; i++)
		{
			sb.Append(CultureInfo.InvariantCulture, $"{(i == 0 ? "M " : " L ")}{points[i].x} {points[i].y}");
		}

		sb.Append(" Z");

		return sb.ToString();
	}

	private string GridClipId(Coords.GridClip clipKind) => clipKind switch
	{
		Coords.GridClip.Plot => Plot.Id + "-plot",
		_ => clip,
	};

	private Task OnClick(MouseEventArgs e)
	{
		if (Data.OnClick is null)
		{

			return Task.CompletedTask;
		}

		return Data.OnClick(e);
	}
}

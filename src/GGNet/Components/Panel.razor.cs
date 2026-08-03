using GGNet.Scales;
using GGNet.Transformations;

using Microsoft.JSInterop;

namespace GGNet.Components;

using Rendering;

public partial class Panel<T, TX, TY> : ComponentBase, ICoord, IPanel, IAsyncDisposable
  where TX : struct
  where TY : struct
{
	[CascadingParameter]
	public required Plot<T, TX, TY> Plot { get; init; }

	// IServiceProvider, not IJSRuntime: the headless renderer builds an empty
	// container, so a hard IJSRuntime dependency would break every static
	// render. Resolved lazily on the pan path only, which headless never takes.
	[Inject]
	private IServiceProvider Services { get; init; } = default!;

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

	private readonly RenderFragment renderTitle;
	private readonly RenderFragment renderSubTitle;
	private readonly RenderFragment renderStrip;
	private readonly RenderFragment renderGrid;
	private readonly RenderFragment renderCaption;
	private readonly RenderFragment renderXLab;
	private readonly RenderFragment renderYLab;
	private readonly RenderFragment renderContent;

	// Trimming must preserve the JS-invokable gesture commits.
	[DynamicDependency(nameof(OnPanEndAsync))]
	[DynamicDependency(nameof(OnWheelAsync))]
	public Panel()
	{
		renderTitle = RenderTitle;
		renderSubTitle = RenderSubTitle;
		renderStrip = RenderStrip;
		renderGrid = RenderGrid;
		renderCaption = RenderCaption;
		renderXLab = RenderXLab;
		renderYLab = RenderYLab;
		renderContent = RenderContent;
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

		Refresh();
	}

	protected void Render(bool firstRender)
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

		coord.Measure(Area, Data.X.Titles.Any());

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

		if (!firstRender)
		{
			areaRenderModeHandler?.Refresh();
		}
	}

	public void Refresh() => renderModeHandler?.Refresh();

	protected override bool ShouldRender() => renderModeHandler?.ShouldRender() ?? true;

	public double ToX(double value) => Area.X + xscale.Coord(value) * Area.Width;

	public (double min, double max) XRange => xscale.Range;

	public ITransformation<double> XTransformation => xscale.RangeTransformation;

	public double ToY(double value) => Area.Y + (1 - yscale.Coord(value)) * Area.Height;

	public (double min, double max) YRange => yscale.Range;

	public ITransformation<double> YTransformation => yscale.RangeTransformation;

	public (double x, double y) Project(double x, double y)
	  => coord.Project(xscale.Coord(x), yscale.Coord(y));

	public (double x, double y) Unproject(double px, double py)
	{
		var (cx, cy) = coord.Unproject(px, py);

		return (xscale.Invert(cx), yscale.Invert(cy));
	}

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

	/// <summary>Commits a wheel zoom: shrinks or grows the view window about the cursor and re-renders. Invoked from the collocated JS module, which converts pointer coordinates into svg units against the rendered size.</summary>
	/// <param name="px">Cursor x, in svg units.</param>
	/// <param name="py">Cursor y, in svg units.</param>
	/// <param name="deltaY">Wheel delta; negative zooms in.</param>
	[JSInvokable]
	public Task OnWheelAsync(double px, double py, double deltaY)
	{
		if (Plot.Interactivity is not { } options || deltaY == 0)
		{
			return Task.CompletedTask;
		}

		// Over the axis bands or margins: not a zoom gesture.
		if (px < Area.X || px > (Area.X + Area.Width)
			|| py < Area.Y || py > (Area.Y + Area.Height))
		{
			return Task.CompletedTask;
		}

		var factor = deltaY < 0 ? options.ZoomStep : 1.0 / options.ZoomStep;

		var (cx, cy) = coord.Unproject(px, py);

		if (options.Zoom is ZoomAxis.X or ZoomAxis.Both)
		{
			Plot.Context.Positions.X.SetView(Window(xscale, cx, factor));
		}

		// Under AutoFitY the y window is derived from the x window.
		if (options.AutoFitY)
		{
			Plot.Context.FitYToXView();
		}
		else if (options.Zoom is ZoomAxis.Y or ZoomAxis.Both)
		{
			Plot.Context.Positions.Y.SetView(Window(yscale, cy, factor));
		}

		return Plot.RefreshAsync(RenderTarget.Render, CancellationToken.None);
	}

	// Shrinks (or grows) the committed range about the cursor's data position,
	// so the point under the cursor stays put while the window scales.
	private static (double min, double max) Window<TKey>(Position<TKey> scale, double fraction, double factor)
		where TKey : struct
	{
		var anchor = scale.Invert(fraction);
		var (min, max) = scale.Range;

		return (anchor - (anchor - min) * factor, anchor + (max - anchor) * factor);
	}

	private Task OnResetViewAsync(MouseEventArgs e)
	{
		if (Plot.Interactivity is null)
		{
			return Task.CompletedTask;
		}

		Plot.Context.ResetView();

		return Plot.RefreshAsync(RenderTarget.Render, CancellationToken.None);
	}

	// The active crosshair, in pixels plus readout text; null when the pointer
	// is not over the panel. Panel-local presentation state, not plot state.
	private (double x, string? label)? crosshair;

	private void OnCrosshairOver(double px)
	{
		if (Plot.Interactivity is not { Crosshair: true })
		{
			return;
		}

		var (cx, _) = coord.Unproject(px, Area.Y);
		var mapped = xscale.Invert(cx);

		var (x, label) = xscale.Unmap(mapped) is { } key
			? (ToX(xscale.Map(key)), xscale.Label(key))
			: (px, null);

		if (crosshair is { } current && current.x == x)
		{
			return;
		}

		crosshair = (x, label);

		Refresh();
	}

	private void OnCrosshairLeave()
	{
		if (crosshair is null)
		{
			return;
		}

		crosshair = null;

		Refresh();
	}

	// Drag-pan (the first JS-interop feature): the collocated module owns the
	// pointer gesture and the client-side preview; .NET sees one callback per
	// gesture, carrying the net pixel delta.
	private ElementReference captureRef;
	private ElementReference panRef;

	private PanelInterop? interop;
	private DotNetObjectReference<Panel<T, TX, TY>>? dotNetRef;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Wheel capture lives in the module, so any opt-in initializes it.
		if (!firstRender || Plot.Interactivity is not { } options || !coord.CarvesAxisBands)
		{
			return;
		}

		if (Services.GetService(typeof(IJSRuntime)) is not IJSRuntime runtime)
		{
			return;
		}

		interop = new PanelInterop(runtime);
		dotNetRef = DotNetObjectReference.Create(this);

		await interop.InitializeAsync(
			clip,
			captureRef,
			panRef,
			dotNetRef,
			new PanelInteropOptions(
				Pan: options.Pan,
				PanX: options.Zoom is ZoomAxis.X or ZoomAxis.Both,
				// Y is derived under AutoFitY — a vertical preview would only snap back.
				PanY: !options.AutoFitY && options.Zoom is ZoomAxis.Y or ZoomAxis.Both,
				Tooltip: options.CursorTooltip));
	}

	// Hands the glued tooltip's element to the JS module after each render
	// with content; the module shows the popover and keeps it on the cursor.
	private async ValueTask GlueTooltipAsync(ElementReference element)
	{
		if (interop is not null)
		{
			await interop.ShowTooltipAsync(element);
		}
	}

	/// <summary>Commits a finished drag-pan: converts the net pixel delta into a view-window shift and re-renders. Invoked from the collocated JS module.</summary>
	/// <param name="dx">Net horizontal drag, in svg units (positive right).</param>
	/// <param name="dy">Net vertical drag, in svg units (positive down).</param>
	[JSInvokable]
	public async Task OnPanEndAsync(double dx, double dy)
	{
		if (Plot.Interactivity is not { Pan: true } options)
		{
			return;
		}

		if (dx != 0 && options.Zoom is ZoomAxis.X or ZoomAxis.Both)
		{
			var (min, max) = xscale.Range;
			var shift = -dx / Area.Width * (max - min);

			Plot.Context.Positions.X.SetView((min + shift, max + shift));
		}

		// Under AutoFitY the y window is derived from the x window.
		if (options.AutoFitY)
		{
			Plot.Context.FitYToXView();
		}
		else if (dy != 0 && options.Zoom is ZoomAxis.Y or ZoomAxis.Both)
		{
			var (min, max) = yscale.Range;
			var shift = dy / Area.Height * (max - min);

			Plot.Context.Positions.Y.SetView((min + shift, max + shift));
		}

		await Plot.RefreshAsync(RenderTarget.Render, CancellationToken.None);
	}

	private int disposing;

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		if (Interlocked.CompareExchange(ref disposing, 1, 0) == 1)
		{
			return;
		}

		try
		{
			if (interop is not null)
			{
				await interop.DisposeAsync();
			}
		}
		catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
		{
		}

		dotNetRef?.Dispose();
	}
}

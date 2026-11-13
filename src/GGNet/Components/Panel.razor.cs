using GGNet.Scales;
using GGNet.Transformations;

using Microsoft.JSInterop;

namespace GGNet.Components;

using Readings;
using Rendering;

public partial class Panel<T, TX, TY> : ComponentBase, ICoord, IPanel, IAsyncDisposable
  where TX : struct
  where TY : struct
{
	[CascadingParameter]
	public required Plot<T, TX, TY> Plot { get; init; }

	// INVARIANT: IJSRuntime resolves lazily through IServiceProvider — the headless renderer builds an
	// empty container, so a hard dependency breaks every static render and export.
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
	private Data.Panel<T, TX, TY>? boundData;

	private Position<TX> xscale = default!;
	private Position<TY> yscale = default!;

	private Coords.CoordinateSystem coord = default!;
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

	// INVARIANT: trimming removes the JS-invokable gesture commits unless each is named here; no test
	// trims, so a published app is the first place it would fail.
	[DynamicDependency(nameof(OnPanEndAsync))]
	[DynamicDependency(nameof(OnWheelAsync))]
	[DynamicDependency(nameof(OnCursorAsync))]
	[DynamicDependency(nameof(OnCursorLeaveAsync))]
	[DynamicDependency(nameof(OnCursorRestoreAsync))]
	[DynamicDependency(nameof(OnNavigateEndAsync))]
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
		cursorSource = Cursor;
	}

	protected override void OnInitialized()
	{
		renderModeHandler = Plot.RenderModeHandler?.Child();
		areaRenderModeHandler = Plot.RenderModeHandler?.Child();
	}

	protected override void OnParametersSet()
	{
		if (ReferenceEquals(boundData, Data))
		{
			return;
		}

		boundData?.Unregister(this);
		boundData = Data;
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

	// TODO: calibration — the longest gap between the two clicks of a double click or double tap this still catches. 300 ms felt laggy in physical QA on the 10-inch panel; a slower double tap activates before it resets.
	private static readonly TimeSpan activationDelay = TimeSpan.FromMilliseconds(200);

	private CancellationTokenSource? pendingActivation;

	private bool Activates => Data.Activate is not null && Plot.Interactivity is not ({ CursorTooltip: true } or { Crosshair: true });

	private async Task OnActivateAsync(MouseEventArgs e)
	{
		if (Data.Activate is not { } activate)
		{
			return;
		}

		if (Plot.Interactivity is null)
		{
			await activate();

			return;
		}

		CancelPendingActivation();

		if (e.Detail > 1)
		{
			return;
		}

		using var pending = new CancellationTokenSource();

		pendingActivation = pending;

		try
		{
			await Task.Delay(activationDelay, pending.Token);
		}
		catch (OperationCanceledException)
		{
			return;
		}

		if (ReferenceEquals(pendingActivation, pending))
		{
			pendingActivation = null;
		}

		if (Volatile.Read(ref disposing) == 0)
		{
			await activate();
		}
	}

	private void CancelPendingActivation()
	{
		var pending = pendingActivation;

		pendingActivation = null;

		pending?.Cancel();
	}

	/// <summary>Commits a wheel zoom: shrinks or grows the view window about the cursor and re-renders. Invoked from the collocated JS module, which converts pointer coordinates into svg units using the capture element's inverse screen CTM.</summary>
	/// <param name="px">Cursor x, in svg units.</param>
	/// <param name="py">Cursor y, in svg units.</param>
	/// <param name="deltaY">Wheel delta; negative zooms in.</param>
	[JSInvokable]
	public Task OnWheelAsync(double px, double py, double deltaY)
	{
		if (Volatile.Read(ref disposing) != 0 || Plot.Interactivity is not { } options || deltaY == 0)
		{
			return Task.CompletedTask;
		}

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

		if (options.AutoFitY)
		{
			Plot.Context.FitYToXView();
		}
		else if (options.Zoom is ZoomAxis.Y or ZoomAxis.Both)
		{
			Plot.Context.Positions.Y.SetView(Window(yscale, cy, factor));
		}

		return Plot.CommitViewAsync(ViewChangeCause.Gesture, RenderTarget.Render, CancellationToken.None);
	}

	private static (double min, double max) Window<TKey>(Position<TKey> scale, double fraction, double factor)
		where TKey : struct
	{
		return (scale.Invert(fraction * (1 - factor)), scale.Invert(1 + (1 - fraction) * (factor - 1)));
	}

	private Task OnResetViewAsync(MouseEventArgs e)
	{
		if (Plot.Interactivity is null)
		{
			return Task.CompletedTask;
		}

		CancelPendingActivation();

		Plot.Context.ResetView();

		return Plot.CommitViewAsync(ViewChangeCause.Reset, RenderTarget.Render, CancellationToken.None);
	}

	private CrosshairCursor? cursorComponent;
	private Func<CursorFrame> cursorSource = default!;
	private TX? localCursor;

	private bool SharesCursor => Plot.InspectsSharedX;

	public void RefreshCursor() => cursorComponent?.Refresh();

	/// <summary>Moves the crosshair to a pointer position, or pins it there. Invoked from the collocated JS module, latest position wins: at most one call is in flight per plot.</summary>
	/// <param name="px">Pointer x, in svg units.</param>
	/// <param name="pinned">True for a tap, a click or the end of a drag; false while a hover or a drag is previewing.</param>
	[JSInvokable]
	public Task OnCursorAsync(double px, bool pinned)
	{
		if (Volatile.Read(ref disposing) != 0 || Plot.Interactivity is not { Crosshair: true })
		{
			return Task.CompletedTask;
		}

		if (px < Area.X || px > (Area.X + Area.Width))
		{
			return Task.CompletedTask;
		}

		var (cx, _) = coord.Unproject(px, Area.Y);

		if (xscale.Unmap(xscale.Invert(cx)) is not { } key)
		{
			return Task.CompletedTask;
		}

		if (SharesCursor)
		{
			return Plot.InspectAsync(new(key, pinned), notify: true);
		}

		localCursor = key;

		RefreshCursor();

		return Task.CompletedTask;
	}

	/// <summary>Ends an unpinned crosshair when the pointer leaves the plot area. A pinned one stays. Invoked from the collocated JS module.</summary>
	[JSInvokable]
	public Task OnCursorLeaveAsync()
	{
		if (Volatile.Read(ref disposing) != 0 || Plot.Interactivity is not { Crosshair: true })
		{
			return Task.CompletedTask;
		}

		if (SharesCursor)
		{
			return Plot.CurrentInspection is { Pinned: false } ? Plot.InspectAsync(null, notify: true) : Task.CompletedTask;
		}

		localCursor = null;

		RefreshCursor();

		return Task.CompletedTask;
	}

	/// <summary>Puts the crosshair back where it was last pinned, or clears it when nothing was pinned: a drag that was abandoned must leave no trace. Invoked from the collocated JS module.</summary>
	[JSInvokable]
	public Task OnCursorRestoreAsync()
	{
		if (Volatile.Read(ref disposing) != 0 || Plot.Interactivity is not { Crosshair: true } || !SharesCursor)
		{
			return Task.CompletedTask;
		}

		return Plot.RestoreInspectionAsync();
	}

	private CursorFrame Cursor()
	{
		var top = Area.Y;
		var bottom = Area.Y + Area.Height;

		var (key, pinned) = SharesCursor
			? (Plot.CurrentInspection?.X, Plot.CurrentInspection?.Pinned ?? false)
			: (localCursor, false);

		if (key is not { } value || xscale.Map(value) is var mapped && double.IsNaN(mapped))
		{
			return new(Area.X, top, bottom, null, false, false, []);
		}

		var x = ToX(mapped);
		var visible = x >= Area.X - 0.5 && x <= Area.X + Area.Width + 0.5;

		List<(double x, double y)> markers = [];

		if (visible && SharesCursor && InspectAxis.Supports<TX>())
		{
			var cursor = InspectAxis.Coordinate(value);

			for (var i = 0; i < Data.Geoms.Count; i++)
			{
				var series = Data.Geoms[i].InspectSeries();

				for (var j = 0; j < series.Count; j++)
				{
					if (series[j].Read(cursor) is not { Row: >= 0 } reading)
					{
						continue;
					}

					var (mx, my) = series[j].Position(reading.Row);

					if (!double.IsNaN(mx) && !double.IsNaN(my))
					{
						markers.Add((ToX(mx), ToY(my)));
					}
				}
			}
		}

		return new(x, top, bottom, xscale.Label(value), pinned, visible, markers);
	}

	private ElementReference captureRef;
	private ElementReference panRef;

	private PanelInterop? interop;
	private DotNetObjectReference<Panel<T, TX, TY>>? dotNetRef;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (Volatile.Read(ref disposing) != 0 || !firstRender || Plot.Interactivity is not { } options || !coord.CarvesAxisBands)
		{
			return;
		}

		if (Services.GetService(typeof(IJSRuntime)) is not IJSRuntime runtime)
		{
			return;
		}

		interop = new PanelInterop(runtime);
		dotNetRef = DotNetObjectReference.Create(this);

		await interop.InitializeAsync(clip, captureRef, panRef, dotNetRef, InteropOptions(options));
	}

	private PanelInteropOptions InteropOptions(InteractivityOptions options)
		=> PanelInteropOptions.Create(options) with
		{
			Group = Plot.Id,
			SharedX = Plot.Context.Positions.X.Scales.Count == 1,
			SharedY = Plot.Context.Positions.Y.Scales.Count == 1,
			Cursor = !options.Crosshair ? null : SharesCursor ? PanelInterop.SharedCursor : PanelInterop.LocalCursor,
			CursorContinuous = options.Crosshair && SharesCursor && xscale.InvertsExactly
		};

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
		if (Volatile.Read(ref disposing) != 0 || Plot.Interactivity is not { Pan: true } options)
		{
			return;
		}

		if (dx != 0 && options.Zoom is ZoomAxis.X or ZoomAxis.Both)
		{
			var shift = -dx / Area.Width;
			Plot.Context.Positions.X.SetView((xscale.Invert(shift), xscale.Invert(1 + shift)));
		}

		if (options.AutoFitY)
		{
			Plot.Context.FitYToXView();
		}
		else if (dy != 0 && options.Zoom is ZoomAxis.Y or ZoomAxis.Both)
		{
			var shift = dy / Area.Height;
			Plot.Context.Positions.Y.SetView((yscale.Invert(shift), yscale.Invert(1 + shift)));
		}

		await Plot.CommitViewAsync(ViewChangeCause.Gesture, RenderTarget.Render, CancellationToken.None);
	}

	/// <summary>Commits a finished two-finger gesture under the crosshair: the x value that was under the fingers' midpoint at the start ends under their midpoint at the end, with the window narrowed or widened by the pinch. Invoked from the collocated JS module.</summary>
	/// <param name="from">Midpoint of the two fingers when the gesture began, in svg units.</param>
	/// <param name="to">Midpoint of the two fingers when it ended, in svg units.</param>
	/// <param name="scale">Finger distance at the end over the distance at the start; above 1 zooms in.</param>
	[JSInvokable]
	public async Task OnNavigateEndAsync(double from, double to, double scale)
	{
		if (Volatile.Read(ref disposing) != 0
			|| Plot.Interactivity is not { Pan: true, Crosshair: true, Zoom: ZoomAxis.X or ZoomAxis.Both } options
			|| !SharesCursor
			|| !double.IsFinite(from) || !double.IsFinite(to) || !double.IsFinite(scale) || scale <= 0
			|| Area.Width <= 0)
		{
			return;
		}

		scale = Math.Clamp(scale, minPinch, maxPinch);

		var start = (from - Area.X) / Area.Width;
		var end = (to - Area.X) / Area.Width;

		Plot.Context.Positions.X.SetView((xscale.Invert(start - end / scale), xscale.Invert(start + (1 - end) / scale)));

		if (options.AutoFitY)
		{
			Plot.Context.FitYToXView();
		}

		await Plot.CommitViewAsync(ViewChangeCause.Gesture, RenderTarget.Render, CancellationToken.None);
	}

	// TODO: calibration — how far one pinch may narrow or widen the window; physical QA on a touchscreen settles whether a single gesture needs more.
	private const double minPinch = 0.1;
	private const double maxPinch = 10.0;

	private int disposing;

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		if (Interlocked.CompareExchange(ref disposing, 1, 0) == 1)
		{
			return;
		}

		boundData?.Unregister(this);

		CancelPendingActivation();

		try
		{
			if (interop is not null)
			{
				await interop.DisposeAsync();
			}
		}
		finally
		{
			dotNetRef?.Dispose();
		}
	}
}

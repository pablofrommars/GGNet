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
	private readonly string renderSession = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
	private long renderSequence;
	internal string? TouchFrame { get; private set; }
	internal string? TouchView { get; private set; }
	internal string? TouchCommit { get; private set; }
	private string? CurrentTouchView => Plot.StampsFrames
		? string.Create(CultureInfo.InvariantCulture, $"{Plot.TouchSession} {Plot.RenderedTouchEpoch} {Plot.RenderedTouchRevision} {xscale.Range.min:R} {xscale.Range.max:R}")
		: null;

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
	[DynamicDependency(nameof(OnTouchNavigateAsync))]
	[DynamicDependency(nameof(OnTouchInspectAsync))]
	[DynamicDependency(nameof(OnTouchActionAsync))]
	[DynamicDependency(nameof(OnCursorLabelMeasuredAsync))]
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
		TouchFrame = Plot.StampsFrames
			? string.Create(CultureInfo.InvariantCulture, $"{renderSession}:{++renderSequence}")
			: null;
		TouchView = CurrentTouchView;
		TouchCommit = Plot.StampsFrames && Plot.RenderedPanCommit != 0
			? Plot.RenderedPanCommit.ToString(CultureInfo.InvariantCulture)
			: null;

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
		if (Volatile.Read(ref disposing) != 0 || !Plot.InspectionEnabled)
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
		if (Volatile.Read(ref disposing) != 0 || !Plot.InspectionEnabled || !SharesCursor)
		{
			return Task.CompletedTask;
		}

		return Plot.RestoreInspectionAsync();
	}

	private CursorFrame Cursor() => CursorGeometry() with
	{
		InspectionStamp = Plot.StampsInspection ? Plot.TouchInspectionStamp : null,
		Selection = Plot.CurrentInspection is { } selected ? xscale.Map(selected.X) : null,
		CommittedSelection = Plot.PinnedInspection is { } committed ? xscale.Map(committed.X) : null,
		Draw = Plot.Interactivity is { Crosshair: true }
	};

	private CursorFrame CursorGeometry()
	{
		var top = Area.Y;
		var bottom = Area.Y + Area.Height;

		var (key, pinned) = SharesCursor
			? (Plot.CurrentInspection?.X, Plot.CurrentInspection?.Pinned ?? false)
			: (localCursor, false);

		if (key is not { } value || xscale.Map(value) is var mapped && double.IsNaN(mapped))
		{
			return new(Area.X, top, bottom, null, false, false, [], CurrentTouchView);
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

		var frame = new CursorFrame(x, top, bottom, xscale.Label(value), pinned, visible, markers, CurrentTouchView);
		if (Plot.Style.Cursor is not { } style)
		{
			return frame;
		}

		if (style.FirstPanelOnly && SharesCursor && Plot.Context.Panels.Any(panel => panel.Coord.col == Data.Coord.col && panel.Coord.row < Data.Coord.row))
		{
			return frame with { ShowLabel = false };
		}

		var padding = double.IsFinite(style.Padding) ? Math.Max(0, style.Padding) : 4;
		var gap = double.IsFinite(style.Gap) ? Math.Max(0, style.Gap) : 4;
		var size = (style.FontSize ?? Plot.Style.Axis.Text.X.FontSize).Height();
		size = double.IsFinite(size) && size > 0 ? size : 12;
		var measureKey = string.Create(CultureInfo.InvariantCulture, $"{size:R}:{frame.Label}");
		var measured = cursorLabelMetrics is { } metrics && metrics.key == measureKey;
		var width = measured ? cursorLabelMetrics!.Value.width : frame.Label.Width(new Size { Value = size, Units = Units.PX });
		var height = measured ? cursorLabelMetrics!.Value.height : size * 1.25;
		var verticalFit = Math.Min(1, Math.Max(1, Area.Height - 2 * padding - gap) / height);
		width *= verticalFit;
		height *= verticalFit;
		var available = Math.Max(1, Area.Width - 2 * padding);
		var fittedWidth = Math.Min(width, available);
		var center = Area.X + Area.Width / 2;
		var halfTravel = Math.Max(0, (Area.Width - fittedWidth) / 2 - padding);
		return frame with
		{
			Top = Math.Min(bottom, top + padding + height + gap),
			LabelX = Math.Clamp(x, center - halfTravel, center + halfTravel),
			LabelTop = top + padding,
			LabelWidth = measured && width > available ? available : null,
			LabelFontSize = size * verticalFit,
			MeasureFontSize = size,
			MeasureKey = measureKey,
			LabelReady = measured
		};
	}

	private (string key, double width, double height)? cursorLabelMetrics;

	[JSInvokable]
	public Task OnCursorLabelMeasuredAsync(string key, double width, double height)
	{
		if (Volatile.Read(ref disposing) == 0 && double.IsFinite(width) && width >= 0
			&& double.IsFinite(height) && height > 0 && CursorGeometry().MeasureKey == key)
		{
			cursorLabelMetrics = (key, width, height);
			RefreshCursor();
		}

		return Task.CompletedTask;
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
			Group = Plot.TouchSession,
			SharedX = Plot.Context.Positions.X.Scales.Count == 1,
			SharedY = Plot.Context.Positions.Y.Scales.Count == 1,
			Touch = Plot.TouchOptions,
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
	/// <param name="commit">The module's own number for this commit, which the frame that renders it carries; zero carries none.</param>
	/// <returns>The epoch this commit advanced the plot to, which the frame that renders it carries; zero when nothing was committed.</returns>
	[JSInvokable]
	public async Task<long> OnPanEndAsync(double dx, double dy, long commit = 0)
	{
		if (Volatile.Read(ref disposing) != 0 || Plot.Interactivity is not { Pan: true } options)
		{
			return 0;
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

		var epoch = await Plot.RenderViewAsync(ViewChangeCause.Gesture, RenderTarget.Render, CancellationToken.None, pan: commit);

		await Plot.ReportViewChangeAsync(ViewChangeCause.Gesture).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing);

		return epoch;
	}

	[JSInvokable]
	public async Task OnTouchNavigateAsync(string session, long epoch, long revision, double min, double max)
	{
		if (Volatile.Read(ref disposing) != 0
			|| Plot.Interactivity is not { } options
			|| Plot.TouchOptions is not { Navigation: not TouchNavigation.None }
			|| !double.IsFinite(min) || !double.IsFinite(max) || max <= min
			|| !Plot.AcceptTouch(session, epoch, revision, navigation: true))
		{
			return;
		}

		Plot.Context.Positions.X.SetView((min, max));
		if (options.AutoFitY)
		{
			Plot.Context.FitYToXView();
		}

		await Plot.CommitViewAsync(ViewChangeCause.Gesture, RenderTarget.Render, CancellationToken.None, touch: true);
	}

	[JSInvokable]
	public Task OnTouchActionAsync(string session, long epoch, long revision, bool reset)
	{
		if (Volatile.Read(ref disposing) != 0 || Plot.TouchOptions is not { } options
			|| (reset ? !options.DoubleTapResetsView : !options.TapActivatesPanel)
			|| !Plot.AcceptTouch(session, epoch, revision, navigation: null))
		{
			return Task.CompletedTask;
		}

		if (!reset)
		{
			return Data.Activate?.Invoke() ?? Task.CompletedTask;
		}

		Plot.Context.ResetView();
		return Plot.CommitViewAsync(ViewChangeCause.Reset, RenderTarget.Render, CancellationToken.None);
	}

	[JSInvokable]
	public Task OnTouchInspectAsync(string session, long epoch, long revision, double mapped, bool pinned, bool restore)
	{
		if (Volatile.Read(ref disposing) != 0 || Plot.TouchOptions is not { Inspect: true }
			|| !double.IsFinite(mapped))
		{
			return Task.CompletedTask;
		}

		var key = xscale.Unmap(mapped);
		if (!Plot.AcceptTouch(session, epoch, revision, navigation: false))
		{
			return Task.CompletedTask;
		}

		if (restore || (key is null && pinned))
		{
			return Plot.RestoreInspectionAsync(forceFrame: true);
		}

		return Plot.InspectAsync(key is { } value ? new(value, pinned) : Plot.CurrentInspection, notify: true, forceFrame: true);
	}

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

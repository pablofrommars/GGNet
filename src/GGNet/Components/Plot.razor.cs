using GGNet.Exceptions;

using Microsoft.Extensions.Logging;

namespace GGNet.Components;

using Readings;
using Rendering;

public partial class Plot<T, TX, TY> : ComponentBase, IPlot, IPlotRendering, IAsyncDisposable
  where TX : struct
  where TY : struct
{
	[Parameter]
	public required PlotContext<T, TX, TY> Context { get; init; }

	[Parameter]
	public required RenderMode RenderMode { get; init; }

	[Parameter]
	public double Width { get; init; } = 720;

	[Parameter]
	public double Height { get; init; } = 576;

	[Parameter]
	public string Theme { get; init; } = "default";

	[Parameter]
	public InteractivityOptions? Interactivity { get; init; }

	/// <summary>Raised once after each committed change of the view window: a wheel zoom, a finished drag pan, the reset gesture, or a host command. A drag preview raises nothing until it commits. The argument carries the cause and both windows in data units.</summary>
	[Parameter]
	public EventCallback<ViewChange<TX, TY>> ViewChanged { get; init; }

	/// <summary>The crosshair's position, for <c>@bind-Inspection</c>. Null is no inspection. Setting it from the host moves, pins or clears the crosshair; it survives a new <c>Context</c>. Needs <c>Interactivity</c> with <c>Crosshair</c>.</summary>
	[Parameter]
	public Inspection<TX>? Inspection { get; init; }

	/// <summary>Raised when the crosshair's position or pinned state changes through a gesture or <c>ClearInspectionAsync</c>. Not raised when only the readings change.</summary>
	[Parameter]
	public EventCallback<Inspection<TX>?> InspectionChanged { get; init; }

	/// <summary>Raised whenever the readout changes: after the crosshair moves, and when data changes under a position that stays where it is.</summary>
	[Parameter]
	public EventCallback<InspectionReadings<TX>> ReadingsChanged { get; init; }

	[Parameter(CaptureUnmatchedValues = true)]
	public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

	// INVARIANT: logging resolves lazily through IServiceProvider — the headless renderer builds an
	// empty container, so a hard ILoggerFactory dependency breaks every static render and export.
	[Inject]
	private IServiceProvider Services { get; init; } = default!;

	public string Id => Context.Id;

	public Style Style => Context.Style!;

	public IRenderModeHandler? RenderModeHandler { get; set; }

	internal Zone Title;
	internal Zone SubTitle;

	internal Zone Legend;

	internal Zone Caption;

	private Zone wrapper;

	private IChildRenderModeHandler? definitionsRenderModeHandler;

	private bool loading;
	private PlotContext<T, TX, TY>? renderedContext;
	private double renderedWidth;
	private double renderedHeight;
	private bool parametersAccepted;
	private InteractivityOptions? initialInteractivity;
	private ((double min, double max)? x, (double min, double max)? y) reportedView;
	private bool inspectionSupplied;
	private Inspection<TX>? inspection;
	private Inspection<TX>? pinned;
	private InspectionReadings<TX>? readings;
	private bool publishingReadings;
	private bool readingsPending;
	internal string TouchSession { get; } = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
	private long touchEpoch;
	private (double min, double max)? touchXView;
	private long touchRevision;
	private long touchNavigationRevision;
	private long touchInspectionRevision;
	private long publishedTouchEpoch;
	private long publishedInspectionRevision;
	internal string? TouchInspectionStamp => !publishingReadings && publishedTouchEpoch == touchEpoch && publishedInspectionRevision == touchInspectionRevision
		? string.Create(CultureInfo.InvariantCulture, $"{TouchSession} {touchEpoch} {touchInspectionRevision}") : null;
	internal Inspection<TX>? PinnedInspection => pinned;
	internal long RenderedTouchEpoch { get; private set; }
	internal long RenderedTouchRevision { get; private set; }
	private long panCommit;
	internal long RenderedPanCommit { get; private set; }

	internal bool AcceptTouch(string session, long epoch, long revision, bool? navigation)
	{
		if (session != TouchSession || epoch != touchEpoch || revision <= touchRevision || Volatile.Read(ref disposing) != 0)
		{
			return false;
		}

		touchRevision = revision;
		if (navigation == true)
		{
			touchNavigationRevision = revision;
		}
		else if (navigation == false)
		{
			touchInspectionRevision = revision;
		}
		return true;
	}

	private readonly RenderFragment renderLegendGradients;
	private readonly RenderFragment renderLegend;
	private readonly RenderFragment renderPanels;

	public Plot()
	{
		renderLegendGradients = RenderLegendGradients;
		renderLegend = RenderLegend;
		renderPanels = RenderPanels;
	}

	public override Task SetParametersAsync(ParameterView parameters)
	{
		var requested = Interactivity;

		foreach (var parameter in parameters)
		{
			if (string.Equals(parameter.Name, nameof(Interactivity), StringComparison.OrdinalIgnoreCase))
			{
				requested = (InteractivityOptions?)parameter.Value;
			}
			else if (string.Equals(parameter.Name, nameof(Inspection), StringComparison.OrdinalIgnoreCase))
			{
				inspectionSupplied = true;
			}
		}

		if (parametersAccepted && requested != initialInteractivity)
		{
			throw new GGNetUserException("Plot.Interactivity is initialization-only. Recreate the Plot component with a different @key to change its interaction options.");
		}

		initialInteractivity = requested;
		parametersAccepted = true;

		return base.SetParametersAsync(parameters);
	}

	private string? CssClass()
	{
		var @class = (string)(AdditionalAttributes?.GetValueOrDefault("class", string.Empty) ?? string.Empty);

		return (@class, loading) switch
		{
			("", true) => "ggnet loading",
			(_, true) => $"ggnet {@class} loading",
			("", false) => "ggnet",
			(_, false) => $"ggnet {@class}",
		};
	}

	protected override void OnInitialized()
	{
		var logger = (Services.GetService(typeof(ILoggerFactory)) as ILoggerFactory)?.CreateLogger("GGNet.Plot");

		RenderModeHandler = Rendering.RenderModeHandler.Factory(RenderMode, this, logger);

		Context.Init();

		reportedView = (Context.Positions.X.View, Context.Positions.Y.View);
		touchXView = Context.Positions.X.View;

		definitionsRenderModeHandler = RenderModeHandler?.Child();

		Compose(RenderTarget.Render);

		RequireReadableAxis();
	}

	protected override async Task OnParametersSetAsync()
	{
		base.OnParametersSet();

		if (!ReferenceEquals(renderedContext, Context) || renderedWidth != Width || renderedHeight != Height)
		{
			Context.Init();

			if (!ReferenceEquals(renderedContext, Context))
			{
				reportedView = (Context.Positions.X.View, Context.Positions.Y.View);

				if (InspectionEnabled)
				{
					Context.Render();

					RequireReadableAxis();
				}
			}

			await RefreshAsync(RenderTarget.Render, CancellationToken.None);
		}

		if (inspectionSupplied)
		{
			inspectionSupplied = false;

			if (StampsInspection && !EqualityComparer<Inspection<TX>?>.Default.Equals(inspection, Inspection))
			{
				touchEpoch++;
			}
			await InspectAsync(Inspection, notify: false);
		}
	}

	/// <summary>The current readout: the inspected position and one reading per inspected series. Null until something is inspected.</summary>
	public InspectionReadings<TX>? Readings => readings;

	/// <summary>Clears the crosshair and its readout, and reports the change through <c>InspectionChanged</c> and <c>ReadingsChanged</c>. This and the Escape key are the only ways an inspection ends.</summary>
	public Task ClearInspectionAsync() => InvokeAsync(async () =>
	{
		if (StampsInspection)
		{
			touchEpoch++;
		}
		await InspectAsync(null, notify: true, forceFrame: true);
	});

	internal bool SupportsSharedTouch => Context.CoordSystem == CoordSystem.Cartesian && !Context.Flip
		&& Context.Faceting is not { FreeX: true }
		&& Context.Positions.X.Scales.Count == 1;

	internal TouchInteractionOptions? TouchOptions => Interactivity?.ResolveTouch(SupportsSharedTouch);

	internal bool InspectionEnabled => Interactivity is { Crosshair: true } || TouchOptions is { Inspect: true };

	internal bool StampsInspection => TouchOptions is { Inspect: true } or { TapActivatesPanel: true } or { DoubleTapResetsView: true };

	internal bool StampsFrames => Interactivity is { Pan: true } || TouchOptions is { Enabled: true };

	internal bool InspectsSharedX => InspectionEnabled && SupportsSharedTouch;

	internal Inspection<TX>? CurrentInspection => inspection;

	internal Task RestoreInspectionAsync(bool forceFrame = false) => InspectAsync(pinned, notify: true, forceFrame);

	internal async Task InspectAsync(Inspection<TX>? value, bool notify, bool forceFrame = false)
	{
		if (!InspectionEnabled || EqualityComparer<Inspection<TX>?>.Default.Equals(inspection, value))
		{
			if (forceFrame)
			{
				AcknowledgeInspection();
			}
			return;
		}

		inspection = value;

		if (value is null or { Pinned: true })
		{
			pinned = value;
		}

		RefreshInspection();

		if (notify && InspectionChanged.HasDelegate)
		{
			await InspectionChanged.InvokeAsync(value);
		}

		await ReportReadingsAsync();
		AcknowledgeInspection();
	}

	private void AcknowledgeInspection()
	{
		var current = Read();
		if (publishingReadings || !(current is null ? readings is null : current.Equals(readings)))
		{
			return;
		}

		publishedTouchEpoch = touchEpoch;
		publishedInspectionRevision = touchInspectionRevision;
		RefreshInspection();
	}

	private void RefreshInspection()
	{
		for (var i = 0; i < Context.Panels.Count; i++)
		{
			Context.Panels[i].Component?.RefreshCursor();
		}
	}

	private IReadOnlyDictionary<string, object>? keyboard;

	private IReadOnlyDictionary<string, object>? Keyboard => Interactivity is { Crosshair: true }
		? keyboard ??= new Dictionary<string, object>
		{
			["tabindex"] = "0",
			["onkeydown"] = EventCallback.Factory.Create<KeyboardEventArgs>(this, OnKeyDownAsync)
		}
		: null;

	private Task OnKeyDownAsync(KeyboardEventArgs e) => e.Key switch
	{
		"Escape" => ClearInspectionAsync(),
		"ArrowRight" => StepAsync(forward: true),
		"ArrowLeft" => StepAsync(forward: false),
		_ => Task.CompletedTask
	};

	private Task StepAsync(bool forward)
	{
		if (!InspectsSharedX || !InspectAxis.Supports<TX>())
		{
			return Task.CompletedTask;
		}

		var scale = Context.Positions.X.Scales[0];
		var from = inspection is { } current
			? InspectAxis.Coordinate(current.X)
			: scale.Unmap(forward ? scale.Range.min : scale.Range.max) is { } edge
				? InspectAxis.Coordinate(edge)
				: double.NaN;

		if (double.IsNaN(from))
		{
			return Task.CompletedTask;
		}

		double? best = null;

		for (var i = 0; i < Context.Panels.Count; i++)
		{
			var geoms = Context.Panels[i].Geoms;

			for (var j = 0; j < geoms.Count; j++)
			{
				var series = geoms[j].StepSeries();

				for (var k = 0; k < series.Count; k++)
				{
					var candidate = forward ? series[k].After(from) : series[k].Before(from);

					if (candidate is { } value && (best is not { } held || (forward ? value < held : value > held)))
					{
						best = value;
					}
				}
			}
		}

		return best is { } next ? InspectAsync(new(InspectAxis.Value<TX>(next)), notify: true) : Task.CompletedTask;
	}

	private void RequireReadableAxis()
	{
		_ = TouchOptions;

		if (!InspectionEnabled)
		{
			return;
		}

		for (var i = 0; i < Context.Panels.Count; i++)
		{
			var geoms = Context.Panels[i].Geoms;

			for (var j = 0; j < geoms.Count; j++)
			{
				if (!geoms[j].Inspected)
				{
					continue;
				}

				if (!InspectAxis.Supports<TX>())
				{
					throw InspectAxis.Unsupported<TX>();
				}

				geoms[j].InspectSeries();
			}
		}
	}

	private Task ReportReadingsAsync()
	{
		var next = Read();

		if (next is null ? readings is null : next.Equals(readings))
		{
			return Task.CompletedTask;
		}

		readings = next ?? new(null, [], false);

		if (!ReadingsChanged.HasDelegate)
		{
			return Task.CompletedTask;
		}

		readingsPending = true;
		return publishingReadings ? Task.CompletedTask : PublishReadingsAsync();
	}

	private async Task PublishReadingsAsync()
	{
		publishingReadings = true;
		try
		{
			do
			{
				readingsPending = false;
				await ReadingsChanged.InvokeAsync(readings!);
			}
			while (readingsPending);
		}
		finally
		{
			publishingReadings = false;
		}
		AcknowledgeInspection();
	}

	private InspectionReadings<TX>? Read()
	{
		if (inspection is not { } current)
		{
			return readings is null ? null : new(null, [], false);
		}

		var scale = Context.Positions.X.Scales[0];
		var mapped = scale.Map(current.X);
		var outside = !(mapped >= scale.Range.min && mapped <= scale.Range.max);

		List<Reading<TX>> values = [];

		if (InspectsSharedX && InspectAxis.Supports<TX>())
		{
			var cursor = InspectAxis.Coordinate(current.X);

			for (var i = 0; i < Context.Panels.Count; i++)
			{
				var geoms = Context.Panels[i].Geoms;

				for (var j = 0; j < geoms.Count; j++)
				{
					var series = geoms[j].InspectSeries();

					for (var k = 0; k < series.Count; k++)
					{
						values.Add(InspectAxis.Reading<TX>(series[k].Read(cursor), cursor));
					}
				}
			}
		}

		return new(current, values, outside);
	}

	private async Task ReportReadingsSafelyAsync()
	{
		try
		{
			await InvokeAsync(ReportReadingsAsync);
			await InvokeAsync(AcknowledgeInspection);
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private bool Compose(RenderTarget target)
	{
		if (target == RenderTarget.Loading)
		{
			loading = true;
			return false;
		}

		loading = false;

		var epoch = touchEpoch;
		var revision = touchNavigationRevision;
		var commit = panCommit;
		Context.Render();
		RenderedTouchEpoch = epoch;
		RenderedTouchRevision = revision;
		RenderedPanCommit = commit;

		var zones = Layout.PlotLayout.Compute(new(
			Width: Width,
			Height: Height,
			Title: Context.Title,
			SubTitle: Context.SubTitle,
			Caption: Context.Caption,
			HasLegends: Context.Legends.Count > 0,
			LegendsDimension: Context.Legends.Count > 0 ? Context.Legends.Dimension() : default,
			Style: Context.Style!));

		wrapper = zones.Wrapper;
		Title = zones.Title;
		SubTitle = zones.SubTitle;
		Legend = zones.Legend;
		Caption = zones.Caption;

		renderedContext = Context;
		renderedWidth = Width;
		renderedHeight = Height;

		return true;
	}

	public void Render(RenderTarget target)
	{
		if (!Compose(target))
		{
			return;
		}

		definitionsRenderModeHandler?.Refresh();

		if (inspection is not null)
		{
			_ = ReportReadingsSafelyAsync();
		}
		else
		{
			_ = InvokeAsync(AcknowledgeInspection);
		}

		for (var i = 0; i < Context.Panels.Count; i++)
		{
			Context.Panels[i].Component?.Refresh();
		}
	}

	/// <summary>Zooms the x axis to an explicit data-unit window and re-renders. Last writer wins; the render is coalesced by the interactive channel.</summary>
	/// <param name="min">Window start, in x data units.</param>
	/// <param name="max">Window end, in x data units.</param>
	/// <param name="token">Cancels the refresh hand-off.</param>
	public Task ZoomToXAsync(TX min, TX max, CancellationToken token = default)
	{
		Context.SetXView(min, max);

		if (Interactivity is { AutoFitY: true })
		{
			Context.FitYToXView();
		}

		return CommandViewAsync(token);
	}

	/// <summary>Zooms the y axis to an explicit data-unit window and re-renders. Last writer wins; the render is coalesced by the interactive channel.</summary>
	/// <param name="min">Window start, in y data units.</param>
	/// <param name="max">Window end, in y data units.</param>
	/// <param name="token">Cancels the refresh hand-off.</param>
	public Task ZoomToYAsync(TY min, TY max, CancellationToken token = default)
	{
		Context.SetYView(min, max);

		return CommandViewAsync(token);
	}

	/// <summary>Clears the view windows on both axes and re-renders at the authored limits or the data extent.</summary>
	/// <param name="token">Cancels the refresh hand-off.</param>
	public Task ResetViewAsync(CancellationToken token = default)
	{
		Context.ResetView();

		return CommandViewAsync(token);
	}

	private Task<long> CommandViewAsync(CancellationToken token) => CommitViewAsync(ViewChangeCause.HostCommand, RenderTarget.Render, token, command: true);

	public Task StateHasChangedAsync() => InvokeAsync(StateHasChanged);

	protected override bool ShouldRender() => RenderModeHandler?.ShouldRender() ?? true;

	protected override void OnAfterRender(bool firstRender) => RenderModeHandler?.OnAfterRender(firstRender);

	public Task RefreshAsync(RenderTarget target, CancellationToken token) => CommitViewAsync(ViewChangeCause.HostCommand, target, token);

	internal async Task<long> CommitViewAsync(ViewChangeCause cause, RenderTarget target, CancellationToken token, bool touch = false, bool command = false)
	{
		var epoch = await RenderViewAsync(cause, target, token, touch, command);

		await ReportViewChangeAsync(cause);

		return epoch;
	}

	internal async Task ReportViewChangeAsync(ViewChangeCause cause)
	{
		if (ViewChanged.HasDelegate)
		{
			await InvokeAsync(() => ReportViewAsync(cause));
		}
	}

	internal async Task<long> RenderViewAsync(ViewChangeCause cause, RenderTarget target, CancellationToken token, bool touch = false, bool command = false, long pan = 0)
	{
		if (token.IsCancellationRequested)
		{
			await Task.FromCanceled(token);
		}

		var xView = Context.Positions.X.View;
		var supersedes = cause switch
		{
			ViewChangeCause.Gesture => !touch,
			ViewChangeCause.HostCommand => command || xView != touchXView,
			_ => true
		};
		if (target == RenderTarget.Render)
		{
			if (supersedes)
			{
				touchEpoch++;
			}
			touchXView = xView;
		}

		if (pan != 0)
		{
			panCommit = pan;
		}

		var epoch = touchEpoch;

		if (RenderModeHandler is not null)
		{
			// DEVIATION: forward the token (C# guide §11) — it was honoured above; a second check in the handler could refuse a hand-off whose epoch already advanced.
			await RenderModeHandler.RefreshAsync(target, CancellationToken.None);
		}

		return epoch;
	}

	private Task ReportViewAsync(ViewChangeCause cause)
	{
		var view = (Context.Positions.X.View, Context.Positions.Y.View);

		if (view.Equals(reportedView))
		{
			return Task.CompletedTask;
		}

		reportedView = view;

		return ViewChanged.InvokeAsync(new(cause, Context.XView, Context.YView));
	}

	private int disposing;

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		if (Interlocked.CompareExchange(ref disposing, 1, 0) == 1)
		{
			return ValueTask.CompletedTask;
		}

		if (RenderModeHandler is null)
		{
			return ValueTask.CompletedTask;
		}

		return RenderModeHandler.DisposeAsync();
	}
}

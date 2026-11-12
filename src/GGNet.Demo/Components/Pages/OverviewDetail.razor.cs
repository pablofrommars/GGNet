using GGNet.Components;

using Microsoft.JSInterop;

namespace GGNet.Demo.Components.Pages;

public partial class OverviewDetail
{
	private readonly RenderMode renderMode = RenderMode.Interactive;

	private readonly InteractivityOptions navigate = new() { Pan = true };

	private readonly InteractivityOptions inspect = new() { Crosshair = true, CursorTooltip = true };

	private PlotContext<TelemetryReading, Instant, double> overviewContext = default!;

	private PlotContext<TelemetryReading, Instant, double> detailContext = default!;

	private PlotContext<TelemetryReading, Instant, double> plainContext = default!;

	private PlotContext<TelemetryReading, Instant, double> inspectContext = default!;

	private Plot<TelemetryReading, Instant, double> overview = default!;

	private Plot<TelemetryReading, Instant, double> detail = default!;

	private bool open;

	private int activations;

	private string lastActivation = "—";

	private (Instant min, Instant max)? overviewWindow;

	private int overviewChanges;

	private string overviewCause = "—";

	private (Instant min, Instant max)? detailWindow;

	private int detailChanges;

	private string detailCause = "—";

	private int plainActivations;

	private int inspectActivations;

	private int inspectOpens;

	private readonly List<TelemetryReading> readings = [.. Telemetry.Readings];

	private IJSObjectReference? module;

	private DotNetObjectReference<OverviewDetail>? self;

	private bool measuring;

	private string measurement = "not measured";

	protected override void OnInitialized()
	{
		overviewContext = PlotContext.Build(readings, r => r.Ts, r => r.Temperature)
			.Scale_X_Instant()
			.Panel(p => p.Scale_Y_Continuous().Geom_Line(color: "#dc2626").Geom_Point(color: "#dc2626", size: 6).YLab("°C"), height: 0.5, onActivate: OnActivatedAsync, key: "temperature")
			.Panel(p => p.Scale_Y_Continuous().Geom_Line(y: r => r.Gravity, color: "#2563eb").YLab("SG"), height: 0.3, onActivate: OnActivatedAsync, key: "gravity")
			.Panel(p => p.Scale_Y_Continuous().Geom_Line(y: r => r.Pressure, color: "#059669").YLab("bar"), height: 0.2, onActivate: OnActivatedAsync, key: "pressure")
			.Title("Overview")
			.Style();

		detailContext = PlotContext.Build(readings, r => r.Ts, r => r.Temperature)
			.Scale_X_Instant()
			.Panel(p => p.Scale_Y_Continuous().Geom_Line(color: "#dc2626").Geom_Point(color: "#dc2626", size: 6).YLab("°C"), height: 0.75)
			.Panel(p => p.Scale_Y_Continuous(limits: (-1.2, 1.2), hide: true).Geom_Area(y: r => r.Activity, fill: "#f59e0b", fillOpacity: 0.6), height: 0.25)
			.Title("Temperature and equipment activity")
			.Style();

		plainContext = PlotContext.Build(Telemetry.Readings, r => r.Ts, r => r.Temperature)
			.Scale_X_Instant()
			.Panel(p => p.Geom_Point(color: "#dc2626", size: 6), onActivate: _ => CountAsync(() => plainActivations++), key: "plain")
			.Title("No interactivity")
			.Style();

		inspectContext = PlotContext.Build(Telemetry.Readings, r => r.Ts, r => r.Temperature)
			.Scale_X_Instant()
			.Panel(
				p => p.Geom_Point(
					color: "#dc2626",
					size: 6,
					tooltip: r => builder => builder.AddContent(0, FormattableString.Invariant($"{r.Temperature:0.0} °C"))),
				onActivate: _ => CountAsync(() => inspectActivations++),
				key: "inspect")
			.Title("Crosshair and tooltip")
			.Style();
	}

	private sealed record RefreshTimes(int Rounds, double Mean, double Max);

	private sealed record RefreshMeasurement(RefreshTimes Visible, RefreshTimes Both);

	private async Task MeasureAsync()
	{
		measuring = true;
		measurement = "measuring…";

		try
		{
			module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Pages/OverviewDetail.razor.js");
			self ??= DotNetObjectReference.Create(this);

			var result = await module.InvokeAsync<RefreshMeasurement>("measure", self, 20);

			measurement = FormattableString.Invariant(
				$"overview only: mean {result.Visible.Mean:0.0} ms, max {result.Visible.Max:0.0} ms, {result.Visible.Rounds}/20 · with hidden detail: mean {result.Both.Mean:0.0} ms, max {result.Both.Max:0.0} ms, {result.Both.Rounds}/20 · extra {result.Both.Mean - result.Visible.Mean:0.0} ms");
		}
		catch (Exception e) when (e is JSException or JSDisconnectedException or TaskCanceledException)
		{
			measurement = "measurement failed: " + e.Message;
		}
		finally
		{
			measuring = false;
		}
	}

	[JSInvokable]
	public async Task RefreshRoundAsync(bool hidden)
	{
		var last = readings[^1];

		readings[^1] = last with { Temperature = last.Temperature + 0.05 };

		await overview.RefreshAsync(RenderTarget.Render, CancellationToken.None);

		if (hidden)
		{
			await detail.RefreshAsync(RenderTarget.Render, CancellationToken.None);
		}
	}

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		self?.Dispose();

		if (module is not null)
		{
			try
			{
				await module.DisposeAsync();
			}
			catch (JSDisconnectedException)
			{
			}
		}
	}

	private Task CountAsync(Action count)
		=> InvokeAsync(() =>
		{
			count();

			StateHasChanged();
		});

	private static string Layer(bool shown)
		=> shown
			? "opacity: 1; transform: scale(1); transition: opacity 250ms, transform 250ms; pointer-events: auto;"
			: "opacity: 0; transform: scale(0.8); transition: opacity 250ms, transform 250ms; pointer-events: none;";

	private static string Format((Instant min, Instant max)? window)
		=> window is { } w
			? FormattableString.Invariant($"{w.min.ToUnixTimeSeconds()}..{w.max.ToUnixTimeSeconds()}")
			: "none";

	private void OnOverviewViewChanged(ViewChange<Instant, double> change)
	{
		overviewWindow = change.X;
		overviewChanges++;
		overviewCause = change.Cause.ToString();
	}

	private void OnDetailViewChanged(ViewChange<Instant, double> change)
	{
		detailWindow = change.X;
		detailChanges++;
		detailCause = change.Cause.ToString();
	}

	private async Task OnActivatedAsync(PanelActivation activation)
	{
		activations++;
		lastActivation = FormattableString.Invariant($"{activation.Index} {activation.Key}");

		if (activation.Key is "temperature")
		{
			await CarryAsync(overviewContext, detail);

			open = true;
		}

		StateHasChanged();
	}

	private async Task CloseAsync()
	{
		await CarryAsync(detailContext, overview);

		open = false;
	}

	private static Task CarryAsync(PlotContext<TelemetryReading, Instant, double> from, Plot<TelemetryReading, Instant, double> to)
		=> from.XView is { } window
			? to.ZoomToXAsync(window.min, window.max)
			: to.ResetViewAsync();
}

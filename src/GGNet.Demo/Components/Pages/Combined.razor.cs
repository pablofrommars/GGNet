using GGNet.Components;

namespace GGNet.Demo.Components.Pages;

public partial class Combined
{
	private readonly RenderMode renderMode = RenderMode.Interactive;

	private readonly InteractivityOptions interactivity = new() { Crosshair = true, Pan = true, CursorTooltip = true };

	private PlotContext<TimedReading, Instant, double> context = default!;

	private PlotContext<Reading, double, double> facetedContext = default!;

	private Plot<TimedReading, Instant, double> plot = default!;

	protected override void OnInitialized()
	{
		context = PlotContext.Build(SampleData.TimedReadings, r => r.Ts, r => r.Gravity)
			.Scale_X_Instant()
			.Geom_Point(tooltip: r => builder => builder.AddContent(0, FormattableString.Invariant($"{r.Gravity:0.000}")))
			.Title("Buttons, wheel, crosshair and reset on one view window")
			.YLab("Specific gravity")
			.Style();

		facetedContext = PlotContext.Build(SampleData.Readings, r => r.Hours, r => r.Gravity)
			.Geom_Point()
			.Facet_Wrap(r => r.Batch)
			.XLab("Hours")
			.Style();
	}

	private Task ZoomToMidWeekAsync()
		=> plot.ZoomToXAsync(SampleData.Start + Duration.FromDays(2), SampleData.Start + Duration.FromDays(4));

	private Task ShowLastTwoDaysAsync() => plot.ShowLastAsync(Duration.FromHours(48));

	private Task ResetAsync() => plot.ResetViewAsync();
}

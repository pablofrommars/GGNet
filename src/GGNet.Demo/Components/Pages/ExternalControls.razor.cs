using GGNet.Components;

namespace GGNet.Demo.Components.Pages;

public partial class ExternalControls
{
	private readonly RenderMode renderMode = RenderMode.Interactive;

	private PlotContext<TimedReading, Instant, double> context = default!;

	private Plot<TimedReading, Instant, double> plot = default!;

	protected override void OnInitialized()
	{
		context = PlotContext.Build(SampleData.TimedReadings, r => r.Ts, r => r.Gravity)
			.Scale_X_Instant()
			.Geom_Point(tooltip: r => builder => builder.AddContent(0, FormattableString.Invariant($"{r.Gravity:0.000}")))
			.Title("Fermentation — specific gravity over time")
			.YLab("Specific gravity")
			.Style();
	}

	private Task ZoomToMidWeekAsync()
		=> plot.ZoomToXAsync(SampleData.Start + Duration.FromDays(2), SampleData.Start + Duration.FromDays(4));

	private Task ShowLastTwoDaysAsync() => plot.ShowLastAsync(Duration.FromHours(48));

	private Task ResetAsync() => plot.ResetViewAsync();
}

using GGNet.Components;

using Microsoft.AspNetCore.Components.Web;

namespace GGNet.Demo.Components.Pages;

public partial class Touch
{
	private readonly RenderMode renderMode = RenderMode.Interactive;

	private readonly InteractivityOptions panX = new() { Pan = true };

	private readonly InteractivityOptions panY = new() { Pan = true, Zoom = ZoomAxis.Y };

	private readonly InteractivityOptions panBoth = new() { Pan = true, Zoom = ZoomAxis.Both };

	private readonly InteractivityOptions inspect = new() { Pan = true, Crosshair = true, CursorTooltip = true };

	private PlotContext<Reading, double, double> tapContext = default!;

	private PlotContext<Reading, double, double> panXContext = default!;

	private PlotContext<Reading, double, double> panYContext = default!;

	private PlotContext<Reading, double, double> panBothContext = default!;

	private PlotContext<Reading, double, double> tooltipContext = default!;

	private PlotContext<Reading, double, double> classicContext = default!;

	private Plot<Reading, double, double> tapPlot = default!;

	private int markClicks;

	private int ancestorClicks;

	private string lastMark = "—";

	protected override void OnInitialized()
	{
		tapContext = TouchCharts.Points("Tap a mark, tap the background, then drag", onclick: OnMarkClickAsync);
		panXContext = TouchCharts.Points("Pan x");
		panYContext = TouchCharts.Points("Pan y");
		panBothContext = TouchCharts.Points("Pan x and y");
		tooltipContext = TouchCharts.Points("Tap a mark for its tooltip", tooltip: TouchCharts.Tooltip);
		classicContext = TouchCharts.Points("Mark-anchored tooltip", tooltip: TouchCharts.Tooltip);
	}

	private Task OnMarkClickAsync(Reading reading, MouseEventArgs e)
	{
		markClicks++;
		lastMark = FormattableString.Invariant($"{reading.Batch} {reading.Hours} h");

		return InvokeAsync(StateHasChanged);
	}

	private void OnAncestorClick() => ancestorClicks++;

	private void ResetCounters()
	{
		markClicks = 0;
		ancestorClicks = 0;
		lastMark = "—";
	}

	private Task ResetViewAsync() => tapPlot.ResetViewAsync();
}

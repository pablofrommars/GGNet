using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace GGNet.Demo.Components.Pages;

public partial class TouchLayout
{
	private static readonly int[] widths = [100, 70, 45];

	private readonly RenderMode renderMode = RenderMode.Interactive;

	private readonly InteractivityOptions inspect = new() { Pan = true, CursorTooltip = true };

	private readonly InteractivityOptions inspectWithCrosshair = new() { Pan = true, Crosshair = true, CursorTooltip = true };

	private PlotContext<Reading, double, double> nestedContext = default!;

	private PlotContext<Reading, double, double> dialogContext = default!;

	private PlotContext<Reading, double, double> responsiveContext = default!;

	private PlotContext<Reading, double, double> mountContext = default!;

	private ContactDiagnostics diagnostics = default!;

	private ElementReference dialog;

	private bool dialogOpen;

	private bool dialogPending;

	private int dialogOpens;

	private int width = 100;

	private bool mounted = true;

	private int mounts = 1;

	private int markClicks;

	protected override void OnInitialized()
	{
		nestedContext = TouchCharts.Points("Inside a scroll container", tooltip: TouchCharts.Tooltip);
		responsiveContext = TouchCharts.Points("Resize the container", tooltip: TouchCharts.Tooltip);
		mountContext = BuildMountContext();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (dialogPending)
		{
			dialogPending = false;

			await diagnostics.ShowModalAsync(dialog);
		}
	}

	private PlotContext<Reading, double, double> BuildMountContext()
		=> TouchCharts.Points("Tap a mark after remounting", onclick: OnMarkClickAsync, tooltip: TouchCharts.Tooltip);

	private void OpenDialog()
	{
		dialogContext = TouchCharts.Points("Inside a modal dialog", tooltip: TouchCharts.Tooltip);
		dialogOpen = true;
		dialogPending = true;
		dialogOpens++;
	}

	private void CloseDialog() => dialogOpen = false;

	private void ToggleMount()
	{
		mounted = !mounted;

		if (mounted)
		{
			mounts++;
			mountContext = BuildMountContext();
		}
	}

	private Task OnMarkClickAsync(Reading reading, MouseEventArgs e)
	{
		markClicks++;

		return InvokeAsync(StateHasChanged);
	}
}

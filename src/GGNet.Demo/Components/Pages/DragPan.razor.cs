namespace GGNet.Demo.Components.Pages;

public partial class DragPan
{
	private readonly RenderMode renderMode = RenderMode.Interactive;

	private readonly InteractivityOptions fitted = new() { Pan = true, AutoFitY = true };

	private readonly InteractivityOptions plain = new() { Pan = true };

	private PlotContext<Reading, double, double> fittedContext = default!;

	private PlotContext<Reading, double, double> plainContext = default!;

	protected override void OnInitialized()
	{
		fittedContext = Build("Auto-fit y — drag to pan, y follows the window");
		plainContext = Build("Fixed y — drag to pan, y keeps the data range");
	}

	private static PlotContext<Reading, double, double> Build(string title)
		=> PlotContext.Build(SampleData.Readings, r => r.Hours, r => r.Gravity)
			.Scale_Color_Discrete(r => r.Batch, ["#2563eb", "#f59e0b"], name: "Batch")
			.Geom_Point()
			.Title(title)
			.XLab("Hours")
			.YLab("Specific gravity")
			.Style();
}

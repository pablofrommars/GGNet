namespace GGNet.Demo.Components.Pages;

public partial class Crosshair
{
	private readonly RenderMode renderMode = RenderMode.Interactive;

	private readonly InteractivityOptions interactivity = new() { Crosshair = true };

	private PlotContext<Reading, double, double> context = default!;

	protected override void OnInitialized()
	{
		context = PlotContext.Build(SampleData.Readings, r => r.Hours, r => r.Gravity)
			.Scale_Color_Discrete(r => r.Batch, ["#2563eb", "#f59e0b"], name: "Batch")
			.Geom_Point()
			.Title("Fermentation — hover for the crosshair readout")
			.XLab("Hours")
			.YLab("Specific gravity")
			.Style();
	}
}

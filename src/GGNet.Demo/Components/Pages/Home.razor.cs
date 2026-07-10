namespace GGNet.Demo.Components.Pages;

public partial class Home
{
	// GGNet.RenderMode: named here (not in markup) to avoid the ambiguity with
	// Microsoft.AspNetCore.Components.Web.RenderMode imported by _Imports.razor.
	private readonly RenderMode renderMode = RenderMode.Interactive;

	private PlotContext<Reading, double, double> context = default!;

	protected override void OnInitialized()
	{
		context = PlotContext.Build(SampleData.Readings, r => r.Hours, r => r.Gravity)
			.Scale_Color_Discrete(r => r.Batch, ["#2563eb", "#f59e0b"], name: "Batch")
			.Geom_Point(tooltip: r => builder => builder.AddContent(0, FormattableString.Invariant($"{r.Batch}: {r.Gravity:0.000} at {r.Hours}h")))
			.Title("Fermentation — specific gravity")
			.XLab("Hours")
			.YLab("Specific gravity")
			.Style();
	}
}

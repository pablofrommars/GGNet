using Microsoft.AspNetCore.Components;

namespace GGNet.Demo.Components.Pages;

public partial class Tooltips
{
	private readonly RenderMode renderMode = RenderMode.Interactive;

	private readonly InteractivityOptions glued = new() { CursorTooltip = true };

	private PlotContext<Reading, double, double> classicContext = default!;

	private PlotContext<Reading, double, double> gluedContext = default!;

	private PlotContext<Reading, double, double> richContext = default!;

	protected override void OnInitialized()
	{
		classicContext = Build("Mark-anchored — hover edge points to see the flip", Simple);
		gluedContext = Build("Cursor-glued — the bubble follows the pointer", Simple);
		richContext = Build("Rich content — any markup", reading => richTemplate(reading));
	}

	private static RenderFragment Simple(Reading reading)
		=> builder => builder.AddContent(0, FormattableString.Invariant($"{reading.Batch}: {reading.Gravity:0.000}"));

	private static PlotContext<Reading, double, double> Build(string title, Func<Reading, RenderFragment> tooltip)
		=> PlotContext.Build(SampleData.Readings, r => r.Hours, r => r.Gravity)
			.Scale_Color_Discrete(r => r.Batch, ["#2563eb", "#f59e0b"], name: "Batch")
			.Geom_Point(tooltip: tooltip)
			.Title(title)
			.XLab("Hours")
			.YLab("Specific gravity")
			.Style();
}

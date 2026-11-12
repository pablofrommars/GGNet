using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace GGNet.Demo.Data;

public static class TouchCharts
{
	public static PlotContext<Reading, double, double> Points(
		string title,
		Func<Reading, MouseEventArgs, Task>? onclick = null,
		Func<Reading, RenderFragment>? tooltip = null)
		=> PlotContext.Build(SampleData.Readings, r => r.Hours, r => r.Gravity)
			.Scale_Color_Discrete(r => r.Batch, ["#2563eb", "#f59e0b"], name: "Batch")
			.Geom_Point(onclick: onclick, tooltip: tooltip, size: 9)
			.Title(title)
			.XLab("Hours")
			.YLab("Specific gravity")
			.Style();

	public static RenderFragment Tooltip(Reading reading)
		=> builder => builder.AddContent(0, FormattableString.Invariant($"{reading.Batch} · {reading.Hours} h · {reading.Gravity:0.000}"));
}

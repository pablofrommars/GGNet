namespace GGNet.Headless.Tests;

// Tier-C chart compositions (dot plot, dumbbell, waffle): supported by the
// selector without pinned gallery examples — decided in plan/PLAN.md 1.2.
// These tests keep their skill-documented recipes compiling and rendering
// (structure verified, no byte pin); the csharp blocks in
// skills/ggnet/reference/geoms.md §multi-layer must match this source
// (SkillExampleConsistencyTests).
public class TierCCompositionTests
{
	public enum Team { Alpha, Beta, Gamma, Delta }

	private sealed record Kpi(Team Team, double Value);

	private static readonly Kpi[] kpis =
	[
		new(Team.Alpha, 3.2),
		new(Team.Beta, 4.1),
		new(Team.Gamma, 2.6),
		new(Team.Delta, 3.8)
	];

	private sealed record Change(Team Team, double Before, double After);

	private static readonly Change[] changes =
	[
		new(Team.Alpha, 2.0, 3.5),
		new(Team.Beta, 4.0, 2.5),
		new(Team.Gamma, 3.0, 3.2)
	];

	private sealed record Unit(double Column, double Row, string Part);

	private static readonly Unit[] units =
	[
		.. from index in Enumerable.Range(0, 100)
		   select new Unit(index % 10, index / 10, index < 42 ? "a" : index < 77 ? "b" : "c")
	];

	private static async Task RenderPlot(IPlotContext plot)
	{
		var svg = await plot.AsStringAsync();

		XDocument.Parse(svg);

		svg.Should().Contain("<svg");
	}

	[Fact]
	public Task DotPlot()
		=> RenderPlot(PlotContext.Build(kpis, k => k.Value, k => k.Team)
			.Geom_Point()
			.Style());

	[Fact]
	public Task Dumbbell()
		=> RenderPlot(PlotContext.Build(changes, c => c.Before, c => c.Team)
			.Geom_Segment(c => c.Before, c => c.After, c => c.Team, c => c.Team)
			.Geom_Point()
			.Geom_Point(x: c => c.After)
			.Style());

	[Fact]
	public Task Waffle()
		=> RenderPlot(PlotContext.Build(units, u => u.Column, u => u.Row)
			.Scale_Fill_Discrete(u => u.Part, ["#23d0fc", "#fc9d23", "#8b5cf6"])
			.Geom_Tile(u => u.Column, u => u.Row, u => 0.95, u => 0.95)
			.Style());
}

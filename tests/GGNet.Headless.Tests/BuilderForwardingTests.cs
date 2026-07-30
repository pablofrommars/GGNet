namespace GGNet.Headless.Tests;

// The overload families forward positionally; Line is the one family with a
// parameter after inherit (piecewise), where a missing argument silently
// shifted piecewise into the inherit slot — disabling aesthetic inheritance
// and dropping piecewise. These tests pin both forwarding paths.
public class BuilderForwardingTests
{
	private sealed record Grouped(double Pos, double Value, double Series);

	private static readonly Grouped[] grouped =
	[
		new(1, 2.0, 1), new(1, 3.0, 2),
		new(2, 4.0, 1), new(2, 1.0, 2),
		new(3, 3.0, 1), new(3, 2.0, 2)
	];

	private sealed record XY(double X, double Y);

	private static readonly XY[] gapped =
	[
		new(1, 2.0),
		new(2, 3.0),
		new(3, double.NaN),
		new(4, 2.5),
		new(5, 3.5)
	];

	[Fact]
	public async Task LineInheritsColorScale()
	{
		// Arrange

		var plot = PlotContext.Build(grouped, i => i.Pos, i => i.Value)
			.Scale_Color_Discrete(i => i.Series, ["#23d0fc", "#fc9d23"])
			.Geom_Line()
			.Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		svg.Should().Contain("legend-label");
		svg.Should().Contain("stroke=\"#23d0fc\"")
			.And.Contain("stroke=\"#fc9d23\"");
	}

	[Fact]
	public async Task LinePiecewiseForwards()
	{
		// Arrange

		var plot = PlotContext.Build(gapped, i => i.X, i => i.Y)
			.Geom_Line(piecewise: true)
			.Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		var d = System.Text.RegularExpressions.Regex.Match(svg, "<path d=\"([^\"]+)\"").Groups[1].Value;

		d.Should().NotBeEmpty();
		System.Text.RegularExpressions.Regex.Count(d, " M ").Should().Be(2, "the NaN gap lifts the pen into a second subpath");
	}
}

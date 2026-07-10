namespace GGNet.Components.Tests;

// Phase 0 reference harness: proves bUnit can render a GGNet Plot to SVG and
// dispatch a mark event through the circuit. The two patterns every Phase 2
// component/circuit test builds on — render + assert markup, and fire + assert
// handler. Rendered with RenderMode.InteractiveAuto: a synchronous inline
// handler, so re-render completes in the dispatch turn and assertions need no
// WaitForState.
public class HarnessSmokeTests
{
	private sealed record XY(double X, double Y);

	private static readonly XY[] xy = [new(1.0, 2.0), new(2.0, 3.5)];

	[Fact]
	public void RenderPlot()
	{
		// Arrange

		using var ctx = new BunitContext();

		var context = PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Point()
			.Style();

		// Act

		var cut = ctx.Render<Plot<XY, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto));

		// Assert

		using var _ = new AssertionScope();

		cut.Markup.Should().Contain("<svg").And.Contain("viewBox");
		cut.FindAll("rect.panel").Should().NotBeEmpty();
	}

	[Fact]
	public void MarkClickInvokesHandler()
	{
		// Arrange

		using var ctx = new BunitContext();

		var clicked = false;

		var context = PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Point(onclick: (item, e) =>
			{
				clicked = true;
				return Task.CompletedTask;
			})
			.Style();

		var cut = ctx.Render<Plot<XY, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto));

		// Act

		cut.Find("circle").Click();

		// Assert

		clicked.Should().BeTrue();
	}
}

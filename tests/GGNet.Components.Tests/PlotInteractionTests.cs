namespace GGNet.Components.Tests;

// Phase 2 — the circuit: mark events, panel clicks, and tooltip hover, driven
// through real Blazor events with bUnit. The roadmap's untested triad.
public class PlotInteractionTests : BunitContext
{
	private sealed record P(double X, double Y);

	private static readonly P[] data = [new(1.0, 2.0), new(2.0, 3.5)];

	private IRenderedComponent<Plot<P, double, double>> Render(PlotContext<P, double, double> context)
		=> Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto));

	[Fact]
	public void PointMouseOverAndMouseOutInvokeHandlers()
	{
		// Arrange

		var over = false;
		var out_ = false;

		var context = PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Point(
				onmouseover: (item, e) => { over = true; return Task.CompletedTask; },
				onmouseout: (item, e) => { out_ = true; return Task.CompletedTask; })
			.Style();

		var cut = Render(context);
		var circle = cut.Find("circle");

		// Act

		circle.MouseOver();
		circle.MouseOut();

		// Assert

		using var _ = new AssertionScope();

		over.Should().BeTrue();
		out_.Should().BeTrue();
	}

	[Fact]
	public void BarClickInvokesHandler()
	{
		// Arrange

		var clicked = false;

		var context = PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Bar(onclick: (item, e) => { clicked = true; return Task.CompletedTask; }, animation: true)
			.Style();

		var cut = Render(context);

		// Act

		cut.Find("rect.animate-bar").Click();

		// Assert

		clicked.Should().BeTrue();
	}

	[Fact]
	public void PanelClickInvokesHandler()
	{
		// Arrange

		var clicked = false;

		var context = PlotContext.Build(data, i => i.X, i => i.Y)
			.Panel(p => p.Geom_Point(), onClick: e => { clicked = true; return Task.CompletedTask; })
			.Style();

		var cut = Render(context);

		// Act

		cut.Find("rect.panel").Click();

		// Assert

		clicked.Should().BeTrue();
	}

	[Fact]
	public void PanelClickWithoutHandlerIsNoop()
	{
		// Arrange

		var cut = Render(PlotContext.Build(data, i => i.X, i => i.Y).Geom_Point().Style());

		// Act

		Action act = () => cut.Find("rect.panel").Click();

		// Assert

		act.Should().NotThrow();
	}

	[Fact]
	public void HoverShowsTooltipAndMouseOutHidesIt()
	{
		// Arrange

		var context = PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Point(tooltip: item => builder => builder.AddContent(0, "tip"))
			.Style();

		var cut = Render(context);
		var circle = cut.Find("circle");

		// Act / Assert

		cut.FindAll("foreignObject").Should().BeEmpty();

		circle.MouseOver();
		using (new AssertionScope())
		{
			cut.FindAll("foreignObject").Should().NotBeEmpty();
			cut.Markup.Should().Contain("tip");
		}

		circle.MouseOut();
		cut.FindAll("foreignObject").Should().BeEmpty();
	}
}

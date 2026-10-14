namespace GGNet.Components.Tests;

// The circuit: mark events, panel clicks, and tooltip hover, driven
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

		// Act

		var beforeHover = cut.FindAll("foreignObject").Count;

		circle.MouseOver();
		var whileHovered = cut.FindAll("foreignObject").Count;
		var hoveredMarkup = cut.Markup;

		circle.MouseOut();
		var afterMouseOut = cut.FindAll("foreignObject").Count;

		// Assert

		using var _ = new AssertionScope();

		beforeHover.Should().Be(0);
		whileHovered.Should().BePositive();
		hoveredMarkup.Should().Contain("tip");
		afterMouseOut.Should().Be(0);
	}
}

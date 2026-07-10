using System.Text.RegularExpressions;

namespace GGNet.Components.Tests;

// Phase 2 — component rendering. Rendered with InteractiveAuto (synchronous
// inline handler) so markup is available immediately, no WaitForState. Inherits
// BunitContext: xUnit constructs/disposes it per test.
public class PlotComponentTests : BunitContext
{
	private sealed record P(double X, double Y, string G);

	private static readonly P[] data =
	[
		new(1.0, 2.0, "a"),
		new(2.0, 3.5, "a"),
		new(3.0, 2.8, "b"),
		new(4.0, 4.2, "b")
	];

	private static PlotContext<P, double, double> PointPlot()
		=> PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Point()
			.Style();

	private IRenderedComponent<Plot<P, double, double>> Render(PlotContext<P, double, double> context, RenderMode mode)
		=> Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, mode));

	// Strip the two instance-varying tokens bUnit's raw markup carries: the
	// per-plot Guid Id, and Blazor's internal event-handler ids (blazor:onX="N",
	// renumbered each render — not part of the SVG; Headless drops handlers).
	private static string Normalize(string markup, string id)
		=> Regex.Replace(markup.Replace(id, "ID"), "blazor:on\\w+=\"\\d+\"", "blazor:evt");

	[Fact]
	public void RendersSvgWithPanel()
	{
		// Arrange / Act

		var cut = Render(PointPlot(), RenderMode.InteractiveAuto);

		// Assert

		using var _ = new AssertionScope();

		cut.Markup.Should().Contain("<svg").And.Contain("viewBox=\"0 0 720 576\"");
		cut.FindAll("rect.panel").Should().ContainSingle();
		cut.FindAll("circle").Should().HaveCount(data.Length);
	}

	[Fact]
	public void RendersLegendForColorScale()
	{
		// Arrange

		var context = PlotContext.Build(data, i => i.X, i => i.Y)
			.Scale_Color_Discrete(i => i.G, ["#111111", "#222222"], name: "grp")
			.Geom_Point()
			.Style();

		// Act

		var cut = Render(context, RenderMode.InteractiveAuto);

		// Assert

		cut.FindAll("text.legend-label").Should().NotBeEmpty();
	}

	[Fact]
	public void StaticAndInteractiveAutoProduceSameMarkup()
	{
		// Arrange

		var forStatic = PointPlot();
		var forAuto = PointPlot();

		// Act

		var staticCut = Render(forStatic, RenderMode.Static);
		var autoCut = Render(forAuto, RenderMode.InteractiveAuto);

		// Assert

		var staticMarkup = Normalize(staticCut.Markup, forStatic.Id);
		var autoMarkup = Normalize(autoCut.Markup, forAuto.Id);

		autoMarkup.Should().Be(staticMarkup);
	}

	[Fact]
	public void ReRenderIsIdempotent()
	{
		// Arrange

		var context = PointPlot();
		var cut = Render(context, RenderMode.InteractiveAuto);
		var before = Normalize(cut.Markup, context.Id);

		// Act

		cut.Render();

		// Assert

		Normalize(cut.Markup, context.Id).Should().Be(before);
	}
}

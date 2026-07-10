using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Components.Web;

namespace GGNet.Components.Tests;

// The opt-in gate and the wheel gesture (implementation-blocks Blocks 5 + the
// responsive upgrade). The §0 acceptance: without Interactivity the markup
// carries no gesture scaffolding; with it the capture group appears and the
// svg stays responsive — wheel coordinates arrive from the JS module in svg
// units, so the commit path is exercised by invoking the JSInvokable directly.
public class PlotInteractivityTests : BunitContext
{
	public PlotInteractivityTests()
	{
		// Any opt-in initializes the interactivity module.
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	private sealed record P(double X, double Y);

	private static readonly P[] data =
	[
		new(1.0, 2.0),
		new(2.0, 3.5),
		new(3.0, 2.8),
		new(4.0, 4.2)
	];

	private static PlotContext<P, double, double> PointPlot()
		=> PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Point()
			.Style();

	private IRenderedComponent<Plot<P, double, double>> Render(PlotContext<P, double, double> context, InteractivityOptions? interactivity = null)
		=> Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, interactivity));

	// Also scrub element-reference capture ids: they vary per render.
	private static string Normalize(string markup, string id)
		=> Regex.Replace(
			Regex.Replace(markup.Replace(id, "ID"), "blazor:on\\w+=\"\\d+\"", "blazor:evt"),
			"blazor:elementreference=\"[^\"]*\"", "blazor:ref", RegexOptions.IgnoreCase);

	[Fact]
	public void OptInOffEmitsNoScaffolding()
	{
		// Arrange / Act

		var cut = Render(PointPlot());

		// Assert

		using var _ = new AssertionScope();

		cut.Markup.Should().NotContain("blazor:ondblclick").And.NotContain("blazor:onmouseleave");
		Regex.IsMatch(cut.Markup, "<svg[^>]*\\swidth=").Should().BeFalse();
	}

	[Fact]
	public void OptInOnEmitsTheCaptureGroupAndKeepsTheSvgResponsive()
	{
		// Arrange / Act

		var cut = Render(PointPlot(), new InteractivityOptions());

		// Assert

		using var _ = new AssertionScope();

		cut.Markup.Should().Contain("blazor:ondblclick");
		Regex.IsMatch(cut.Markup, "<svg[^>]*\\swidth=").Should().BeFalse();
	}

	[Fact]
	public async Task WheelZoomsAboutTheCursorAndDoubleClickResets()
	{
		// Arrange

		var context = PointPlot();
		var cut = Render(context, new InteractivityOptions { Zoom = ZoomAxis.Both });

		var baseline = Normalize(cut.Markup, context.Id);

		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;

		// Act

		// The callback the JS module fires per wheel notch, in svg units.
		await cut.InvokeAsync(() => panel.OnWheelAsync(360.0, 280.0, -100.0));
		var zoomed = Normalize(cut.Markup, context.Id);

		await cut.Find("rect.panel").TriggerEventAsync("ondblclick", new MouseEventArgs());
		var restored = Normalize(cut.Markup, context.Id);

		// Assert

		using var _ = new AssertionScope();

		zoomed.Should().NotBe(baseline);
		restored.Should().Be(baseline);
	}

	private sealed record G(double X, double Y, string Key);

	private static readonly G[] grouped =
	[
		new(1.0, 2.0, "a"),
		new(2.0, 3.5, "a"),
		new(3.0, 2.8, "b"),
		new(4.0, 4.2, "b")
	];

	[Fact]
	public async Task FacetedWheelZoomSurvivesReInstancedScales()
	{
		// Arrange

		// Faceted passes re-instance every scale; the view window lives on the
		// axis container and must be stamped onto the fresh instances.
		var context = PlotContext.Build(grouped, i => i.X, i => i.Y)
			.Geom_Point()
			.Facet_Wrap(i => i.Key)
			.Style();

		var cut = Render<Plot<G, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, new InteractivityOptions()));

		var baseline = Normalize(cut.Markup, context.Id);

		var panel = cut.FindComponents<Panel<G, double, double>>()[0].Instance;

		// Act

		// Facet panels stack vertically; land inside the first panel's area.
		await cut.InvokeAsync(() => panel.OnWheelAsync(200.0, 150.0, -100.0));
		var zoomed = Normalize(cut.Markup, context.Id);

		await cut.FindAll("rect.panel")[0].TriggerEventAsync("ondblclick", new MouseEventArgs());
		var restored = Normalize(cut.Markup, context.Id);

		// Assert

		using var _ = new AssertionScope();

		zoomed.Should().NotBe(baseline);
		restored.Should().Be(baseline);
	}

	[Fact]
	public async Task WheelOutsideThePlottingAreaIsIgnored()
	{
		// Arrange

		var context = PointPlot();
		var cut = Render(context, new InteractivityOptions());

		var baseline = Normalize(cut.Markup, context.Id);

		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;

		// Act

		// Coordinates over the axis bands or margins must not zoom.
		await cut.InvokeAsync(() => panel.OnWheelAsync(2.0, 2.0, -100.0));

		// Assert

		Normalize(cut.Markup, context.Id).Should().Be(baseline);
	}
}

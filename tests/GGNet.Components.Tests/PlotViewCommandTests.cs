using System.Text.RegularExpressions;

namespace GGNet.Components.Tests;

// The imperative view commands (implementation-blocks Block 4): host code
// drives the view window through the Plot component's @ref surface; each
// command mutates context state and re-renders through the handler.
public class PlotViewCommandTests : BunitContext
{
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

	private IRenderedComponent<Plot<P, double, double>> Render(PlotContext<P, double, double> context)
		=> Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto));

	// Strip the two instance-varying tokens bUnit's raw markup carries: the
	// per-plot Guid Id, and Blazor's internal event-handler ids.
	private static string Normalize(string markup, string id)
		=> Regex.Replace(markup.Replace(id, "ID"), "blazor:on\\w+=\"\\d+\"", "blazor:evt");

	[Fact]
	public async Task ZoomToXThenResetRestoresTheView()
	{
		// Arrange

		var context = PointPlot();
		var cut = Render(context);

		var baseline = Normalize(cut.Markup, context.Id);

		// Act

		await cut.InvokeAsync(() => cut.Instance.ZoomToXAsync(2.0, 3.0));
		var windowed = Normalize(cut.Markup, context.Id);

		await cut.InvokeAsync(() => cut.Instance.ResetViewAsync());
		var restored = Normalize(cut.Markup, context.Id);

		// Assert

		using var _ = new AssertionScope();

		windowed.Should().NotBe(baseline);
		restored.Should().Be(baseline);
	}

	[Fact]
	public async Task ZoomToYAsyncWindowsTheYAxis()
	{
		// Arrange

		var context = PointPlot();
		var cut = Render(context);

		var baseline = Normalize(cut.Markup, context.Id);

		// Act

		await cut.InvokeAsync(() => cut.Instance.ZoomToYAsync(2.5, 3.0));

		// Assert

		Normalize(cut.Markup, context.Id).Should().NotBe(baseline);
	}
}

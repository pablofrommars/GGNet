using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Components.Web;

namespace GGNet.Components.Tests;

// Drag-pan (implementation-blocks Block 8): the JS module owns the gesture, so
// bUnit verifies the .NET half — interop is initialized only under the Pan
// opt-in, and the gesture-end callback commits a window shift. The executed-JS
// half belongs to the deferred Playwright smoke layer.
public class PanInteropTests : BunitContext
{
	private const string ModulePath = "./_content/GGNet/Components/Panel.razor.js";

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

	private IRenderedComponent<Plot<P, double, double>> Render(PlotContext<P, double, double> context, InteractivityOptions interactivity)
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
	public void PanOptInInitializesTheModule()
	{
		// Arrange

		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("initialize", _ => true).SetVoidResult();

		// Act

		var cut = Render(PointPlot(), new InteractivityOptions { Pan = true });

		// Assert

		cut.WaitForAssertion(() => module.Invocations.Should().ContainSingle(i => i.Identifier == "initialize"));
	}

	[Fact]
	public void WithoutTheOptInNoInteropHappens()
	{
		// Arrange / Act

		// Strict JSInterop: an unexpected call would throw during render.
		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, PointPlot())
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto));

		// Assert

		JSInterop.Invocations.Should().BeEmpty();
	}

	[Fact]
	public async Task AutoFitYDerivesTheYWindowFromThePanCommit()
	{
		// Arrange

		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("initialize", _ => true).SetVoidResult();

		var fitted = Render(PointPlot(), new InteractivityOptions { Pan = true, AutoFitY = true });
		var plain = Render(PointPlot(), new InteractivityOptions { Pan = true });

		// Act

		// The same x-only pan commit on both plots; only the fitted one may
		// move its y axis.
		await fitted.InvokeAsync(() => fitted.FindComponent<Panel<P, double, double>>().Instance.OnPanEndAsync(80.0, 0.0));
		await plain.InvokeAsync(() => plain.FindComponent<Panel<P, double, double>>().Instance.OnPanEndAsync(80.0, 0.0));

		// Assert

		// Identical x windows, so any markup difference is the y refit.
		Normalize(fitted.Markup, fitted.Instance.Context.Id)
			.Should().NotBe(Normalize(plain.Markup, plain.Instance.Context.Id));
	}

	[Fact]
	public async Task PanEndCommitsAWindowShiftAndDoubleClickResets()
	{
		// Arrange

		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("initialize", _ => true).SetVoidResult();

		var context = PointPlot();
		var cut = Render(context, new InteractivityOptions { Pan = true });

		var baseline = Normalize(cut.Markup, context.Id);

		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;

		// Act

		// The callback the JS module fires on pointerup, invoked directly.
		await cut.InvokeAsync(() => panel.OnPanEndAsync(80.0, 0.0));
		var panned = Normalize(cut.Markup, context.Id);

		await cut.Find("rect.panel").TriggerEventAsync("ondblclick", new MouseEventArgs());
		var restored = Normalize(cut.Markup, context.Id);

		// Assert

		using var _ = new AssertionScope();

		panned.Should().NotBe(baseline);
		restored.Should().Be(baseline);
	}
}

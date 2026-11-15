using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Components.Web;

namespace GGNet.Components.Tests;

// Drag-pan: the JS module owns the gesture, so
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

	// Capture ids and frame stamps vary even when reset restores identical geometry.
	private static string Normalize(string markup, string id)
	{
		markup = Regex.Replace(markup, "data-ggnet-view=\"[^\"]*\"", "data-ggnet-view=\"VIEW\"");
		return Regex.Replace(
			Regex.Replace(
				Regex.Replace(markup.Replace(id, "ID"), "data-ggnet-(marks-)?frame=\"[^\"]*\"", "data-ggnet-$1frame=\"FRAME\""),
				"blazor:on\\w+=\"\\d+\"", "blazor:evt"),
			"blazor:elementreference=\"[^\"]*\"", "blazor:ref", RegexOptions.IgnoreCase);
	}

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
		// Arrange — strict JSInterop: an unexpected call would throw during render.

		var context = PointPlot();

		// Act

		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
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
	public async Task PanEndAnswersWithTheEpochOfItsOwnCommit()
	{
		// Arrange — the host clears the selection from its view callback, which advances the epoch
		// again, without a frame, before the commit completes.

		JSInterop.SetupModule(ModulePath).SetupVoid("initialize", _ => true).SetVoidResult();
		IRenderedComponent<Plot<P, double, double>>? cut = null;
		cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, PointPlot())
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, new InteractivityOptions { Pan = true, Crosshair = true })
			.Add(p => p.ViewChanged, (ViewChange<double, double> _) => cut!.Instance.ClearInspectionAsync()));
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var before = RenderedEpoch(cut);

		// Act

		var answered = await cut.InvokeAsync(() => panel.OnPanEndAsync(80.0, 0.0));

		// Assert

		using var _ = new AssertionScope();
		answered.Should().Be(before + 1);
		RenderedEpoch(cut).Should().Be(answered, "the frame that rendered the commit carries the answered epoch");
	}

	private static long RenderedEpoch(IRenderedComponent<Plot<P, double, double>> cut)
		=> long.Parse(cut.Find("[data-ggnet-marks-frame]").GetAttribute("data-ggnet-view")!.Split(' ')[1], System.Globalization.CultureInfo.InvariantCulture);

	[Theory]
	[InlineData(RenderMode.InteractiveAuto)]
	[InlineData(RenderMode.Interactive)]
	public async Task ThePanFrameNamesItsCommitWhileTheAnswerStillWaitsForTheHost(RenderMode mode)
	{
		// Arrange — the host's view callback does not return until released, which holds the answer.

		JSInterop.SetupModule(ModulePath).SetupVoid("initialize", _ => true).SetVoidResult();
		var host = new TaskCompletionSource();
		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, PointPlot())
			.Add(p => p.RenderMode, mode)
			.Add(p => p.Interactivity, new InteractivityOptions { Pan = true })
			.Add(p => p.ViewChanged, (ViewChange<double, double> _) => host.Task));
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var before = cut.Find("[data-ggnet-marks-frame]").GetAttribute("data-ggnet-commit");

		// Act

		var answer = cut.InvokeAsync(() => panel.OnPanEndAsync(80.0, 0.0, 7));
		cut.WaitForAssertion(() => cut.Find("[data-ggnet-marks-frame]").GetAttribute("data-ggnet-commit").Should().Be("7"));
		var waiting = answer.IsCompleted;
		var rendered = RenderedEpoch(cut);
		host.SetResult();
		var answered = await answer;

		// Assert

		using var _ = new AssertionScope();
		before.Should().BeNull("no pan has been committed");
		waiting.Should().BeFalse();
		answered.Should().Be(rendered);
	}

	[Fact]
	public async Task AThrowingViewCallbackStillAnswersThePanWithItsEpoch()
	{
		// Arrange

		JSInterop.SetupModule(ModulePath).SetupVoid("initialize", _ => true).SetVoidResult();
		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, PointPlot())
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, new InteractivityOptions { Pan = true })
			.Add(p => p.ViewChanged, (ViewChange<double, double> _) => throw new InvalidOperationException("host failure")));
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var before = RenderedEpoch(cut);

		// Act

		var answered = await cut.InvokeAsync(() => panel.OnPanEndAsync(80.0, 0.0, 3));

		// Assert

		using var _ = new AssertionScope();
		answered.Should().Be(before + 1);
		RenderedEpoch(cut).Should().Be(answered);
		cut.Find("[data-ggnet-marks-frame]").GetAttribute("data-ggnet-commit").Should().Be("3");
	}

	[Fact]
	public async Task AfterAnUnseenEpochAdvanceThePanAnswersTheEpochItsFrameCarries()
	{
		// Arrange — a host Clear advances the epoch without rendering the marks, so the rendered
		// stamp is one behind when the pan commits.

		JSInterop.SetupModule(ModulePath).SetupVoid("initialize", _ => true).SetVoidResult();
		var cut = Render(PointPlot(), new InteractivityOptions { Pan = true, Crosshair = true });
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		await cut.Instance.ClearInspectionAsync();
		var stale = RenderedEpoch(cut);

		// Act

		var answered = await cut.InvokeAsync(() => panel.OnPanEndAsync(80.0, 0.0, 1));

		// Assert

		using var _ = new AssertionScope();
		answered.Should().Be(stale + 2, "the Clear and the pan each advanced the epoch once");
		RenderedEpoch(cut).Should().Be(answered);
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

namespace GGNet.Components.Tests;

// The cursor-glued tooltip (implementation-blocks Block 9, slices a+b): under
// the CursorTooltip opt-in the bubble renders as a manual top-layer popover
// whose element is handed to the JS module after each contentful render; the
// classic mark-anchored tooltip stays byte-untouched without the opt-in.
public class CursorTooltipTests : BunitContext
{
	private const string ModulePath = "./_content/GGNet/Components/Panel.razor.js";

	private sealed record P(double X, double Y);

	private static readonly P[] data = [new(1.0, 2.0), new(2.0, 3.5)];

	private static PlotContext<P, double, double> TooltipPlot()
		=> PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Point(tooltip: i => builder => builder.AddContent(0, FormattableString.Invariant($"{i.Y}")))
			.Style();

	private IRenderedComponent<Plot<P, double, double>> Render(PlotContext<P, double, double> context, InteractivityOptions? interactivity)
		=> Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, interactivity));

	[Fact]
	public void GluedTooltipRendersAsPopoverAndCallsTheModule()
	{
		// Arrange

		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("initialize", _ => true).SetVoidResult();
		module.SetupVoid("showTooltip", _ => true).SetVoidResult();

		var cut = Render(TooltipPlot(), new InteractivityOptions { CursorTooltip = true });

		// Act

		cut.Find("circle").MouseOver();

		// Assert

		using var _ = new AssertionScope();

		cut.Markup.Should().Contain("popover=\"manual\"").And.Contain("role=\"tooltip-glued\"");
		cut.WaitForAssertion(() => module.Invocations.Should().Contain(i => i.Identifier == "showTooltip"));
	}

	[Fact]
	public void GluedTooltipHidesOnMouseOut()
	{
		// Arrange

		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("initialize", _ => true).SetVoidResult();
		module.SetupVoid("showTooltip", _ => true).SetVoidResult();

		var cut = Render(TooltipPlot(), new InteractivityOptions { CursorTooltip = true });

		cut.Find("circle").MouseOver();

		// Act

		cut.Find("circle").MouseOut();

		// Assert

		cut.Markup.Should().NotContain("popover=\"manual\"");
	}

	[Fact]
	public void TooltipCarriesTheMarkColor()
	{
		// Arrange

		var cut = Render(TooltipPlot(), interactivity: null);

		// Act

		cut.Find("g[transform] > circle").MouseOver();

		// Assert

		// The mark's fill rides as --tooltip-color; the theme derives the
		// bubble background from it, falling back to --ggnet-tooltip-bg.
		cut.Markup.Should().Contain("--tooltip-color: #23d0fc").And.NotContain("--tootip-color");
	}

	[Fact]
	public void ClassicTooltipIsUntouchedWithoutTheOptIn()
	{
		// Arrange

		var cut = Render(TooltipPlot(), interactivity: null);

		// Act

		cut.Find("circle").MouseOver();

		// Assert

		// Mark-anchored quadrant placement, no popover attribute, no interop.
		using var _ = new AssertionScope();

		cut.Markup.Should().Contain("role=\"tooltip-").And.NotContain("tooltip-glued").And.NotContain("popover=");
		JSInterop.Invocations.Should().BeEmpty();
	}
}

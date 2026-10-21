namespace GGNet.Components.Tests;

public class TouchPolicyTests : BunitContext
{
	private sealed record P(double X, double Y);

	private static PlotContext<P, double, double> Context()
		=> PlotContext.Build(new[] { new P(1, 2), new P(2, 3) }, p => p.X, p => p.Y)
			.Geom_Point()
			.Style();

	[Theory]
	[InlineData(ZoomAxis.X, false, "pan-y pinch-zoom")]
	[InlineData(ZoomAxis.Y, false, "pan-x pinch-zoom")]
	[InlineData(ZoomAxis.Both, false, "pinch-zoom")]
	[InlineData(ZoomAxis.Both, true, "pan-y pinch-zoom")]
	public void EffectivePanAxesEmitTheirInlineTouchPolicy(ZoomAxis zoom, bool autoFitY, string touchAction)
	{
		// Arrange

		JSInterop.Mode = JSRuntimeMode.Loose;
		var context = Context();
		var options = new InteractivityOptions { Pan = true, Zoom = zoom, AutoFitY = autoFitY };

		// Act

		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, options));

		// Assert

		using var _ = new AssertionScope();

		var viewport = cut.FindAll("svg[data-ggnet-viewport]").Should().ContainSingle().Subject;
		var style = viewport.GetAttribute("style")!
			.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(declaration => declaration.Split(':', 2, StringSplitOptions.TrimEntries))
			.ToDictionary(declaration => declaration[0], declaration => declaration[1]);

		style["display"].Should().Be("block");
		style["touch-action"].Should().Be(touchAction);
		viewport.QuerySelector("rect.panel").Should().NotBeNull();
		cut.Find("svg").HasAttribute("style").Should().BeFalse();
	}

	[Theory]
	[InlineData(false, ZoomAxis.X, false)]
	[InlineData(false, ZoomAxis.Y, false)]
	[InlineData(false, ZoomAxis.Both, false)]
	[InlineData(true, ZoomAxis.Y, true)]
	public void WithoutEffectivePanAxesThePlotKeepsNativeTouchBehavior(bool pan, ZoomAxis zoom, bool autoFitY)
	{
		// Arrange

		JSInterop.Mode = JSRuntimeMode.Loose;
		var context = Context();
		var options = new InteractivityOptions { Pan = pan, Zoom = zoom, AutoFitY = autoFitY };

		// Act

		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, options));

		// Assert

		using var _ = new AssertionScope();

		cut.FindAll("[data-ggnet-viewport]").Should().BeEmpty();
		cut.Find(".ggnet > svg").QuerySelectorAll("svg").Should().BeEmpty();
		cut.Markup.Should().NotContain("touch-action");
	}

	[Fact]
	public void WithoutInteractivityThePlotKeepsNativeTouchBehavior()
	{
		// Arrange

		var context = Context();

		// Act

		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto));

		// Assert

		using var _ = new AssertionScope();

		cut.FindAll("[data-ggnet-viewport]").Should().BeEmpty();
		cut.Find(".ggnet > svg").QuerySelectorAll("svg").Should().BeEmpty();
		cut.Markup.Should().NotContain("touch-action");
		JSInterop.Invocations.Should().BeEmpty();
	}

	[Fact]
	public void PolarPlotsKeepNativeTouchBehaviorEvenWithBothPanAxesRequested()
	{
		// Arrange

		var context = Context().Coord_Polar();

		// Act

		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, new InteractivityOptions { Pan = true, Zoom = ZoomAxis.Both }));

		// Assert

		using var _ = new AssertionScope();

		cut.FindAll("[data-ggnet-viewport]").Should().BeEmpty();
		cut.Find(".ggnet > svg").QuerySelectorAll("svg").Should().BeEmpty();
		cut.Markup.Should().NotContain("touch-action");
		JSInterop.Invocations.Should().BeEmpty();
	}
}

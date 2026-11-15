namespace GGNet.Components.Tests;

public class TouchPolicyTests : BunitContext
{
	[Theory]
	[InlineData(TouchNavigation.None, true, "pan-y pinch-zoom")]
	[InlineData(TouchNavigation.TwoFingerX, false, "pan-y")]
	[InlineData(TouchNavigation.PanAndPinchX, false, "pan-y")]
	public void ExplicitScrollPolicyIsIndependentOfCrosshairAndMousePan(TouchNavigation navigation, bool inspect, string expected)
	{
		// Arrange

		JSInterop.Mode = JSRuntimeMode.Loose;
		var options = new InteractivityOptions { Touch = new() { Navigation = navigation, Inspect = inspect, Ownership = TouchOwnership.ScrollY } };

		// Act

		var cut = Render<Plot<P, double, double>>(p => p.Add(x => x.Context, Context())
			.Add(x => x.RenderMode, RenderMode.InteractiveAuto).Add(x => x.Interactivity, options));

		// Assert

		cut.Find("svg[data-ggnet-viewport]").GetAttribute("style").Should().Contain($"touch-action: {expected};");
	}

	[Fact]
	public void EmptyTouchOptionsDisableTouchEvenWithMousePanAndCrosshair()
	{
		// Arrange

		JSInterop.Mode = JSRuntimeMode.Loose;

		// Act

		var cut = Render<Plot<P, double, double>>(p => p.Add(x => x.Context, Context())
			.Add(x => x.RenderMode, RenderMode.InteractiveAuto)
			.Add(x => x.Interactivity, new() { Pan = true, Crosshair = true, Touch = new() }));

		// Assert

		cut.FindAll("svg[data-ggnet-viewport]").Should().BeEmpty();
	}

	[Fact]
	public void ExplicitInspectionAcceptsHostSelectionWithoutCrosshair()
	{
		// Arrange

		JSInterop.Mode = JSRuntimeMode.Loose;
		var selected = new Inspection<double>(2);

		// Act

		var cut = Render<Plot<P, double, double>>(p => p.Add(x => x.Context, Context())
			.Add(x => x.RenderMode, RenderMode.InteractiveAuto)
			.Add(x => x.Interactivity, new() { Touch = new() { Inspect = true } })
			.Add(x => x.Inspection, selected));

		// Assert

		cut.Instance.Readings!.Inspection.Should().Be(selected);
		cut.FindAll("[data-ggnet-cursor]").Should().BeEmpty();
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ExplicitSharedTouchRejectsUnsupportedCoordinates(bool polar)
	{
		// Arrange

		var context = polar ? Context().Coord_Polar() : Context().Flip();

		// Act

		var mount = () => Render<Plot<P, double, double>>(p => p.Add(x => x.Context, context)
			.Add(x => x.RenderMode, RenderMode.InteractiveAuto)
			.Add(x => x.Interactivity, new() { Touch = new() { Navigation = TouchNavigation.TwoFingerX } }));

		// Assert

		mount.Should().Throw<Exceptions.GGNetUserException>().WithMessage("*shared x scale*");
	}

	private sealed record P(double X, double Y);

	private static PlotContext<P, double, double> Context()
		=> PlotContext.Build(new[] { new P(1, 2), new P(2, 3) }, p => p.X, p => p.Y)
			.Geom_Point()
			.Style();

	[Theory]
	[InlineData(ZoomAxis.X, false, "none")]
	[InlineData(ZoomAxis.Both, false, "none")]
	[InlineData(ZoomAxis.Both, true, "none")]
	public void InferredXNavigationOwnsTouchBeforeContact(ZoomAxis zoom, bool autoFitY, string touchAction)
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
	[InlineData(true, ZoomAxis.Y, false)]
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

	[Theory]
	[InlineData(false, ZoomAxis.X, "none")]
	[InlineData(true, ZoomAxis.X, "none")]
	[InlineData(true, ZoomAxis.Both, "none")]
	[InlineData(true, ZoomAxis.Y, "none")]
	public void InferredInspectionOwnsTouchBeforeContact(bool pan, ZoomAxis zoom, string touchAction)
	{
		// Arrange

		JSInterop.Mode = JSRuntimeMode.Loose;
		var context = Context();

		// Act

		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, new InteractivityOptions { Crosshair = true, Pan = pan, Zoom = zoom }));

		// Assert

		cut.FindAll("svg[data-ggnet-viewport]").Should().ContainSingle()
			.Which.GetAttribute("style").Should().Contain($"touch-action: {touchAction};");
	}
}

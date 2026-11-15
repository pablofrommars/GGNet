using System.Globalization;

namespace GGNet.Components.Tests;

// The browser module owns the touch gestures and their arithmetic: it turns the
// fingers' midpoint and spread into an absolute x window, which the browser suite
// covers. bUnit pins what the server does with that window: one commit, under the
// session, epoch and revision the command carries.
public class TwoFingerNavigationTests : BunitContext
{
	public sealed record P(double X, double Y);

	private static readonly P[] data = [new(0, 1), new(25, 9), new(50, 2), new(75, 4), new(100, 3)];

	private readonly List<ViewChange<double, double>> changes = [];

	private readonly List<Inspection<double>?> positions = [];

	public TwoFingerNavigationTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	private IRenderedComponent<Plot<P, double, double>> Mount(InteractivityOptions interactivity, Inspection<double>? inspection = null)
	{
		var context = PlotContext.Build(data, i => i.X, i => i.Y).Geom_Point().Style();

		context.SetXView(0, 100);

		return Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, interactivity)
			.Add(p => p.Inspection, inspection)
			.Add(p => p.InspectionChanged, positions.Add)
			.Add(p => p.ViewChanged, changes.Add));
	}

	private static double At(IRenderedComponent<Panel<P, double, double>> panel, double fraction)
	{
		var area = panel.Find("rect.panel");

		return double.Parse(area.GetAttribute("x")!, CultureInfo.InvariantCulture)
			+ fraction * double.Parse(area.GetAttribute("width")!, CultureInfo.InvariantCulture);
	}

	// The identity a browser would send. A host-set selection advances the epoch without
	// re-rendering the marks, so the inspection stamp is read where the plot has one.
	private static (string session, long epoch) LiveIdentity(IRenderedComponent<Plot<P, double, double>> cut)
	{
		var inspection = cut.FindAll("[data-ggnet-inspection]");
		var stamp = (inspection.Count > 0
			? inspection[0].GetAttribute("data-ggnet-inspection")!
			: cut.Find("[data-ggnet-view]").GetAttribute("data-ggnet-view")!).Split(' ');

		return (stamp[0], long.Parse(stamp[1], CultureInfo.InvariantCulture));
	}

	[Theory]
	[InlineData(20.0, 120.0)]
	[InlineData(25.0, 75.0)]
	[InlineData(-50.0, 150.0)]
	[InlineData(-12.5, 37.5)]
	public async Task AnAbsoluteWindowIsCommittedOnceAsAGesture(double min, double max)
	{
		// Arrange

		var cut = Mount(new() { Crosshair = true, Pan = true });
		var panel = cut.FindComponent<Panel<P, double, double>>();
		var (session, epoch) = LiveIdentity(cut);

		// Act

		await cut.InvokeAsync(() => panel.Instance.OnTouchNavigateAsync(session, epoch, 1, min, max));

		// Assert

		var change = changes.Should().ContainSingle().Subject;

		using var _ = new AssertionScope();

		change.Cause.Should().Be(ViewChangeCause.Gesture);
		change.X!.Value.min.Should().BeApproximately(min, 1e-6);
		change.X!.Value.max.Should().BeApproximately(max, 1e-6);
	}

	[Fact]
	public async Task ExplicitTouchNavigationWorksWithoutCrosshairOrMousePan()
	{
		// Arrange

		var cut = Mount(new() { Touch = new() { Navigation = TouchNavigation.TwoFingerX } });
		var panel = cut.FindComponent<Panel<P, double, double>>();
		var (session, epoch) = LiveIdentity(cut);

		// Act

		await cut.InvokeAsync(() => panel.Instance.OnTouchNavigateAsync(session, epoch, 1, 25, 75));

		// Assert

		changes.Should().ContainSingle().Which.X.Should().Be((25.0, 75.0));
		positions.Should().BeEmpty();
	}

	[Fact]
	public async Task NavigationStampsThePanelAndItsRenderedMarksTogether()
	{
		// Arrange

		var cut = Mount(new() { Pan = true, Crosshair = true });
		var before = cut.Find("[data-ggnet-frame]").GetAttribute("data-ggnet-frame");

		// Act

		await cut.InvokeAsync(() => cut.Instance.ZoomToXAsync(25, 75));

		// Assert

		var frame = cut.Find("[data-ggnet-frame]").GetAttribute("data-ggnet-frame");
		using var _ = new AssertionScope();
		frame.Should().NotBeNullOrEmpty().And.NotBe(before);
		cut.Find("[data-ggnet-marks-frame]").GetAttribute("data-ggnet-marks-frame").Should().Be(frame);
	}

	[Fact]
	public async Task OlderTouchNavigationCannotOverwriteANewerAbsoluteWindow()
	{
		// Arrange

		var cut = Mount(new() { Pan = true });
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var identity = cut.Find("[data-ggnet-view]").GetAttribute("data-ggnet-view")!.Split(' ');
		var epoch = long.Parse(identity[1], CultureInfo.InvariantCulture);

		// Act

		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync(identity[0], epoch, 2, 40, 90));
		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync(identity[0], epoch, 1, 20, 120));
		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync(identity[0], epoch, 2, 10, 30));

		// Assert

		changes.Should().ContainSingle().Which.X.Should().Be((40.0, 90.0));
		cut.Instance.Context.XView.Should().Be((40.0, 90.0));
		cut.Find("[data-ggnet-view]").GetAttribute("data-ggnet-view")!.Split(' ')[2].Should().Be("2");
	}

	[Fact]
	public async Task HostViewportReplacementRejectsAnOlderTouchEpoch()
	{
		// Arrange

		var cut = Mount(new() { Pan = true });
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var identity = cut.Find("[data-ggnet-view]").GetAttribute("data-ggnet-view")!.Split(' ');
		var epoch = long.Parse(identity[1], CultureInfo.InvariantCulture);

		// Act

		await cut.InvokeAsync(() => cut.Instance.ZoomToXAsync(5, 15));
		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync(identity[0], epoch, 100, 40, 90));

		// Assert

		changes.Should().ContainSingle().Which.Cause.Should().Be(ViewChangeCause.HostCommand);
		cut.Instance.Context.XView.Should().Be((5.0, 15.0));
	}

	[Fact]
	public async Task ADataRefreshKeepsAPendingTouchNavigationValid()
	{
		// Arrange

		var cut = Mount(new() { Pan = true });
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var identity = cut.Find("[data-ggnet-view]").GetAttribute("data-ggnet-view")!.Split(' ');
		var epoch = long.Parse(identity[1], CultureInfo.InvariantCulture);

		// Act

		await cut.InvokeAsync(() => cut.Instance.RefreshAsync(RenderTarget.Render, CancellationToken.None));
		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync(identity[0], epoch, 1, 40, 90));

		// Assert

		changes.Should().ContainSingle().Which.Cause.Should().Be(ViewChangeCause.Gesture);
		cut.Instance.Context.XView.Should().Be((40.0, 90.0));
	}

	[Fact]
	public async Task ResettingAnUnchangedWindowStillRejectsAnOlderTouchEpoch()
	{
		// Arrange — no window is set, so the reset leaves the x window exactly as it was.

		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, PlotContext.Build(data, i => i.X, i => i.Y).Geom_Point().Style())
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, new() { Pan = true })
			.Add(p => p.ViewChanged, changes.Add));
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var identity = cut.Find("[data-ggnet-view]").GetAttribute("data-ggnet-view")!.Split(' ');
		var epoch = long.Parse(identity[1], CultureInfo.InvariantCulture);

		// Act

		await cut.InvokeAsync(() => cut.Instance.ResetViewAsync());
		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync(identity[0], epoch, 1, 40, 90));

		// Assert

		using var _ = new AssertionScope();
		changes.Should().BeEmpty();
		cut.Instance.Context.XView.Should().BeNull();
	}

	private IRenderedComponent<Plot<P, double, double>> MountQueued()
	{
		var context = PlotContext.Build(data, i => i.X, i => i.Y).Geom_Point().Style();

		context.SetXView(0, 100);

		return Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.Interactive)
			.Add(p => p.Interactivity, new() { Pan = true }));
	}

	private static Task Command(Plot<P, double, double> plot, string command, CancellationToken token) => command switch
	{
		"x" => plot.ZoomToXAsync(5, 15, token),
		"y" => plot.ZoomToYAsync(2, 3, token),
		_ => plot.ResetViewAsync(token)
	};

	[Theory]
	[InlineData("x")]
	[InlineData("y")]
	[InlineData("reset")]
	public async Task ACancelledHostCommandLeavesTheRenderedTouchEpochValid(string command)
	{
		// Arrange — the queued render mode hands no frame off for a cancelled token, so nothing could carry an advance.

		var cut = MountQueued();
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var (session, epoch) = RenderedIdentity(cut);
		using var cancelled = new CancellationTokenSource();
		await cancelled.CancelAsync();

		// Act

		var refused = async () => await cut.InvokeAsync(() => Command(cut.Instance, command, cancelled.Token));
		await refused.Should().ThrowAsync<OperationCanceledException>();
		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync(session, epoch, 1, 40, 90));

		// Assert

		cut.Instance.Context.XView.Should().Be((40.0, 90.0));
	}

	[Fact]
	public async Task AWindowLeftByACancelledHostCommandSupersedesWithTheNextFrame()
	{
		// Arrange

		var cut = MountQueued();
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var (session, epoch) = RenderedIdentity(cut);
		using var cancelled = new CancellationTokenSource();
		await cancelled.CancelAsync();
		var refused = async () => await cut.InvokeAsync(() => cut.Instance.ZoomToXAsync(5, 15, cancelled.Token));
		await refused.Should().ThrowAsync<OperationCanceledException>();

		// Act

		await cut.InvokeAsync(() => cut.Instance.RefreshAsync(RenderTarget.Render, CancellationToken.None));
		cut.WaitForState(() => RenderedIdentity(cut).epoch > epoch, TimeSpan.FromSeconds(5));
		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync(session, epoch, 1, 40, 90));

		// Assert

		using var _ = new AssertionScope();
		cut.Find("[data-ggnet-view]").GetAttribute("data-ggnet-view").Should().EndWith(" 5 15");
		cut.Instance.Context.XView.Should().Be((5.0, 15.0));
	}

	[Fact]
	public async Task AWindowSetBeforeALoadingRefreshStillSupersedesAnOlderTouchEpoch()
	{
		// Arrange

		var cut = Mount(new() { Pan = true });
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var (session, epoch) = RenderedIdentity(cut);

		// Act

		cut.Instance.Context.SetXView(10, 20);
		await cut.InvokeAsync(() => cut.Instance.RefreshAsync(RenderTarget.Loading, CancellationToken.None));
		await cut.InvokeAsync(() => cut.Instance.RefreshAsync(RenderTarget.Render, CancellationToken.None));
		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync(session, epoch, 1, 40, 90));

		// Assert

		using var _ = new AssertionScope();
		RenderedIdentity(cut).epoch.Should().BeGreaterThan(epoch);
		cut.Instance.Context.XView.Should().Be((10.0, 20.0));
	}

	[Theory]
	[InlineData(10, 20, false)]
	[InlineData(0, 100, true)]
	public async Task AReplacedContextSupersedesTouchNavigationOnlyWhenItsWindowDiffers(double min, double max, bool kept)
	{
		// Arrange — a host may rebuild its context for new data and carry the window over. This pins the
		// server's acceptance rule only: in a browser the swap re-creates the panels, so no client can
		// still send the old command (see AReplacedContextLeavesNoTouchFeedbackBehind).

		var cut = Mount(new() { Pan = true });
		var (session, epoch) = RenderedIdentity(cut);
		var replacement = PlotContext.Build(data, i => i.X, i => i.Y).Geom_Point().Style();
		replacement.SetXView(min, max);

		// Act

		cut.Render(parameters => parameters.Add(p => p.Context, replacement));
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync(session, epoch, 1, 40, 90));

		// Assert

		cut.Instance.Context.XView.Should().Be(kept ? (40.0, 90.0) : (min, max));
	}

	private static readonly InteractivityOptions navigationOnly = new() { Pan = true, Touch = new() { Navigation = TouchNavigation.PanAndPinchX } };

	private static (string session, long epoch) RenderedIdentity(IRenderedComponent<Plot<P, double, double>> cut)
	{
		var stamp = cut.Find("[data-ggnet-view]").GetAttribute("data-ggnet-view")!.Split(' ');

		return (stamp[0], long.Parse(stamp[1], CultureInfo.InvariantCulture));
	}

	[Fact]
	public async Task ClearingWithoutTouchInspectionKeepsTouchNavigationValid()
	{
		// Arrange

		var cut = Mount(navigationOnly);
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;

		// Act

		await cut.Instance.ClearInspectionAsync();
		var (session, epoch) = RenderedIdentity(cut);
		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync(session, epoch, 1, 40, 90));

		// Assert

		cut.Instance.Context.XView.Should().Be((40.0, 90.0));
	}

	[Fact]
	public async Task ASuppliedSelectionWithoutTouchInspectionKeepsTouchNavigationValid()
	{
		// Arrange — a host shares one selection with plots whose inspection is off.

		var cut = Mount(navigationOnly, new(50));
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;

		// Act

		var (session, epoch) = RenderedIdentity(cut);
		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync(session, epoch, 1, 40, 90));
		cut.Render(parameters => parameters.Add(p => p.Inspection, new(60)));
		(session, epoch) = RenderedIdentity(cut);
		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync(session, epoch, 2, 10, 30));

		// Assert

		cut.Instance.Context.XView.Should().Be((10.0, 30.0));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AMouseCrosshairSelectionChangeKeepsTouchNavigationValid(bool clear)
	{
		// Arrange — the crosshair renders, but without touch inspection no stamp carries an inspection epoch.

		var cut = Mount(new() { Pan = true, Crosshair = true, Touch = new() { Navigation = TouchNavigation.PanAndPinchX } }, new(50));
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;

		// Act

		if (clear)
		{
			await cut.Instance.ClearInspectionAsync();
		}
		else
		{
			cut.Render(parameters => parameters.Add(p => p.Inspection, new(60)));
		}
		var (session, epoch) = RenderedIdentity(cut);
		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync(session, epoch, 1, 40, 90));

		// Assert

		cut.Instance.Context.XView.Should().Be((40.0, 90.0));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task AWheelOrDragCommitSupersedesAnAcceptedTouchNavigation(bool wheel)
	{
		// Arrange — both commits can share one rendered frame, which then carries the touch revision with other bounds.

		var cut = Mount(new() { Pan = true });
		var panel = cut.FindComponent<Panel<P, double, double>>();
		var (session, epoch) = RenderedIdentity(cut);
		var area = panel.Find("rect.panel");
		var y = double.Parse(area.GetAttribute("y")!, CultureInfo.InvariantCulture) + double.Parse(area.GetAttribute("height")!, CultureInfo.InvariantCulture) / 2;
		await cut.InvokeAsync(() => panel.Instance.OnTouchNavigateAsync(session, epoch, 1, 40, 90));

		// Act

		await cut.InvokeAsync(() => wheel ? panel.Instance.OnWheelAsync(At(panel, 0.5), y, -100) : panel.Instance.OnPanEndAsync(40, 0));
		var window = cut.Instance.Context.XView;
		await cut.InvokeAsync(() => panel.Instance.OnTouchNavigateAsync(session, epoch, 2, 10, 30));

		// Assert

		using var _ = new AssertionScope();
		RenderedIdentity(cut).epoch.Should().BeGreaterThan(epoch);
		window.Should().NotBe((40.0, 90.0));
		cut.Instance.Context.XView.Should().Be(window);
	}

	[Fact]
	public async Task TouchNavigationRejectsADifferentMountedSession()
	{
		// Arrange

		var cut = Mount(new() { Pan = true });
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var identity = cut.Find("[data-ggnet-view]").GetAttribute("data-ggnet-view")!.Split(' ');

		// Act

		await cut.InvokeAsync(() => panel.OnTouchNavigateAsync("retired", long.Parse(identity[1], CultureInfo.InvariantCulture), 1, 40, 90));

		// Assert

		changes.Should().BeEmpty();
		cut.Instance.Context.XView.Should().Be((0.0, 100.0));
	}

	[Fact]
	public async Task ThePinKeepsItsValueAndFollowsTheWindow()
	{
		// Arrange

		var cut = Mount(new() { Crosshair = true, Pan = true }, new(50));
		var panel = cut.FindComponent<Panel<P, double, double>>();
		var before = cut.Find("line.crosshair").GetAttribute("x1");
		var (session, epoch) = LiveIdentity(cut);

		// Act — the window that puts the value at half the width under three tenths of it.

		await cut.InvokeAsync(() => panel.Instance.OnTouchNavigateAsync(session, epoch, 1, 20, 120));

		// Assert

		using var _ = new AssertionScope();

		positions.Should().BeEmpty();
		cut.Instance.Readings!.Inspection.Should().Be(new Inspection<double>(50));
		cut.Find("line.crosshair").GetAttribute("x1").Should().Be(Fixed(At(panel, 0.3))).And.NotBe(before);
	}

	[Theory]
	[InlineData(true, false, ZoomAxis.X)]
	[InlineData(true, true, ZoomAxis.Y)]
	public async Task WithoutInferredXNavigationNothingIsCommitted(bool crosshair, bool pan, ZoomAxis zoom)
	{
		// Arrange

		var cut = Mount(new() { Crosshair = crosshair, Pan = pan, Zoom = zoom });
		var panel = cut.FindComponent<Panel<P, double, double>>();
		var (session, epoch) = LiveIdentity(cut);

		// Act

		await cut.InvokeAsync(() => panel.Instance.OnTouchNavigateAsync(session, epoch, 1, 40, 90));

		// Assert

		changes.Should().BeEmpty();
	}

	[Theory]
	[InlineData(double.NaN, 90.0)]
	[InlineData(40.0, double.PositiveInfinity)]
	[InlineData(90.0, 40.0)]
	[InlineData(50.0, 50.0)]
	public async Task AMeaninglessWindowIsIgnored(double min, double max)
	{
		// Arrange

		var cut = Mount(new() { Crosshair = true, Pan = true });
		var panel = cut.FindComponent<Panel<P, double, double>>();
		var (session, epoch) = LiveIdentity(cut);

		// Act

		await cut.InvokeAsync(() => panel.Instance.OnTouchNavigateAsync(session, epoch, 1, min, max));
		await cut.InvokeAsync(() => panel.Instance.OnTouchNavigateAsync(session, epoch, 1, 40, 90));

		// Assert — the refused command consumed no revision, so the same one still commits.

		changes.Should().ContainSingle().Which.X.Should().Be((40.0, 90.0));
	}

	[Fact]
	public async Task AutoFitYRefitsInTheSameChange()
	{
		// Arrange

		var cut = Mount(new() { Crosshair = true, Pan = true, AutoFitY = true });
		var panel = cut.FindComponent<Panel<P, double, double>>();
		var (session, epoch) = LiveIdentity(cut);

		// Act — zoom onto the stretch around x = 25, where y peaks at 9.

		await cut.InvokeAsync(() => panel.Instance.OnTouchNavigateAsync(session, epoch, 1, 15, 35));

		// Assert

		var change = changes.Should().ContainSingle().Subject;

		using var _ = new AssertionScope();

		change.X!.Value.Should().Be((15.0, 35.0));
		change.Y.Should().NotBeNull();
		change.Y!.Value.max.Should().BeGreaterThanOrEqualTo(9.0).And.BeLessThan(11.0);
	}

	private static string Fixed(double value)
		=> value.ToString("0.###############", CultureInfo.InvariantCulture);
}

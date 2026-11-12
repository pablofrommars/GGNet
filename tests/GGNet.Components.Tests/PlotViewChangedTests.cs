namespace GGNet.Components.Tests;

public class PlotViewChangedTests : BunitContext
{
	private sealed record P(double X, double Y);

	private static readonly P[] data =
	[
		new(1.0, 2.0),
		new(2.0, 3.5),
		new(3.0, 2.8),
		new(4.0, 4.2)
	];

	private readonly List<ViewChange<double, double>> changes = [];

	public PlotViewChangedTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	private static PlotContext<P, double, double> PointPlot()
		=> PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Point()
			.Style();

	private IRenderedComponent<Plot<P, double, double>> Render(PlotContext<P, double, double> context, InteractivityOptions? interactivity = null)
		=> Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, interactivity)
			.Add(p => p.ViewChanged, changes.Add));

	[Fact]
	public void AWindowSetBeforeTheFirstRenderIsNotAChange()
	{
		// Arrange

		var context = PointPlot();

		context.SetXView(2.0, 3.0);

		// Act

		Render(context);

		// Assert

		changes.Should().BeEmpty();
	}

	[Fact]
	public async Task ZoomToXReportsOneHostCommandWithTheWindow()
	{
		// Arrange

		var cut = Render(PointPlot());

		// Act

		await cut.InvokeAsync(() => cut.Instance.ZoomToXAsync(2.0, 3.0));

		// Assert

		changes.Should().ContainSingle().Which.Should().Be(
			new ViewChange<double, double>(ViewChangeCause.HostCommand, (2.0, 3.0), null));
	}

	[Fact]
	public async Task ZoomToYReportsOneHostCommandWithTheWindow()
	{
		// Arrange

		var cut = Render(PointPlot());

		// Act

		await cut.InvokeAsync(() => cut.Instance.ZoomToYAsync(2.5, 3.0));

		// Assert

		changes.Should().ContainSingle().Which.Should().Be(
			new ViewChange<double, double>(ViewChangeCause.HostCommand, null, (2.5, 3.0)));
	}

	[Fact]
	public async Task ResetViewReportsOneHostCommandWithNoWindows()
	{
		// Arrange

		var cut = Render(PointPlot());

		await cut.InvokeAsync(() => cut.Instance.ZoomToXAsync(2.0, 3.0));
		changes.Clear();

		// Act

		await cut.InvokeAsync(() => cut.Instance.ResetViewAsync());

		// Assert

		changes.Should().ContainSingle().Which.Should().Be(
			new ViewChange<double, double>(ViewChangeCause.HostCommand, null, null));
	}

	[Fact]
	public async Task ACommandThatLeavesTheWindowUnchangedReportsNothing()
	{
		// Arrange

		var cut = Render(PointPlot());

		await cut.InvokeAsync(() => cut.Instance.ZoomToXAsync(2.0, 3.0));
		changes.Clear();

		// Act

		await cut.InvokeAsync(() => cut.Instance.ZoomToXAsync(2.0, 3.0));
		await cut.InvokeAsync(() => cut.Instance.RefreshAsync(RenderTarget.Render, CancellationToken.None));

		// Assert

		changes.Should().BeEmpty();
	}

	[Fact]
	public async Task AWindowSetOnTheContextIsReportedByTheNextRefresh()
	{
		// Arrange

		var context = PointPlot();
		var cut = Render(context);

		// Act

		context.SetXView(2.0, 3.0);

		var before = changes.Count;

		await cut.InvokeAsync(() => cut.Instance.RefreshAsync(RenderTarget.Render, CancellationToken.None));
		await cut.InvokeAsync(() => cut.Instance.RefreshAsync(RenderTarget.Render, CancellationToken.None));

		// Assert

		using var _ = new AssertionScope();

		before.Should().Be(0);
		changes.Should().ContainSingle().Which.Should().Be(
			new ViewChange<double, double>(ViewChangeCause.HostCommand, (2.0, 3.0), null));
	}

	[Fact]
	public async Task WheelZoomReportsOneGesture()
	{
		// Arrange

		var context = PointPlot();
		var cut = Render(context, new InteractivityOptions());
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;

		// Act

		await cut.InvokeAsync(() => panel.OnWheelAsync(350, 200, -1));

		// Assert

		using var _ = new AssertionScope();

		changes.Should().ContainSingle();
		changes[0].Cause.Should().Be(ViewChangeCause.Gesture);
		changes[0].X.Should().NotBeNull().And.Be(context.XView);
		changes[0].Y.Should().BeNull();
	}

	[Fact]
	public async Task PanReportsOneGestureOnCommitAndNothingBefore()
	{
		// Arrange

		var context = PointPlot();
		var cut = Render(context, new InteractivityOptions { Pan = true });
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;

		// Act

		var before = changes.Count;

		await cut.InvokeAsync(() => panel.OnPanEndAsync(80, 0));

		// Assert

		using var _ = new AssertionScope();

		before.Should().Be(0);
		changes.Should().ContainSingle();
		changes[0].Cause.Should().Be(ViewChangeCause.Gesture);
		changes[0].X.Should().NotBeNull().And.Be(context.XView);
	}

	[Fact]
	public async Task AutoFitYTravelsInTheSameNotificationAsItsXChange()
	{
		// Arrange

		var context = PointPlot();
		var cut = Render(context, new InteractivityOptions { Pan = true, AutoFitY = true });
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;

		// Act

		await cut.InvokeAsync(() => panel.OnPanEndAsync(80, 0));

		// Assert

		using var _ = new AssertionScope();

		changes.Should().ContainSingle();
		changes[0].X.Should().NotBeNull();
		changes[0].Y.Should().NotBeNull().And.Be(context.YView);
	}

	[Fact]
	public async Task TheResetGestureReportsOneReset()
	{
		// Arrange

		var cut = Render(PointPlot(), new InteractivityOptions());

		await cut.InvokeAsync(() => cut.Instance.ZoomToXAsync(2.0, 3.0));
		changes.Clear();

		// Act

		await cut.Find("g[style*='user-select']").DoubleClickAsync(new());

		// Assert

		changes.Should().ContainSingle().Which.Should().Be(
			new ViewChange<double, double>(ViewChangeCause.Reset, null, null));
	}

	private sealed record Timed(NodaTime.Instant Ts, double Value);

	[Fact]
	public async Task ShowLastReportsOneHostCommandWithTheTrailingWindow()
	{
		// Arrange

		var start = NodaTime.Instant.FromUtc(2026, 7, 1, 0, 0);

		var timed = new Timed[8];

		for (var i = 0; i < timed.Length; i++)
		{
			timed[i] = new(start + NodaTime.Duration.FromHours(12 * i), 1.0 + i);
		}

		var context = PlotContext.Build(timed, t => t.Ts, t => t.Value)
			.Scale_X_Instant()
			.Geom_Point()
			.Style();

		var seen = new List<ViewChange<NodaTime.Instant, double>>();

		var cut = Render<Plot<Timed, NodaTime.Instant, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.ViewChanged, seen.Add));

		// Act

		await cut.InvokeAsync(() => cut.Instance.ShowLastAsync(NodaTime.Duration.FromHours(48)));

		// Assert

		var end = start + NodaTime.Duration.FromHours(12 * (timed.Length - 1));

		seen.Should().ContainSingle().Which.Should().Be(
			new ViewChange<NodaTime.Instant, double>(ViewChangeCause.HostCommand, (end - NodaTime.Duration.FromHours(48), end), null));
	}
}

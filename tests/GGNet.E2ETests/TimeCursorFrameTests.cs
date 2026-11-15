using static GGNet.E2ETests.Fixtures.TouchInput;

namespace GGNet.E2ETests;

public partial class TimeCursorTests
{
	[Theory]
	[InlineData(false, 300)]
	[InlineData(false, 2000)]
	[InlineData(true, 300)]
	[InlineData(true, 2000)]
	public async Task InspectionHidesStaleValuesUntilItsFrameArrives(bool holdInvocations, int delay)
	{
		// Arrange — holding invocations delays the server call itself; holding frames delays only its rendered reply.

		await using var frames = new CircuitGate(holdInvocations);
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.2, y);
		await session.WaitForTextAsync("pinned", "pinned");
		var oldTime = await session.TextAsync("cursor-time");
		frames.Hold();

		// Act

		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.8, y);
		await frames.WaitForHeldMessageAsync();
		await session.Page.WaitForTimeoutAsync(delay);
		var during = await session.CursorsAsync("cursor");
		var staleTime = await session.TextAsync("cursor-time");
		var labelHidden = await session.Page.Locator("[data-qa=cursor] .crosshair-label").First.EvaluateAsync<bool>("element => getComputedStyle(element).visibility === 'hidden'");
		var readoutHidden = await session.Page.Locator("[data-qa=cursor-time]").EvaluateAsync<bool>("element => getComputedStyle(element).visibility === 'hidden'");
		frames.Release();
		await session.Page.WaitForFunctionAsync("() => !document.querySelector('[data-qa=cursor] .ggnet').hasAttribute('data-ggnet-interaction')");
		var after = await session.CursorsAsync("cursor");

		// Assert

		using var _ = new AssertionScope();
		during.Should().HaveCount(4).And.OnlyContain(cursor => cursor.Visible && !cursor.Pinned && Math.Abs(cursor.Fraction - 0.8) < 0.003);
		staleTime.Should().Be(oldTime);
		labelHidden.Should().BeTrue();
		readoutHidden.Should().BeTrue();
		after.Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.8) < 0.003);
		(await session.TextAsync("cursor-time")).Should().NotBe(oldTime);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task HostClearWinsOverAHeldTouchReply()
	{
		// Arrange

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.2, y);
		await session.WaitForTextAsync("pinned", "pinned");
		frames.Hold();

		// Act

		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.8, y);
		await frames.WaitForHeldMessageAsync();
		await session.Page.Locator("[data-qa=clear]").ClickAsync();
		await session.Page.WaitForTimeoutAsync(300);
		frames.Release();
		await session.Page.Locator("[data-qa=empty]").WaitForAsync();
		await session.Page.WaitForTimeoutAsync(500);

		// Assert

		using var _ = new AssertionScope();
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => !cursor.Visible);
		await ExpectNoPendingAsync(session.Page);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task WatchdogQuarantinesLateInspectionRepliesUntilRemount()
	{
		// Arrange

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		await PinAsync(session, panel.X + panel.Width * 0.5, y);
		var acknowledged = await session.TextAsync("cursor-time");
		frames.Hold();

		// Act

		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.2, y);
		await frames.WaitForHeldMessageAsync();
		await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-qa=cursor] .ggnet').dataset.ggnetInteraction === 'unavailable'", null, new() { Timeout = 15_000 });
		var provisionalCount = await session.Page.Locator("[data-ggnet-provisional]").CountAsync();
		var shown = await AcknowledgedValuesAsync(session.Page);
		var shownTime = await session.TextAsync("cursor-time");
		frames.Release();
		await session.WaitForTextAsync("position-changes", "2");
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.8, y);
		await session.Page.WaitForTimeoutAsync(400);

		// Assert

		using var _ = new AssertionScope();
		provisionalCount.Should().Be(0);
		shown.Should().Equal("visible", "visible", "visible");
		shownTime.Should().Be(acknowledged);
		(await AcknowledgedValuesAsync(session.Page)).Should().Equal("visible", "visible", "visible");
		(await session.TextAsync("position-changes")).Should().Be("2");
		(await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction")).Should().Be("unavailable");
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData(100)]
	[InlineData(300)]
	public async Task ASustainedScrubUnderLatencyIsNotQuarantined(int latency)
	{
		// Arrange — every server frame arrives late, yet each scrub position is acknowledged well inside the watchdog's bound.

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		var x = panel.X + panel.Width * 0.5;
		frames.Delay(TimeSpan.FromMilliseconds(latency));
		var states = new HashSet<string?>();

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		await session.Page.WaitForTimeoutAsync(450);
		var clock = System.Diagnostics.Stopwatch.StartNew();
		for (var moves = 1; clock.Elapsed < TimeSpan.FromSeconds(12); moves++)
		{
			await TouchAsync(session.Cdp, "touchMove", Point(x + 150 * Math.Sin(clock.Elapsed.TotalSeconds * 2), y));
			if (moves % 20 == 0)
			{
				states.Add(await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction"));
			}
			await session.Page.WaitForTimeoutAsync(8);
		}
		var acknowledged = int.Parse(await session.TextAsync("position-changes"), System.Globalization.CultureInfo.InvariantCulture);
		await TouchAsync(session.Cdp, "touchEnd");
		await session.WaitForTextAsync("pinned", "pinned");
		for (var i = 0; i < 50 && await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction") is not null; i++)
		{
			await session.Page.WaitForTimeoutAsync(100);
		}

		// Assert

		using var _ = new AssertionScope();
		states.Should().Equal("inspecting");
		acknowledged.Should().BeGreaterThan(10);
		(await session.TextAsync("pinned")).Should().Be("pinned");
		await ExpectNoPendingAsync(session.Page);
		session.Errors.Should().BeEmpty();
	}

	// The pen goes through the DevTools protocol, which can name the pointer type; the mouse is Playwright's.
	private static async Task DragAsync(Session session, string device, double x, double y, double dx, Func<Task>? beforeRelease = null)
	{
		if (device == "mouse")
		{
			await session.Page.Mouse.MoveAsync((float)x, (float)y);
			await session.Page.Mouse.DownAsync();
			await session.Page.Mouse.MoveAsync((float)(x + dx), (float)y, new() { Steps = 6 });
			await (beforeRelease?.Invoke() ?? Task.CompletedTask);
			await session.Page.Mouse.UpAsync();
			return;
		}

		await session.Cdp.SendAsync("Input.dispatchMouseEvent", new() { ["type"] = "mousePressed", ["x"] = x, ["y"] = y, ["button"] = "left", ["buttons"] = 1, ["clickCount"] = 1, ["pointerType"] = device });
		await session.Cdp.SendAsync("Input.dispatchMouseEvent", new() { ["type"] = "mouseMoved", ["x"] = x + dx, ["y"] = y, ["buttons"] = 1, ["pointerType"] = device });
		await (beforeRelease?.Invoke() ?? Task.CompletedTask);
		await session.Cdp.SendAsync("Input.dispatchMouseEvent", new() { ["type"] = "mouseReleased", ["x"] = x + dx, ["y"] = y, ["button"] = "left", ["buttons"] = 0, ["clickCount"] = 1, ["pointerType"] = device });
	}

	// The root's interaction state, whether it marks a navigation, and the computed visibility of an
	// x tick label, the cursor label, a sample marker and the host readout's time.
	private static Task<string[]> PanFeedbackAsync(IPage page)
		=> page.EvaluateAsync<string[]>("""
			() => {
			    const root = document.querySelector('[data-qa=cursor] .ggnet');
			    return [root.getAttribute('data-ggnet-interaction') ?? 'none', root.hasAttribute('data-ggnet-navigation') ? 'navigation' : 'none',
			        ...['[data-qa=cursor] .x-break-label', '[data-qa=cursor] .crosshair-label', '[data-qa=cursor] .crosshair-marker', '[data-qa=cursor-time]']
			            .map(selector => document.querySelector(selector))
			            .map(element => element ? getComputedStyle(element).visibility : 'absent')];
			}
			""");

	[Theory]
	[InlineData("mouse")]
	[InlineData("pen")]
	public async Task AMouseOrPenPanPreviewHidesStaleValuesAndCarriesThePin(string device)
	{
		// Arrange

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		await session.Page.Mouse.ClickAsync((float)(panel.X + panel.Width * 0.5), (float)y);
		await session.WaitForCursorAsync("cursor", cursor => cursor.Pinned && cursor.Label.Length > 0 && Math.Abs(cursor.Fraction - 0.5) < 0.003);
		await session.Page.WaitForTimeoutAsync(300);
		var pinned = await session.CursorsAsync("cursor");
		var idle = await PanFeedbackAsync(session.Page);
		string[] dragging = [];
		frames.Hold();

		// Act

		await DragAsync(session, device, panel.X + panel.Width * 0.6, y, -80, async () => dragging = await PanFeedbackAsync(session.Page));
		await frames.WaitForHeldMessageAsync();
		await session.Page.WaitForTimeoutAsync(2000);
		var held = await PanFeedbackAsync(session.Page);
		var carried = await session.CursorsAsync("cursor");
		frames.Release();
		await session.WaitForTextAsync("view-changes", "1");
		await session.Page.WaitForTimeoutAsync(300);
		var settled = await PanFeedbackAsync(session.Page);
		var committed = await session.CursorsAsync("cursor");

		// Assert

		using var _ = new AssertionScope();
		idle.Should().Equal("none", "none", "visible", "visible", "visible", "visible");
		dragging.Should().Equal("navigating", "navigation", "hidden", "hidden", "hidden", "hidden");
		held.Should().Equal("pending", "navigation", "hidden", "hidden", "hidden", "hidden");
		carried.Should().HaveCount(4).And.OnlyContain(cursor => cursor.Visible && cursor.Pinned);
		carried.Select(cursor => cursor.X).Should().Equal(pinned.Select(cursor => cursor.X - 80), (actual, expected) => Math.Abs(actual - expected) < 1.5);
		settled.Should().Equal("none", "none", "visible", "visible", "visible", "visible");
		committed.Select(cursor => cursor.X).Should().Equal(pinned.Select(cursor => cursor.X - 80), (actual, expected) => Math.Abs(actual - expected) < 1.5);
		(await session.PreviewsAsync("cursor")).Should().OnlyContain(transform => transform == string.Empty);
		session.Errors.Should().BeEmpty();
	}

	// Per panel, the preview transform on the marks and the one on the cursor group; empty when none.
	private static Task<string[][]> PanAndCursorTransformsAsync(IPage page)
		=> page.EvaluateAsync<string[][]>("""
			() => [...document.querySelectorAll('[data-qa=cursor] [data-ggnet-frame]')].map(capture =>
			    [capture.querySelector('[data-ggnet-pan]')?.getAttribute('transform') ?? '', capture.querySelector('[data-ggnet-cursor]')?.getAttribute('transform') ?? ''])
			""");

	[Theory]
	[InlineData("dragging")]
	[InlineData("pending")]
	public async Task APinnedLineStaysOnItsDataWhenReadingsArriveUnderAMousePanPreview(string phase)
	{
		// Arrange — appended readings widen the x range, so their frame redraws the marks and the
		// cursor against another window while the pan is still only a preview.

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var x = panel.X + panel.Width * 0.6;
		var y = panel.Y + panel.Height / 3;
		await session.Page.Mouse.ClickAsync((float)(panel.X + panel.Width * 0.5), (float)y);
		await session.WaitForCursorAsync("cursor", cursor => cursor.Pinned && cursor.Label.Length > 0 && Math.Abs(cursor.Fraction - 0.5) < 0.003);
		await session.Page.WaitForTimeoutAsync(300);
		var before = await FramesAsync(session.Page);
		RenderedFrame[] appended = [];
		string[][] previews = [];
		Cursor[] carried = [];
		async Task ReadAsync()
		{
			await session.Page.WaitForTimeoutAsync(500);
			appended = await FramesAsync(session.Page);
			previews = await PanAndCursorTransformsAsync(session.Page);
			carried = await session.CursorsAsync("cursor");
		}

		// Act

		if (phase == "dragging")
		{
			await DragAsync(session, "mouse", x, y, -80, async () =>
			{
				await session.Page.Locator("[data-qa=append]").DispatchEventAsync("click");
				await ReadAsync();
			});
		}
		else
		{
			frames.Hold();
			await session.Page.Locator("[data-qa=append]").ClickAsync();
			await frames.WaitForHeldMessageAsync();
			await session.Page.WaitForTimeoutAsync(500);
			var telemetry = frames.HeldMessages;
			await DragAsync(session, "mouse", x, y, -80);
			await frames.WaitForHeldMessagesAsync(telemetry + 1);
			await session.Page.WaitForTimeoutAsync(300);
			frames.ReleaseNext(telemetry);
			await ReadAsync();
			frames.Release();
		}

		await session.WaitForTextAsync("view-changes", "1");
		await session.Page.WaitForTimeoutAsync(300);
		var committed = await session.CursorsAsync("cursor");

		// Assert

		using var _ = new AssertionScope();
		appended.Select(frame => frame.Geometry).Should().NotEqual(before.Select(frame => frame.Geometry));
		previews.Should().HaveCount(4).And.OnlyContain(pair => pair[0].Length > 0 && pair[1] == pair[0]);
		carried.Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned);
		committed.Select(cursor => cursor.X).Should().Equal(carried.Select(cursor => cursor.X), (actual, expected) => Math.Abs(actual - expected) < 1.5);
		(await PanAndCursorTransformsAsync(session.Page)).Should().OnlyContain(pair => pair[0].Length == 0 && pair[1].Length == 0);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task AMousePanInUnavailableHidesTheStaleMarkersUntilItSettles()
	{
		// Arrange — a touch inspection whose frame never arrives makes the chart unavailable; the
		// mouse still pans it.

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		await PinAsync(session, panel.X + panel.Width * 0.5, y);
		frames.Hold();
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.2, y);
		await frames.WaitForHeldMessageAsync();
		await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-qa=cursor] .ggnet').dataset.ggnetInteraction === 'unavailable'", null, new() { Timeout = 15_000 });
		frames.Release();
		await session.WaitForTextAsync("position-changes", "2");
		await session.Page.WaitForTimeoutAsync(300);
		var idle = await PanFeedbackAsync(session.Page);
		string[] dragging = [];

		// Act

		await DragAsync(session, "mouse", panel.X + panel.Width * 0.6, y, -80, async () => dragging = await PanFeedbackAsync(session.Page));
		await session.WaitForTextAsync("view-changes", "1");
		await session.Page.WaitForTimeoutAsync(300);
		var settled = await PanFeedbackAsync(session.Page);

		// Assert

		using var _ = new AssertionScope();
		idle.Should().Equal("unavailable", "none", "visible", "visible", "visible", "visible");
		dragging.Take(5).Should().Equal("unavailable", "navigation", "hidden", "hidden", "hidden");
		settled.Should().Equal("unavailable", "none", "visible", "visible", "visible", "visible");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task AMousePanPreviewDroppedAtTheRecoveryBoundLeavesNoFeedbackBehind()
	{
		// Arrange

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var (x, y) = await session.PointAsync("cursor", 0, 0.6);
		frames.Hold();

		// Act

		await DragAsync(session, "mouse", x, y, -80);
		await frames.WaitForHeldMessageAsync();
		await session.Page.WaitForTimeoutAsync(2000);
		var held = await PanFeedbackAsync(session.Page);
		await session.Page.WaitForTimeoutAsync(9000);
		var dropped = await PanFeedbackAsync(session.Page);
		var previews = await session.PreviewsAsync("cursor");
		frames.Release();
		await session.WaitForTextAsync("view-changes", "1");

		// Assert

		using var _ = new AssertionScope();
		held.Take(3).Should().Equal("pending", "navigation", "hidden");
		dropped.Take(3).Should().Equal("none", "none", "visible");
		previews.Should().OnlyContain(transform => transform == string.Empty);
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData("mouse", 300)]
	[InlineData("mouse", 2000)]
	[InlineData("pen", 300)]
	[InlineData("pen", 2000)]
	public async Task AMouseOrPenPanPreviewPersistsUntilItsFrame(string device, int delay)
	{
		// Arrange

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var (x, y) = await session.PointAsync("cursor", 0, 0.6);
		var before = await FramesAsync(session.Page);
		frames.Hold();

		// Act

		await DragAsync(session, device, x, y, -80);
		await frames.WaitForHeldMessageAsync();
		await session.Page.WaitForTimeoutAsync(delay);
		var previews = await session.PreviewsAsync("cursor");
		var held = await FramesAsync(session.Page);
		frames.Release();
		await session.WaitForTextAsync("view-changes", "1");
		await session.Page.WaitForTimeoutAsync(300);

		// Assert

		using var _ = new AssertionScope();
		previews.Should().HaveCount(4).And.OnlyContain(transform => transform.Length > 0);
		held.Should().BeEquivalentTo(before, options => options.WithStrictOrdering());
		(await session.TextAsync("view-changes")).Should().Be("1");
		(await session.PreviewsAsync("cursor")).Should().OnlyContain(transform => transform == string.Empty);
		(await FramesAsync(session.Page)).Select(frame => frame.Geometry).Should().NotEqual(before.Select(frame => frame.Geometry));
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData(300)]
	[InlineData(2000)]
	public async Task AMouseClickPinPreviewPersistsUntilItsFrame(int delay)
	{
		// Arrange

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		await session.Page.Mouse.ClickAsync((float)(panel.X + panel.Width * 0.2), (float)y);
		await session.WaitForTextAsync("pinned", "pinned");
		var oldTime = await session.TextAsync("cursor-time");
		frames.Hold();

		// Act

		await session.Page.Mouse.ClickAsync((float)(panel.X + panel.Width * 0.8), (float)y);
		await frames.WaitForHeldMessageAsync();
		await session.Page.WaitForTimeoutAsync(delay);
		var during = await session.CursorsAsync("cursor");
		var staleTime = await session.TextAsync("cursor-time");
		frames.Release();
		await session.WaitForCursorAsync("cursor", cursor => cursor.Pinned && cursor.Label.Length > 0 && Math.Abs(cursor.Fraction - 0.8) < 0.003);
		await session.Page.WaitForTimeoutAsync(300);

		// Assert

		using var _ = new AssertionScope();
		during.Should().HaveCount(4).And.OnlyContain(cursor => cursor.Visible && Math.Abs(cursor.Fraction - 0.8) < 0.003);
		staleTime.Should().Be(oldTime);
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.8) < 0.003);
		(await session.TextAsync("cursor-time")).Should().NotBe(oldTime);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task ATelemetryFrameDoesNotSettleAPendingMousePan()
	{
		// Arrange — the appended readings render first, so their frame reaches the browser while the
		// pan's own frame is still held behind it.

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var (x, y) = await session.PointAsync("cursor", 0, 0.6);
		var before = await FramesAsync(session.Page);
		frames.Hold();
		await session.Page.Locator("[data-qa=append]").ClickAsync();
		await frames.WaitForHeldMessageAsync();
		await session.Page.WaitForTimeoutAsync(500);
		var telemetry = frames.HeldMessages;
		await DragAsync(session, "mouse", x, y, -80);
		await frames.WaitForHeldMessagesAsync(telemetry + 1);
		await session.Page.WaitForTimeoutAsync(300);

		// Act

		frames.ReleaseNext(telemetry);
		await session.Page.WaitForTimeoutAsync(500);
		var during = await FramesAsync(session.Page);
		var previews = await session.PreviewsAsync("cursor");
		var changes = await session.TextAsync("view-changes");
		frames.Release();
		await session.WaitForTextAsync("view-changes", "1");
		await session.Page.WaitForTimeoutAsync(300);

		// Assert

		using var _ = new AssertionScope();
		during.Select(frame => frame.Geometry).Should().NotEqual(before.Select(frame => frame.Geometry));
		previews.Should().HaveCount(4).And.OnlyContain(transform => transform.Length > 0);
		changes.Should().Be("0");
		(await session.TextAsync("view-changes")).Should().Be("1");
		(await session.PreviewsAsync("cursor")).Should().OnlyContain(transform => transform == string.Empty);
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData("clear")]
	[InlineData("escape")]
	[InlineData("clear-in-flight")]
	public async Task ATelemetryFrameAfterAClearDoesNotSettleAPendingMousePan(string clear)
	{
		// Arrange — clearing advances the plot's epoch without rendering the marks, so the appended
		// readings' frame carries an epoch no earlier frame showed. In flight, not even the cleared
		// cursor has reached the browser when the pan is released.

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var (x, y) = await session.PointAsync("cursor", 0, 0.6);
		await session.Page.Mouse.ClickAsync((float)(x - 100), (float)y);
		await session.WaitForTextAsync("pinned", "pinned");
		if (clear == "escape")
		{
			await session.Page.Keyboard.PressAsync("Escape");
		}
		else if (clear == "clear")
		{
			await session.Page.Locator("[data-qa=clear]").ClickAsync();
		}
		if (clear != "clear-in-flight")
		{
			await session.Page.Locator("[data-qa=empty]").WaitForAsync();
		}
		var before = await FramesAsync(session.Page);
		frames.Hold();
		if (clear == "clear-in-flight")
		{
			await session.Page.Locator("[data-qa=clear]").ClickAsync();
			await frames.WaitForHeldMessageAsync();
			await session.Page.WaitForTimeoutAsync(300);
		}
		var cleared = frames.HeldMessages;
		await session.Page.Locator("[data-qa=append]").ClickAsync();
		await frames.WaitForHeldMessagesAsync(cleared + 1);
		await session.Page.WaitForTimeoutAsync(500);
		var telemetry = frames.HeldMessages;
		await DragAsync(session, "mouse", x, y, -80);
		await frames.WaitForHeldMessagesAsync(telemetry + 1);
		await session.Page.WaitForTimeoutAsync(300);

		// Act

		frames.ReleaseNext(telemetry);
		await session.Page.WaitForTimeoutAsync(500);
		var during = await FramesAsync(session.Page);
		var previews = await session.PreviewsAsync("cursor");
		var changes = await session.TextAsync("view-changes");
		frames.Release();
		await session.WaitForTextAsync("view-changes", "1");
		await session.Page.WaitForTimeoutAsync(300);

		// Assert

		using var _ = new AssertionScope();
		during.Select(frame => frame.Geometry).Should().NotEqual(before.Select(frame => frame.Geometry));
		previews.Should().HaveCount(4).And.OnlyContain(transform => transform.Length > 0);
		changes.Should().Be("0");
		(await session.TextAsync("view-changes")).Should().Be("1");
		(await session.PreviewsAsync("cursor")).Should().OnlyContain(transform => transform == string.Empty);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task AHostViewportCommandSupersedesAPendingMousePan()
	{
		// Arrange — a render in flight makes the pan and the host's reset share one frame.

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var (x, y) = await session.PointAsync("cursor", 0, 0.6);
		var initial = await session.Page.Locator("[data-qa=cursor] [data-ggnet-marks-frame]").First.GetAttributeAsync("data-ggnet-view");
		frames.Hold();
		await session.Page.Locator("[data-qa=append]").ClickAsync();
		await frames.WaitForHeldMessageAsync();

		// Act

		await DragAsync(session, "mouse", x, y, -80);
		await session.Page.WaitForTimeoutAsync(300);
		var previews = await session.PreviewsAsync("cursor");
		await session.Page.Locator("[data-qa=reset]").ClickAsync();
		await session.Page.WaitForTimeoutAsync(300);
		frames.Release();
		await session.Page.WaitForFunctionAsync("() => [...document.querySelectorAll('[data-qa=cursor] [data-ggnet-pan]')].every(marks => !marks.hasAttribute('transform'))", null, new() { Timeout = 5_000 });
		await session.Page.WaitForTimeoutAsync(500);
		var final = await session.Page.Locator("[data-qa=cursor] [data-ggnet-marks-frame]").First.GetAttributeAsync("data-ggnet-view");

		// Assert

		using var _ = new AssertionScope();
		previews.Should().HaveCount(4).And.OnlyContain(transform => transform.Length > 0);
		(await session.PreviewsAsync("cursor")).Should().OnlyContain(transform => transform == string.Empty);
		long.Parse(final!.Split(' ')[1], System.Globalization.CultureInfo.InvariantCulture).Should().BeGreaterThan(long.Parse(initial!.Split(' ')[1], System.Globalization.CultureInfo.InvariantCulture) + 1, "the pan and the reset each advanced the epoch");
		(await session.TextAsync("window")).Should().NotBeEmpty();
		session.Errors.Should().BeEmpty();
	}

	private static async Task PinAsync(Session session, double x, double y)
	{
		await TapAsync(session.Page, session.Cdp, x, y);
		await session.WaitForTextAsync("pinned", "pinned");
		await session.Page.WaitForFunctionAsync("() => !document.querySelector('[data-qa=cursor] .ggnet').hasAttribute('data-ggnet-interaction')");
	}

	// Computed visibility of the cursor label, a sample marker and the host readout's time.
	private static Task<string[]> AcknowledgedValuesAsync(IPage page)
		=> page.EvaluateAsync<string[]>("""
			() => ['[data-qa=cursor] .crosshair-label', '[data-qa=cursor] .crosshair-marker', '[data-qa=cursor-time]']
			    .map(selector => document.querySelector(selector))
			    .map(element => element ? getComputedStyle(element).visibility : 'absent')
			""");

	private static async Task ExpectNoPendingAsync(IPage page)
	{
		(await page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction")).Should().BeNull();
		(await page.Locator("[data-ggnet-provisional]").CountAsync()).Should().Be(0);
	}

	[Theory]
	[InlineData(false, 300)]
	[InlineData(false, 2000)]
	[InlineData(true, 300)]
	[InlineData(true, 2000)]
	public async Task RenderedFrameStampsWaitForServerDelivery(bool holdInvocations, int delay)
	{
		// Arrange — holding invocations delays the navigation call itself; holding frames delays only its rendered reply.

		await using var frames = new CircuitGate(holdInvocations);
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var before = await FramesAsync(session.Page);
		frames.Hold();

		// Act

		await SwipeAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.6, panel.Y + panel.Height / 3, -120, 0);
		try
		{
			await frames.WaitForHeldMessageAsync();
		}
		catch (TimeoutException exception)
		{
			throw new TimeoutException($"{exception.Message} Browser errors: {string.Join(" | ", session.Errors)}; view changes: {await session.TextAsync("view-changes")}", exception);
		}
		await session.Page.WaitForTimeoutAsync(delay);
		var held = await FramesAsync(session.Page);
		var previews = await session.PreviewsAsync("cursor");
		var pending = await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction");
		var ticksHidden = await session.Page.Locator("[data-qa=cursor] .x-break-label").First.EvaluateAsync<bool>("element => getComputedStyle(element).visibility === 'hidden'");
		frames.Release();
		await session.WaitForTextAsync("view-changes", "1");
		await session.Page.WaitForFunctionAsync("""
			before => [...document.querySelectorAll('[data-qa=cursor] [data-ggnet-frame]')].every((panel, i) =>
			    panel.dataset.ggnetFrame !== before[i] &&
			    panel.dataset.ggnetFrame === panel.querySelector('[data-ggnet-marks-frame]').dataset.ggnetMarksFrame)
			""", before.Select(frame => frame.Stamp).ToArray());
		var after = await FramesAsync(session.Page);
		await session.Page.WaitForFunctionAsync("() => !document.querySelector('[data-qa=cursor] .ggnet').hasAttribute('data-ggnet-interaction')");

		// Assert

		using var _ = new AssertionScope();
		before.Should().HaveCount(4);
		held.Should().BeEquivalentTo(before, options => options.WithStrictOrdering());
		pending.Should().Be("pending");
		ticksHidden.Should().BeTrue();
		previews.Should().HaveCount(4).And.OnlyContain(transform => transform.Length > 0);
		(await session.PreviewsAsync("cursor")).Should().OnlyContain(transform => transform == string.Empty);
		after.Should().HaveCount(4);
		for (var i = 0; i < after.Length; i++)
		{
			after[i].Stamp.Should().NotBe(before[i].Stamp).And.Be(after[i].MarksStamp);
			after[i].Geometry.Should().NotBe(before[i].Geometry);
		}
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task SuccessiveNavigationComposesWhileServerFramesAreHeld()
	{
		// Arrange

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var before = await session.Page.Locator("[data-qa=cursor] [data-ggnet-marks-frame]").First.GetAttributeAsync("data-ggnet-view");
		var initial = before!.Split(' ').Skip(3).Select(value => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
		frames.Hold();

		// Act

		await SwipeAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.6, panel.Y + panel.Height / 3, -120, 0);
		await frames.WaitForHeldMessageAsync();
		await SwipeAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.6, panel.Y + panel.Height / 3, -120, 0);
		await session.Page.WaitForTimeoutAsync(2000);
		var pending = await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction");
		frames.Release();
		await session.WaitForTextAsync("view-changes", "2");
		await session.Page.WaitForFunctionAsync("() => !document.querySelector('[data-qa=cursor] .ggnet').hasAttribute('data-ggnet-interaction')");
		var after = await session.Page.Locator("[data-qa=cursor] [data-ggnet-marks-frame]").First.GetAttributeAsync("data-ggnet-view");
		var final = after!.Split(' ').Skip(3).Select(value => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

		// Assert

		using var _ = new AssertionScope();
		var span = initial[1] - initial[0];
		pending.Should().Be("pending");
		final[0].Should().BeApproximately(initial[0] + span * 240 / panel.Width, span * 0.000001);
		(final[1] - final[0]).Should().BeApproximately(span, span * 0.000001);
		(await session.PreviewsAsync("cursor")).Should().OnlyContain(transform => transform == string.Empty);
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task AWheelOrMouseDragAfterAPendingTouchPanWinsWithoutQuarantine(bool wheel)
	{
		// Arrange — a render in flight makes the touch commit and the mouse commit share one frame.

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var x = panel.X + panel.Width * 0.5;
		var y = panel.Y + panel.Height / 3;
		frames.Hold();
		await session.Page.Locator("[data-qa=append]").ClickAsync();
		await frames.WaitForHeldMessageAsync();

		// Act

		await SwipeAsync(session.Page, session.Cdp, x, y, -120, 0);
		await session.Page.WaitForTimeoutAsync(300);
		await session.Page.Mouse.MoveAsync((float)x, (float)y);
		if (wheel)
		{
			await session.Page.Mouse.WheelAsync(0, -120);
		}
		else
		{
			await session.Page.Mouse.DownAsync();
			await session.Page.Mouse.MoveAsync((float)x + 90, (float)y, new() { Steps = 6 });
			await session.Page.Mouse.UpAsync();
		}
		await session.Page.WaitForTimeoutAsync(500);
		frames.Release();
		await session.WaitForTextAsync("view-changes", "2");
		for (var i = 0; i < 50 && await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction") is not null; i++)
		{
			await session.Page.WaitForTimeoutAsync(100);
		}
		var settled = await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction");
		var previews = await session.PreviewsAsync("cursor");
		await SwipeAsync(session.Page, session.Cdp, x, y, -60, 0);
		await session.WaitForTextAsync("view-changes", "3");
		await session.Page.WaitForTimeoutAsync(300);

		// Assert

		using var _ = new AssertionScope();
		settled.Should().BeNull();
		previews.Should().OnlyContain(transform => transform == string.Empty);
		(await session.TextAsync("view-changes")).Should().Be("3");
		await ExpectNoPendingAsync(session.Page);
		(await session.PreviewsAsync("cursor")).Should().OnlyContain(transform => transform == string.Empty);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task TelemetryDuringPendingNavigationKeepsTheIntendedWindowAndPin()
	{
		// Arrange — the appended readings render first, so their frame reaches the browser while the
		// navigation's own frame is still held behind it.

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.5, y);
		await session.WaitForTextAsync("pinned", "pinned");
		var pinnedTime = await session.TextAsync("cursor-time");
		var initial = ParseWindow(await session.Page.Locator("[data-qa=cursor] [data-ggnet-marks-frame]").First.GetAttributeAsync("data-ggnet-view"));
		var before = await FramesAsync(session.Page);
		frames.Hold();
		await session.Page.Locator("[data-qa=append]").ClickAsync();
		await frames.WaitForHeldMessageAsync();
		await session.Page.WaitForTimeoutAsync(500);
		var telemetry = frames.HeldMessages;
		await SwipeAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.6, y, -120, 0);
		await frames.WaitForHeldMessagesAsync(telemetry + 1);
		await session.Page.WaitForTimeoutAsync(300);

		// Act

		frames.ReleaseNext(telemetry);
		await session.Page.WaitForTimeoutAsync(500);
		var during = await FramesAsync(session.Page);
		var pending = await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction");
		var previews = await session.PreviewsAsync("cursor");
		frames.Release();
		await session.WaitForTextAsync("view-changes", "1");
		await session.Page.WaitForFunctionAsync("() => !document.querySelector('[data-qa=cursor] .ggnet').hasAttribute('data-ggnet-interaction')");
		var final = ParseWindow(await session.Page.Locator("[data-qa=cursor] [data-ggnet-marks-frame]").First.GetAttributeAsync("data-ggnet-view"));

		// Assert

		using var _ = new AssertionScope();
		var span = initial[1] - initial[0];
		during.Select(frame => frame.Geometry).Should().NotEqual(before.Select(frame => frame.Geometry));
		pending.Should().Be("pending");
		previews.Should().HaveCount(4).And.OnlyContain(transform => transform.Length > 0);
		final[0].Should().BeApproximately(initial[0] + span * 120 / panel.Width, span * 0.000001);
		(final[1] - final[0]).Should().BeApproximately(span, span * 0.000001);
		(await session.TextAsync("cursor-time")).Should().Be(pinnedTime);
		(await session.TextAsync("pinned")).Should().Be("pinned");
		(await session.PreviewsAsync("cursor")).Should().OnlyContain(transform => transform == string.Empty);
		session.Errors.Should().BeEmpty();
	}

	private static double[] ParseWindow(string? view)
		=> [.. view!.Split(' ').Skip(3).Select(value => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture))];

	[Fact]
	public async Task DisconnectDuringPendingNavigationShowsTheLastAcknowledgedFrame()
	{
		// Arrange

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		await PinAsync(session, panel.X + panel.Width * 0.5, y);
		var before = await FramesAsync(session.Page);
		await HideReconnectOverlayAsync(session.Page);
		frames.Hold();
		await SwipeAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.6, y, -120, 0);
		await frames.WaitForHeldMessageAsync();
		var pending = await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction");
		var errorsBefore = session.Errors.ToArray();
		var clock = System.Diagnostics.Stopwatch.StartNew();

		// Act

		await frames.DisconnectAsync();
		await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-qa=cursor] .ggnet').dataset.ggnetInteraction === 'unavailable'", null, new() { Timeout = 15_000 });
		var elapsed = clock.Elapsed;
		var previews = await session.PreviewsAsync("cursor");
		var ticksHidden = await session.Page.Locator("[data-qa=cursor] .x-break-label").First.EvaluateAsync<bool>("element => getComputedStyle(element).visibility === 'hidden'");
		var shown = await AcknowledgedValuesAsync(session.Page);
		await SwipeAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.6, y, -120, 0);
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.3, y);
		await session.Page.WaitForTimeoutAsync(400);

		// Assert

		TestContext.Current.TestOutputHelper?.WriteLine(FormattableString.Invariant($"unavailable after {elapsed.TotalMilliseconds:F0} ms; errors after disconnect: {string.Join(" | ", session.Errors.Skip(errorsBefore.Length))}"));
		using var _ = new AssertionScope();
		pending.Should().Be("pending");
		errorsBefore.Should().BeEmpty();
		previews.Should().OnlyContain(transform => transform == string.Empty);
		ticksHidden.Should().BeFalse();
		shown.Should().Equal("visible", "visible", "visible");
		(await FramesAsync(session.Page)).Should().BeEquivalentTo(before, options => options.WithStrictOrdering());
		(await session.PreviewsAsync("cursor")).Should().OnlyContain(transform => transform == string.Empty);
		(await session.Page.Locator("[data-ggnet-provisional]").CountAsync()).Should().Be(0);
		(await session.TextAsync("view-changes")).Should().Be("0");
		(await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction")).Should().Be("unavailable");
	}

	[Fact]
	public async Task GesturesAfterADisconnectLeaveNoTransientPaint()
	{
		// Arrange

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		await PinAsync(session, panel.X + panel.Width * 0.5, y);
		var before = await FramesAsync(session.Page);
		await HideReconnectOverlayAsync(session.Page);
		await frames.DisconnectAsync();
		await session.Page.WaitForTimeoutAsync(500);
		var clock = System.Diagnostics.Stopwatch.StartNew();

		// Act

		await SwipeAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.6, y, -120, 0);
		await session.Page.WaitForFunctionAsync("""
			() => document.querySelector('[data-qa=cursor] .ggnet').dataset.ggnetInteraction === 'unavailable'
			    && [...document.querySelectorAll('[data-qa=cursor] [data-ggnet-pan]')].every(marks => !marks.hasAttribute('transform'))
			""", null, new() { Timeout = 15_000 });
		var elapsed = clock.Elapsed;
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.3, y);
		await session.Page.WaitForTimeoutAsync(400);

		// Assert

		TestContext.Current.TestOutputHelper?.WriteLine(FormattableString.Invariant($"settled unavailable after {elapsed.TotalMilliseconds:F0} ms; errors: {string.Join(" | ", session.Errors)}"));
		using var _ = new AssertionScope();
		(await FramesAsync(session.Page)).Should().BeEquivalentTo(before, options => options.WithStrictOrdering());
		(await session.PreviewsAsync("cursor")).Should().OnlyContain(transform => transform == string.Empty);
		(await session.Page.Locator("[data-ggnet-provisional]").CountAsync()).Should().Be(0);
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.5) < 0.003);
		(await AcknowledgedValuesAsync(session.Page)).Should().Equal("visible", "visible", "visible");
	}

	// Blazor's default reconnect overlay covers the page and would swallow the gestures under test;
	// hiding it models a host whose reconnect UI does not block input.
	private static Task<IElementHandle> HideReconnectOverlayAsync(IPage page)
		=> page.AddStyleTagAsync(new() { Content = "#components-reconnect-modal { display: none !important; }" });

	private sealed class RenderedFrame
	{
		public string Stamp { get; set; } = string.Empty;
		public string MarksStamp { get; set; } = string.Empty;
		public string Geometry { get; set; } = string.Empty;
	}

	private static Task<RenderedFrame[]> FramesAsync(IPage page) => page.EvaluateAsync<RenderedFrame[]>("""
		() => [...document.querySelectorAll('[data-qa=cursor] [data-ggnet-frame]')].map(panel => {
		    const marks = panel.querySelector('[data-ggnet-marks-frame]');
		    return {
		        stamp: panel.dataset.ggnetFrame,
		        marksStamp: marks.dataset.ggnetMarksFrame,
		        geometry: JSON.stringify([...marks.querySelectorAll('*')].map(mark =>
		            ['d', 'cx', 'cy', 'x', 'y', 'transform'].map(name => mark.getAttribute(name))))
		    };
		})
		""");
}

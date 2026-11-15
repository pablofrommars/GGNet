using static GGNet.E2ETests.Fixtures.TouchInput;

namespace GGNet.E2ETests;

public partial class TimeCursorTests
{
	[Theory]
	[InlineData("pointercancel")]
	[InlineData("lostpointercapture")]
	[InlineData("blur")]
	[InlineData("resize")]
	[InlineData("outside-second")]
	[InlineData("third")]
	public async Task InterruptedScrubRestoresThePinAndAcceptsTheNextGesture(string interruption)
	{
		// Arrange — contacts are real CDP input; cancellation/lifecycle events are injected explicitly.

		await using var session = await OpenAsync();
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.2, y);
		await session.WaitForTextAsync("pinned", "pinned");
		var original = await session.TextAsync("cursor-time");
		await session.Page.EvaluateAsync("""
			() => window.addEventListener('pointerdown', e => {
			    if (e.pointerType === 'touch' && e.isPrimary) window.__touchId = e.pointerId;
			}, { capture: true })
			""");
		var x = panel.X + panel.Width * 0.5;

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		await session.Page.WaitForTimeoutAsync(450);
		await TouchAsync(session.Cdp, "touchMove", Point(x + 60, y));
		await session.WaitForTextAsync("pinned", "following");
		if (interruption == "outside-second")
		{
			await TouchAsync(session.Cdp, "touchStart", Point(x + 60, y), Point(2, 2, 2));
		}
		else if (interruption == "third")
		{
			await TouchAsync(session.Cdp, "touchStart", Point(x + 60, y), Point(x - 60, y + 30, 2), Point(x + 90, y + 60, 3));
		}
		else
		{
			await session.Page.EvaluateAsync("""
				kind => {
				    const capture = document.querySelector('[data-qa=cursor] [data-ggnet-frame]');
				    if (kind === 'blur' || kind === 'resize') window.dispatchEvent(new Event(kind));
				    else capture.dispatchEvent(new PointerEvent(kind, { bubbles: true, pointerType: 'touch', pointerId: window.__touchId }));
				}
				""", interruption);
		}
		await TouchAsync(session.Cdp, "touchCancel");
		await session.WaitForTextAsync("pinned", "pinned");
		await session.Page.WaitForTimeoutAsync(300);
		var restored = await session.TextAsync("cursor-time");
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.8, y);
		await session.Page.WaitForTimeoutAsync(300);

		// Assert

		using var _ = new AssertionScope();
		restored.Should().Be(original);
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.8) < 0.003);
		(await session.TextAsync("view-changes")).Should().Be("0");
		(await session.TextAsync("ancestor-clicks")).Should().Be("0");
		await ExpectNoPendingAsync(session.Page);
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData("pan")]
	[InlineData("hold")]
	[InlineData("second")]
	public async Task AFailedPointerCaptureEndsTheGestureWithoutATrace(string path)
	{
		// Arrange — the capture failure is injected: a real one needs the pointer to vanish between its event and the call.

		await using var session = await OpenAsync();
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		var x = panel.X + panel.Width * 0.5;
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.2, y);
		await session.WaitForTextAsync("pinned", "pinned");
		var original = await session.TextAsync("cursor-time");
		await session.Page.EvaluateAsync("""
			() => {
			    window.__setPointerCapture = Element.prototype.setPointerCapture;
			    Element.prototype.setPointerCapture = function () { throw new DOMException('injected', 'NotFoundError'); };
			}
			""");

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		if (path == "hold")
		{
			await session.Page.WaitForTimeoutAsync(600);
			await TouchAsync(session.Cdp, "touchMove", Point(x + 60, y));
		}
		else if (path == "pan")
		{
			await TouchAsync(session.Cdp, "touchMove", Point(x - 40, y));
			await TouchAsync(session.Cdp, "touchMove", Point(x - 90, y));
		}
		else
		{
			await TouchAsync(session.Cdp, "touchStart", Point(x, y), Point(x + 120, y, 2));
			await TouchAsync(session.Cdp, "touchMove", Point(x - 60, y), Point(x + 60, y, 2));
		}
		await session.Page.WaitForTimeoutAsync(300);
		var during = await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction");
		var ticksHidden = await session.Page.Locator("[data-qa=cursor] .ggnet[data-ggnet-navigation]").CountAsync();
		var previews = await session.PreviewsAsync("cursor");
		await TouchAsync(session.Cdp, "touchEnd");
		await session.Page.WaitForTimeoutAsync(600);
		var afterTime = await session.TextAsync("cursor-time");
		var afterPinned = await session.TextAsync("pinned");
		var changes = await session.TextAsync("position-changes");
		await session.Page.EvaluateAsync("() => { Element.prototype.setPointerCapture = window.__setPointerCapture; }");
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.8, y);
		await session.WaitForCursorAsync("cursor", cursor => cursor.Pinned && Math.Abs(cursor.Fraction - 0.8) < 0.003);

		// Assert

		using var _ = new AssertionScope();
		during.Should().BeNull();
		ticksHidden.Should().Be(0);
		previews.Should().OnlyContain(transform => transform == string.Empty);
		afterTime.Should().Be(original);
		afterPinned.Should().Be("pinned");
		changes.Should().Be("1");
		(await session.TextAsync("view-changes")).Should().Be("0");
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.8) < 0.003);
		await ExpectNoPendingAsync(session.Page);
		session.Errors.Should().BeEmpty();
	}

	public static TheoryData<string, string> UnfinishedGestureInterruptions()
	{
		var rows = new TheoryData<string, string>();
		foreach (var state in new[] { "hold", "pan", "navigate" })
		{
			foreach (var interruption in new[] { "pointercancel", "lostpointercapture", "blur", "resize" })
			{
				rows.Add(interruption, state);
			}
		}

		return rows;
	}

	[Theory]
	[MemberData(nameof(UnfinishedGestureInterruptions))]
	public async Task AnInterruptedUnfinishedGestureLeavesNoTraceAndAcceptsTheNextOne(string interruption, string state)
	{
		// Arrange — contacts are real CDP input, and so is pointercancel (touchCancel). Lost capture is the
		// browser's own event where the gesture holds an established capture; a pending hold holds none, so
		// it is injected there. Blur and resize are injected window events.

		await using var session = await OpenAsync();
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		var x = panel.X + panel.Width * 0.5;
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.2, y);
		await session.WaitForTextAsync("pinned", "pinned");
		var original = await session.TextAsync("cursor-time");
		await session.Page.EvaluateAsync("""
			() => {
			    window.__touchIds = [];
			    window.addEventListener('pointerdown', e => {
			        if (e.pointerType === 'touch') window.__touchIds.push(e.pointerId);
			    }, { capture: true });
			}
			""");

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		if (state == "pan")
		{
			await TouchAsync(session.Cdp, "touchMove", Point(x - 40, y));
			await TouchAsync(session.Cdp, "touchMove", Point(x - 90, y));
		}
		else if (state == "navigate")
		{
			await TouchAsync(session.Cdp, "touchStart", Point(x, y), Point(x + 120, y, 2));
			await TouchAsync(session.Cdp, "touchMove", Point(x - 60, y), Point(x + 60, y, 2));
		}
		if (state != "hold")
		{
			// The browser applies a requested capture with the pointer's next event; let the moves land first.
			await session.Page.WaitForTimeoutAsync(150);
		}
		var recognized = await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction");
		if (interruption == "pointercancel")
		{
			await TouchAsync(session.Cdp, "touchCancel");
		}
		else
		{
			await session.Page.EvaluateAsync("""
				kind => {
				    const capture = document.querySelector('[data-qa=cursor] [data-ggnet-frame]');
				    const captured = window.__touchIds.find(id => capture.hasPointerCapture(id));
				    if (kind !== 'lostpointercapture') window.dispatchEvent(new Event(kind));
				    else if (captured !== undefined) capture.releasePointerCapture(captured);
				    else capture.dispatchEvent(new PointerEvent(kind, { bubbles: true, pointerType: 'touch', pointerId: window.__touchIds[0] }));
				}
				""", interruption);
			await session.Page.WaitForTimeoutAsync(100);
			if (state == "navigate")
			{
				await TouchAsync(session.Cdp, "touchMove", Point(x - 140, y), Point(x - 20, y, 2));
			}
			else
			{
				await TouchAsync(session.Cdp, "touchMove", Point(x - 140, y));
			}
			await session.Page.WaitForTimeoutAsync(100);
			await TouchAsync(session.Cdp, "touchEnd");
		}
		await session.Page.WaitForTimeoutAsync(700);
		var previews = await session.PreviewsAsync("cursor");
		var ticksHidden = await session.Page.Locator("[data-qa=cursor] .ggnet[data-ggnet-navigation]").CountAsync();
		var restoredTime = await session.TextAsync("cursor-time");
		var restoredPin = await session.TextAsync("pinned");
		var selections = await session.TextAsync("position-changes");
		var interaction = await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction");
		var provisional = await session.Page.Locator("[data-ggnet-provisional]").CountAsync();
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.8, y);
		await session.WaitForCursorAsync("cursor", cursor => cursor.Pinned && Math.Abs(cursor.Fraction - 0.8) < 0.003);

		// Assert

		using var _ = new AssertionScope();
		recognized.Should().Be(state == "hold" ? null : "navigating");
		previews.Should().OnlyContain(transform => transform == string.Empty);
		ticksHidden.Should().Be(0);
		restoredTime.Should().Be(original);
		restoredPin.Should().Be("pinned");
		selections.Should().Be("1");
		interaction.Should().BeNull();
		provisional.Should().Be(0);
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.8) < 0.003);
		(await session.TextAsync("position-changes")).Should().Be("2");
		(await session.TextAsync("view-changes")).Should().Be("0");
		(await session.TextAsync("mark-clicks")).Should().Be("0");
		(await session.TextAsync("ancestor-clicks")).Should().Be("0");
		await ExpectNoPendingAsync(session.Page);
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData("scrub")]
	[InlineData("pan")]
	[InlineData("pending")]
	public async Task LeavingThePageMidGestureOrMidOperationReturnsClean(string moment)
	{
		// Arrange — "pending" leaves while a tap's rendered reply is still held; the others leave with the finger down.

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		var x = panel.X + panel.Width * 0.5;

		// Act

		if (moment == "pending")
		{
			frames.Hold();
			await TapAsync(session.Page, session.Cdp, x, y);
			await frames.WaitForHeldMessageAsync();
		}
		else
		{
			await TouchAsync(session.Cdp, "touchStart", Point(x, y));
			if (moment == "scrub")
			{
				await session.Page.WaitForTimeoutAsync(450);
			}
			await TouchAsync(session.Cdp, "touchMove", Point(x - 40, y));
			await TouchAsync(session.Cdp, "touchMove", Point(x - 90, y));
		}
		var leaving = await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction");
		await session.Page.EvaluateAsync("() => document.querySelector(\"nav a[href='/crosshair']\").click()");
		await session.Page.Locator("h1", new() { HasText = "Crosshair" }).WaitForAsync();
		if (moment == "pending")
		{
			frames.Release();
		}
		else
		{
			await TouchAsync(session.Cdp, "touchMove", Point(x - 140, y));
			await TouchAsync(session.Cdp, "touchEnd");
		}
		await session.Page.WaitForTimeoutAsync(500);
		await session.Page.Locator("nav a[href='/time-cursor']").TapAsync();
		await session.Page.Locator("[data-qa=empty]").WaitForAsync(new() { Timeout = 10_000 });
		await session.Page.WaitForTimeoutAsync(1000);
		var returned = await session.CursorsAsync("cursor");
		panel = await session.PanelAsync("cursor", 0);
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.8, panel.Y + panel.Height / 3);
		await session.WaitForCursorAsync("cursor", cursor => cursor.Pinned && Math.Abs(cursor.Fraction - 0.8) < 0.003);
		await session.Page.WaitForTimeoutAsync(400);

		// Assert

		using var _ = new AssertionScope();
		leaving.Should().Be(moment switch { "scrub" => "inspecting", "pan" => "navigating", _ => "pending" });
		returned.Should().HaveCount(4).And.OnlyContain(cursor => !cursor.Visible);
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.8) < 0.003);
		(await session.TextAsync("position-changes")).Should().Be("1", "one handler set reports one tap once");
		(await session.TextAsync("view-changes")).Should().Be("0");
		(await session.TextAsync("ancestor-clicks")).Should().Be("0");
		(await session.PreviewsAsync("cursor")).Should().OnlyContain(transform => transform == string.Empty);
		await ExpectNoPendingAsync(session.Page);
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData("pending-pan")]
	[InlineData("scrub")]
	[InlineData("unavailable")]
	public async Task AReplacedContextLeavesNoTouchFeedbackBehind(string state)
	{
		// Arrange — a new context replaces every panel, while the plot root that carries the
		// feedback attributes stays. A render in flight keeps the gesture unacknowledged until
		// the replacement's frame.

		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(frames: frames);
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		await PinAsync(session, panel.X + panel.Width * 0.5, y);
		frames.Hold();
		if (state == "unavailable")
		{
			await SwipeAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.6, y, -120, 0);
			await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-qa=cursor] .ggnet').dataset.ggnetInteraction === 'unavailable'", null, new() { Timeout = 15_000 });
		}
		else
		{
			await session.Page.Locator("[data-qa=append]").ClickAsync();
			await frames.WaitForHeldMessageAsync();
			await session.Page.WaitForTimeoutAsync(300);
			if (state == "scrub")
			{
				await TouchAsync(session.Cdp, "touchStart", Point(panel.X + panel.Width * 0.3, y));
				await session.Page.WaitForTimeoutAsync(450);
				await TouchAsync(session.Cdp, "touchMove", Point(panel.X + panel.Width * 0.3 + 60, y));
			}
			else
			{
				await SwipeAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.6, y, -120, 0);
			}
			await session.Page.WaitForTimeoutAsync(300);
		}
		var during = await session.Page.Locator("[data-qa=cursor] .ggnet").GetAttributeAsync("data-ggnet-interaction");

		// Act

		await session.Page.Locator("[data-qa=replace]").ClickAsync();
		await session.Page.WaitForTimeoutAsync(500);
		frames.Release();
		await session.Page.WaitForTimeoutAsync(3000);
		var root = await session.Page.EvaluateAsync<string[]>("""
			() => {
			    const root = document.querySelector('[data-qa=cursor] .ggnet');
			    const shown = selector => { const element = document.querySelector(selector); return element ? getComputedStyle(element).visibility : 'absent'; };
			    return [root.getAttribute('data-ggnet-interaction') ?? '', String(root.hasAttribute('data-ggnet-navigation')),
			        shown('[data-qa=cursor] .x-break-label'), shown('[data-qa=cursor] .crosshair-label'), shown('[data-qa=cursor-time]')];
			}
			""");
		if (state == "scrub")
		{
			await TouchAsync(session.Cdp, "touchEnd");
			await session.Page.WaitForTimeoutAsync(500);
		}
		panel = await session.PanelAsync("cursor", 0);
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.2, panel.Y + panel.Height / 3);
		await session.WaitForCursorAsync("cursor", cursor => cursor.Pinned && Math.Abs(cursor.Fraction - 0.2) < 0.003);
		await session.Page.WaitForTimeoutAsync(400);

		// Assert

		using var _ = new AssertionScope();
		during.Should().Be(state == "scrub" ? "inspecting" : state == "unavailable" ? "unavailable" : "pending");
		root.Should().Equal(["", "false", "visible", "visible", "visible"], "the root shows no interaction, and ticks, cursor label and readout are back, before any further input");
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.2) < 0.003);
		await ExpectNoPendingAsync(session.Page);
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData("/time-cursor")]
	[InlineData("/overview-detail")]
	public async Task ARemountAfterUnavailableAcceptsGesturesAgain(string path)
	{
		// Arrange — time-cursor remounts by leaving and returning; overview-detail remounts by key on a mode change.

		var chart = path == "/time-cursor" ? "cursor" : "overview";
		await using var frames = new CircuitGate();
		await using var session = await OpenAsync(path, frames);
		var panel = await session.PanelAsync(chart, 0);
		var x = panel.X + panel.Width * 0.5;
		var y = panel.Y + panel.Height / 3;
		frames.Hold();
		await SwipeAsync(session.Page, session.Cdp, x, y, -120, 0);
		await session.Page.WaitForFunctionAsync("chart => document.querySelector(`[data-qa=${chart}] .ggnet`).dataset.ggnetInteraction === 'unavailable'", chart, new() { Timeout = 15_000 });
		frames.Release();
		await session.Page.WaitForTimeoutAsync(800);
		await SwipeAsync(session.Page, session.Cdp, x, y, -60, 0);
		await session.Page.WaitForTimeoutAsync(500);
		var quarantined = await session.Page.Locator($"[data-qa={chart}] .ggnet").GetAttributeAsync("data-ggnet-interaction");
		var changes = path == "/time-cursor" ? "view-changes" : "overview-changes";
		var before = await session.TextAsync(changes);

		// Act

		if (path == "/time-cursor")
		{
			await session.Page.Locator("nav a[href='/crosshair']").TapAsync();
			await session.Page.Locator("h1", new() { HasText = "Crosshair" }).WaitForAsync();
			await session.Page.Locator("nav a[href='/time-cursor']").TapAsync();
			await session.Page.Locator("[data-qa=empty]").WaitForAsync(new() { Timeout = 10_000 });
		}
		else
		{
			await session.Page.Locator("[data-qa=scroll-y]").TapAsync();
		}
		await session.Page.WaitForTimeoutAsync(1000);
		var remounted = await session.Page.Locator($"[data-qa={chart}] .ggnet").GetAttributeAsync("data-ggnet-interaction");
		var baseline = path == "/time-cursor" ? "0" : before;
		panel = await session.PanelAsync(chart, 0);
		await SwipeAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.5, panel.Y + panel.Height / 3, -120, 0);
		var expected = (int.Parse(baseline, System.Globalization.CultureInfo.InvariantCulture) + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
		await session.WaitForTextAsync(changes, expected);
		await session.Page.WaitForTimeoutAsync(400);

		// Assert

		using var _ = new AssertionScope();
		quarantined.Should().Be("unavailable");
		before.Should().Be("1", "the held navigation applies late, and the quarantined one is never sent");
		remounted.Should().BeNull();
		(await session.TextAsync(changes)).Should().Be(expected);
		(await session.Page.Locator($"[data-qa={chart}] .ggnet").GetAttributeAsync("data-ggnet-interaction")).Should().BeNull();
		(await session.PreviewsAsync(chart)).Should().OnlyContain(transform => transform == string.Empty);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task APendingHoldThatLeavesThePanelDoesNotInspect()
	{
		// Arrange

		await using var session = await OpenAsync();
		var panel = await session.PanelAsync("cursor", 0);
		var x = panel.X + panel.Width - 2;
		var y = panel.Y + panel.Height / 3;

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		await TouchAsync(session.Cdp, "touchMove", Point(x + 8, y));
		await session.Page.WaitForTimeoutAsync(500);
		await TouchAsync(session.Cdp, "touchEnd");
		await session.Page.WaitForTimeoutAsync(300);

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("position-changes")).Should().Be("0");
		(await session.TextAsync("view-changes")).Should().Be("0");
		await ExpectNoPendingAsync(session.Page);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task ASecondContactCarriesThePanWithoutAnIntermediateCommit()
	{
		// Arrange

		await using var session = await OpenAsync();
		var panel = await session.PanelAsync("cursor", 0);
		var x = panel.X + panel.Width * 0.5;
		var y = panel.Y + panel.Height / 3;

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		await TouchAsync(session.Cdp, "touchMove", Point(x - 80, y));
		var pan = await session.PreviewsAsync("cursor");
		await TouchAsync(session.Cdp, "touchStart", Point(x - 80, y), Point(x + 80, y, 2));
		var navigation = await session.PreviewsAsync("cursor");
		var before = await session.TextAsync("view-changes");
		await TouchAsync(session.Cdp, "touchEnd");
		await session.WaitForTextAsync("view-changes", "1");

		// Assert

		using var _ = new AssertionScope();
		pan.Should().HaveCount(4).And.OnlyContain(transform => transform.Length > 0);
		navigation.Should().Equal(pan);
		before.Should().Be("0");
		(await session.TextAsync("view-changes")).Should().Be("1");
		(await session.TextAsync("position-changes")).Should().Be("0");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task PenDragStillPansWithoutPinning()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", 0, 0.6);

		// Act

		await session.Cdp.SendAsync("Input.dispatchMouseEvent", new() { ["type"] = "mousePressed", ["x"] = x, ["y"] = y, ["button"] = "left", ["buttons"] = 1, ["clickCount"] = 1, ["pointerType"] = "pen" });
		await session.Cdp.SendAsync("Input.dispatchMouseEvent", new() { ["type"] = "mouseMoved", ["x"] = x - 80, ["y"] = y, ["buttons"] = 1, ["pointerType"] = "pen" });
		await session.Cdp.SendAsync("Input.dispatchMouseEvent", new() { ["type"] = "mouseReleased", ["x"] = x - 80, ["y"] = y, ["button"] = "left", ["buttons"] = 0, ["clickCount"] = 1, ["pointerType"] = "pen" });
		await session.WaitForTextAsync("view-changes", "1");

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("view-changes")).Should().Be("1");
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => !cursor.Pinned);
		session.Errors.Should().BeEmpty();
	}
}

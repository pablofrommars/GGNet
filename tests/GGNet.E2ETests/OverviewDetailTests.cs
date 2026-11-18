using static GGNet.E2ETests.Fixtures.TouchInput;

namespace GGNet.E2ETests;

// The overview/detail demo page in a real browser: panel activation against the
// gesture module's taps, drags and resets, and the view window handed between two plots.
public class OverviewDetailTests(DemoAppFixture fixture) : IClassFixture<DemoAppFixture>
{
	// Longer than the wait for a possible second tap, so "no activation" is a real observation.
	private const int Settled = 900;

	[Theory]
	[InlineData("circle")]
	[InlineData("line")]
	[InlineData("rect.panel")]
	public async Task WithoutInteractivityAClickOnAnyPartOfThePlotAreaActivatesAtOnce(string target)
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointOnAsync("plain", target);

		// Act

		await session.Page.Mouse.ClickAsync((float)x, (float)y);
		await session.WaitForTextAsync("plain-activations", "1");

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("plain-activations")).Should().Be("1");
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public async Task ATapOnASubPanelPinsWithoutOpeningDetail(int panel)
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PanelCentreAsync("overview", panel);

		// Act

		await TapAsync(session.Page, session.Cdp, x, y);
		await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-qa=selection-time]').textContent !== 'none'");
		await session.Page.WaitForTimeoutAsync(Settled);

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("activations")).Should().Be("0");
		(await session.TextAsync("selection-time")).Should().NotBe("none");
		(await session.TextAsync("state")).Should().Be("overview");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task ADragPanRaisesOneGestureChangeAndNoActivation()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PanelCentreAsync("overview", 0);

		// Act

		await SwipeAsync(session.Page, session.Cdp, x, y, -160, 0);
		await session.WaitForTextAsync("overview-changes", "1");
		await session.Page.WaitForTimeoutAsync(Settled);

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("overview-changes")).Should().Be("1");
		(await session.TextAsync("overview-cause")).Should().Be("Gesture");
		(await session.TextAsync("overview-window")).Should().NotBe("none");
		(await session.TextAsync("activations")).Should().Be("0");
		(await session.TextAsync("state")).Should().Be("overview");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task ADragMovesEverySubPanelBeforeRelease()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PanelCentreAsync("overview", 0);
		var previews = session.Page.Locator("[data-qa=overview] [data-ggnet-pan]");

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		for (var i = 1; i <= 8; i++)
		{
			await TouchAsync(session.Cdp, "touchMove", Point(x - i * 20, y));
			await session.Page.WaitForTimeoutAsync(30);
		}

		var during = await previews.EvaluateAllAsync<string?[]>("marks => marks.map(mark => mark.getAttribute('transform'))");
		var changesDuring = await session.TextAsync("overview-changes");
		await TouchAsync(session.Cdp, "touchEnd");
		await session.WaitForTextAsync("overview-changes", "1");
		await session.Page.WaitForTimeoutAsync(Settled);
		var after = await previews.EvaluateAllAsync<string?[]>("marks => marks.map(mark => mark.getAttribute('transform'))");

		// Assert

		using var _ = new AssertionScope();
		during.Should().HaveCount(3);
		during[0].Should().StartWith("translate(-");
		during.Should().AllBe(during[0]);
		changesDuring.Should().Be("0");
		after.Should().AllSatisfy(transform => transform.Should().BeNull());
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task MouseResetAndTheAllControlResetWithoutActivating(bool touch)
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PanelCentreAsync("overview", 0);
		await SwipeAsync(session.Page, session.Cdp, x, y, -160, 0);
		await session.WaitForTextAsync("overview-changes", "1");

		// Act

		if (touch)
		{
			await session.Page.Locator("[data-qa=all]").TapAsync();
		}
		else
		{
			await session.Page.Mouse.DblClickAsync((float)x, (float)y);
		}

		await session.WaitForTextAsync("overview-cause", touch ? "HostCommand" : "Reset");
		await session.Page.WaitForTimeoutAsync(Settled);

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("overview-cause")).Should().Be(touch ? "HostCommand" : "Reset");
		(await session.TextAsync("overview-window")).Should().Be("none");
		(await session.TextAsync("overview-changes")).Should().Be("2");
		(await session.TextAsync("activations")).Should().Be("0");
		(await session.TextAsync("state")).Should().Be("overview");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task TheWindowTravelsToTheDetailOnOpenAndBackOnClose()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PanelCentreAsync("overview", 0);
		await SwipeAsync(session.Page, session.Cdp, x, y, -160, 0);
		await session.WaitForTextAsync("overview-changes", "1");
		var panned = await session.TextAsync("overview-window");

		// Act

		await session.Page.Locator("[data-qa=expand]").TapAsync();
		await session.WaitForTextAsync("state", "detail");
		await session.WaitForTextAsync("detail-window", panned);
		var opened = await session.TextAsync("detail-window");
		var openedCause = await session.TextAsync("detail-cause");

		// The open transition rescales the layer, and a pan that starts while it runs is cancelled.
		await session.Page.WaitForTimeoutAsync(500);
		var (dx, dy) = await session.PanelCentreAsync("detail", 0);
		await SwipeAsync(session.Page, session.Cdp, dx, dy, 120, 0);
		await session.WaitForTextAsync("detail-changes", "2");
		var moved = await session.TextAsync("detail-window");

		await session.Page.Locator("[data-qa=close]").TapAsync();
		await session.WaitForTextAsync("state", "overview");
		await session.WaitForTextAsync("overview-window", moved);

		// Assert

		using var _ = new AssertionScope();
		panned.Should().NotBe("none");
		opened.Should().Be(panned);
		openedCause.Should().Be("HostCommand");
		moved.Should().NotBe(opened);
		(await session.TextAsync("detail-cause")).Should().Be("Gesture");
		(await session.TextAsync("overview-window")).Should().Be(moved);
		(await session.TextAsync("overview-cause")).Should().Be("HostCommand");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task SelectionTravelsAcrossExpandAndCloseWhileAllAndClearStaySeparate()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PanelCentreAsync("overview", 0);
		await TapAsync(session.Page, session.Cdp, x, y);
		var selected = await session.TextAsync("selection-time");
		await SwipeAsync(session.Page, session.Cdp, x, y, -100, 0);
		await session.WaitForTextAsync("overview-changes", "1");

		// Act

		await session.Page.Locator("[data-qa=expand]").TapAsync();
		await session.WaitForTextAsync("state", "detail");
		var inDetail = await session.TextAsync("selection-time");
		await session.Page.Locator("[data-qa=all]").TapAsync();
		await session.WaitForTextAsync("detail-window", "none");
		var afterAll = await session.TextAsync("selection-time");
		await session.Page.Locator("[data-qa=clear]").TapAsync();
		await session.WaitForTextAsync("selection-time", "none");
		await session.Page.Locator("[data-qa=close]").TapAsync();
		await session.WaitForTextAsync("state", "overview");

		// Assert

		using var _ = new AssertionScope();
		selected.Should().NotBe("none");
		inDetail.Should().Be(selected);
		afterAll.Should().Be(selected);
		(await session.TextAsync("selection-time")).Should().Be("none");
		(await session.TextAsync("overview-window")).Should().Be("none");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task NavigationOnlyAcceptsContactsInDifferentPanelsWithoutACrosshair()
	{
		// Arrange

		await using var session = await OpenAsync();
		await session.Page.Locator("[data-qa=navigation-only]").TapAsync();
		await session.Page.WaitForTimeoutAsync(500);
		var (ax, ay) = await session.PanelCentreAsync("overview", 0);
		var (bx, by) = await session.PanelCentreAsync("overview", 1);

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(ax, ay), Point(bx, by, 2));
		await TouchAsync(session.Cdp, "touchMove", Point(ax - 80, ay), Point(bx - 80, by, 2));
		await TouchAsync(session.Cdp, "touchEnd");
		await session.WaitForTextAsync("overview-changes", "1");

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("overview-window")).Should().NotBe("none");
		(await session.TextAsync("selection-time")).Should().Be("none");
		(await session.Page.Locator("[data-qa=overview] [data-ggnet-cursor]").CountAsync()).Should().Be(0);
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task NavigationOnlyCommitsAPanAfterClearOrWithARetainedSelection(bool retained)
	{
		// Arrange — the page shares one selection with both plots, in every mode.

		await using var session = await OpenAsync();
		var (x, y) = await session.PanelCentreAsync("overview", 0);
		if (retained)
		{
			await TapAsync(session.Page, session.Cdp, x, y);
			await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-qa=selection-time]').textContent !== 'none'");
		}
		await session.Page.Locator("[data-qa=navigation-only]").TapAsync();
		await session.Page.WaitForTimeoutAsync(500);
		if (!retained)
		{
			await session.Page.Locator("[data-qa=clear]").TapAsync();
			await session.Page.WaitForTimeoutAsync(300);
		}
		(x, y) = await session.PanelCentreAsync("overview", 0);

		// Act

		await SwipeAsync(session.Page, session.Cdp, x, y, -160, 0);
		await session.WaitForTextAsync("overview-changes", "1");
		await session.Page.WaitForTimeoutAsync(300);

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("overview-changes")).Should().Be("1");
		(await session.TextAsync("overview-cause")).Should().Be("Gesture");
		(await session.TextAsync("selection-time") != "none").Should().Be(retained);
		(await session.Page.Locator("[data-qa=overview] .ggnet").GetAttributeAsync("data-ggnet-interaction")).Should().BeNull();
		(await session.Page.Locator("[data-qa=overview] [data-ggnet-pan][transform]").CountAsync()).Should().Be(0);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task ScrollYModeAllowsNativeVerticalScrolling()
	{
		// Arrange

		await using var session = await OpenAsync();
		await session.Page.SetViewportSizeAsync(1185, 900);
		await session.Page.Locator("[data-qa=scroll-y]").TapAsync();
		await session.Page.WaitForTimeoutAsync(500);
		var (x, y) = await session.PanelCentreAsync("overview", 0);
		var before = await session.Page.EvaluateAsync<double>("() => scrollY");

		// Act

		await SwipeAsync(session.Page, session.Cdp, x, y, 0, -180);
		await session.Page.WaitForTimeoutAsync(500);

		// Assert

		using var _ = new AssertionScope();
		(await session.Page.EvaluateAsync<double>("() => scrollY")).Should().BeGreaterThan(before);
		(await session.TextAsync("selection-time")).Should().Be("none");
		(await session.TextAsync("overview-changes")).Should().Be("0");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task TheHiddenDetailReceivesNoTaps()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PanelCentreAsync("overview", 2);

		// Act

		var hit = await session.Page.EvaluateAsync<bool>(
			"([x, y]) => !!document.elementFromPoint(x, y).closest('[data-qa=overview]')", new[] { x, y });

		// Assert

		hit.Should().BeTrue("the detail overlaps the overview and must not take its input while hidden");
	}

	[Fact]
	public async Task AnInspectingTapPinsWithoutActivating()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointOnAsync("inspect", "[data-ggnet-pan] g[transform] > circle");
		var pin = session.Page.Locator("[data-qa=inspect] [data-ggnet-cursor][data-ggnet-pinned]");

		// Act

		await TapAsync(session.Page, session.Cdp, x, y);
		await pin.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
		await session.Page.WaitForTimeoutAsync(Settled);
		var afterTap = await session.TextAsync("inspect-activations");

		await session.Page.Locator("[data-qa=inspect-open]").TapAsync();
		await session.WaitForTextAsync("inspect-opens", "1");

		// Assert

		using var _ = new AssertionScope();
		afterTap.Should().Be("0");
		(await session.TextAsync("inspect-opens")).Should().Be("1");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task TheRefreshMeasurementCompletesEveryRoundForBothCases()
	{
		// Arrange

		await using var session = await OpenAsync();

		// Act

		await session.Page.Locator("[data-qa=measure]").ClickAsync();
		await session.Page.Locator("[data-qa=measure-result]").GetByText("extra").WaitForAsync(new() { Timeout = 60_000 });
		var result = await session.TextAsync("measure-result");

		// Assert

		using var _ = new AssertionScope();
		result.Should().MatchRegex(@"overview only: mean \d+\.\d ms, max \d+\.\d ms, 20/20 · with hidden detail: mean \d+\.\d ms, max \d+\.\d ms, 20/20 · extra -?\d+\.\d ms");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task NativeOverviewDoubleTapResetsWithoutExpandingOrChangingTheHeldPin()
	{
		// Arrange

		await using var session = await OpenAsync();
		await session.EnableNativeActionsAsync();
		var (x, y) = await session.PanelCentreAsync("overview", 0);
		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		await session.Page.WaitForTimeoutAsync(500);
		await TouchAsync(session.Cdp, "touchMove", Point(x + 30, y));
		await TouchAsync(session.Cdp, "touchEnd");
		await session.WaitForCommittedSelectionAsync();
		var selection = await session.TextAsync("selection-time");
		await SwipeAsync(session.Page, session.Cdp, x, y, -80, 0);
		await session.WaitForTextAsync("overview-changes", "1");
		await session.WaitForCommittedSelectionAsync();
		(x, y) = await session.PanelCentreAsync("overview", 0);

		// Act

		await NativeDoubleTapAsync(session, x, y);
		await session.WaitForTextAsync("overview-cause", "Reset");

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("overview-window")).Should().Be("none");
		(await session.TextAsync("activations")).Should().Be("0");
		(await session.TextAsync("state")).Should().Be("overview");
		(await session.TextAsync("selection-time")).Should().Be(selection);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task NativeExpandDetailPinResetAndInChartCloseRetainInspection()
	{
		// Arrange

		await using var session = await OpenAsync();
		await session.EnableNativeActionsAsync();
		var (x, y) = await session.PanelCentreAsync("overview", 0);

		// Act

		await TapAsync(session.Page, session.Cdp, x, y);
		await session.WaitForTextAsync("state", "detail");
		(x, y) = await session.PanelCentreAsync("detail", 0);
		await TapAsync(session.Page, session.Cdp, x - 40, y);
		await session.WaitForCommittedSelectionAsync();
		var selected = await session.TextAsync("selection-time");
		await SwipeAsync(session.Page, session.Cdp, x, y, -80, 0);
		await session.WaitForTextAsync("detail-cause", "Gesture");
		await session.WaitForCommittedSelectionAsync();
		(x, y) = await session.PanelCentreAsync("detail", 0);
		await NativeDoubleTapAsync(session, x + 60, y);
		await session.WaitForTextAsync("detail-cause", "Reset");
		var afterReset = await session.TextAsync("selection-time");
		await session.Page.Locator("[data-qa=close-native]").TapAsync();
		await session.WaitForTextAsync("state", "overview");

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("activations")).Should().Be("1");
		(await session.TextAsync("detail-window")).Should().Be("none");
		afterReset.Should().Be(selected);
		(await session.TextAsync("selection-time")).Should().Be(selected);
		(await session.TextAsync("state")).Should().Be("overview");
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData(0.01)]
	[InlineData(0.5)]
	[InlineData(0.99)]
	public async Task NativeCursorUsesRenderedTextBoundsAndOneLabelAcrossTheStack(double fraction)
	{
		// Arrange

		await using var session = await OpenAsync();
		await session.EnableNativeActionsAsync();
		await session.PanelCentreAsync("overview", 0);
		var box = (await session.Page.Locator("[data-qa=overview] rect.panel").First.BoundingBoxAsync())!;
		var x = box.X + box.Width * fraction;
		var y = box.Y + box.Height / 2;

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		await session.Page.WaitForTimeoutAsync(500);
		await TouchAsync(session.Cdp, "touchEnd");
		await session.Page.WaitForFunctionAsync("""
			() => {
			    const label=document.querySelector('[data-qa=overview] .crosshair-label');
			    return label && getComputedStyle(label).visibility==='visible';
			}
			""");
		var geometry = await session.Page.EvaluateAsync<double[]>("""
			() => {
			    const root=document.querySelector('[data-qa=overview]');
			    const text=root.querySelector('.crosshair-label').getBBox();
			    const lines=[...root.querySelectorAll('line.crosshair')];
			    const panels=[...root.querySelectorAll('rect.panel')].map(p=>p.getBBox());
			    return [root.querySelectorAll('.crosshair-label').length, text.x-panels[0].x,
			        panels[0].x+panels[0].width-text.x-text.width, text.y-panels[0].y,
			        Number(lines[0].getAttribute('y1'))-text.y-text.height,
			        Number(lines[1].getAttribute('y1'))-panels[1].y,
			        Number(lines[2].getAttribute('y1'))-panels[2].y,
			        Number(lines[0].getAttribute('y2'))-Number(lines[1].getAttribute('y1')),
			        (Number(lines[0].getAttribute('x1'))-panels[0].x)/panels[0].width];
			}
			""");

		// Assert

		using var _ = new AssertionScope();
		geometry[0].Should().Be(1);
		geometry[1].Should().BeGreaterThanOrEqualTo(3.5);
		geometry[2].Should().BeGreaterThanOrEqualTo(3.5);
		geometry[3].Should().BeGreaterThanOrEqualTo(3.5);
		geometry[4].Should().BeApproximately(4, 0.5);
		// getBBox rounds geometry while the SVG attributes retain serialized double precision.
		geometry[5].Should().BeApproximately(0, 0.0001);
		geometry[6].Should().BeApproximately(0, 0.0001);
		geometry[7].Should().BeApproximately(0, 0.01);
		geometry[8].Should().BeApproximately(fraction, 0.003);
		session.Errors.Should().BeEmpty();
	}

	private static async Task NativeDoubleTapAsync(Session session, double x, double y)
	{
		await session.Page.EvaluateAsync("""
			() => {
			    window.nativeTapTrace = [];
			    window.nativeTapTraceAbort = new AbortController();
			    for (const type of ['pointerdown', 'pointerup', 'pointercancel', 'blur']) {
			        window.addEventListener(type, event => {
			            const panel = event.target.closest?.('[data-ggnet-frame]');
			            window.nativeTapTrace.push({type, pointer: event.pointerType,
			                target: event.target === window ? 'window' : event.target.tagName,
			                time: performance.now(), x: event.clientX, y: event.clientY,
			                frame: panel?.dataset.ggnetFrame,
			                inspection: panel?.querySelector('[data-ggnet-inspection]')?.dataset.ggnetInspection,
			                feedback: panel?.closest('.ggnet')?.dataset.ggnetInteraction});
			        }, {capture: true, signal: window.nativeTapTraceAbort.signal});
			    }
			}
			""");
		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		await TouchAsync(session.Cdp, "touchEnd");
		await session.Page.WaitForTimeoutAsync(80);
		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		await TouchAsync(session.Cdp, "touchEnd");
		await session.Page.WaitForTimeoutAsync(600);
		var trace = await session.Page.EvaluateAsync<string>("() => { window.nativeTapTraceAbort.abort(); return JSON.stringify(window.nativeTapTrace); }");
		TestContext.Current.TestOutputHelper?.WriteLine("Native double-tap pointer trace: " + trace);
	}

	private async Task<Session> OpenAsync()
	{
		Assert.SkipUnless(fixture.Available, fixture.Reason);

		// The QA device's shape: a portrait touchscreen at device scale 1 on a desktop browser.
		var context = await fixture.Browser.NewContextAsync(new() { ViewportSize = new() { Width = 1185, Height = 1920 }, HasTouch = true });
		var page = await context.NewPageAsync();
		var session = new Session(context, page, await context.NewCDPSessionAsync(page), fixture.AttachServerOutputOnCircuitError(page));

		page.PageError += (_, error) => session.Errors.Add(error);
		page.Console += (_, message) =>
		{
			if (message.Type == "error")
			{
				session.Errors.Add(message.Text);
			}
		};

		await page.GotoAsync(fixture.BaseUrl + "/overview-detail", new() { WaitUntil = WaitUntilState.NetworkIdle });

		// Give the circuit a moment to become interactive after prerender.
		await page.WaitForTimeoutAsync(1000);

		return session;
	}

	private sealed record Session(IBrowserContext Context, IPage Page, ICDPSession Cdp, IAsyncDisposable ServerOutput) : IAsyncDisposable
	{
		public List<string> Errors { get; } = [];

		public async Task WaitForCommittedSelectionAsync()
		{
			await Page.WaitForFunctionAsync("""
				() => {
				    const host = document.querySelector('.overview-detail-demo');
				    const selection = host.querySelector('[data-qa=selection-time]');
				    const cursors = [...host.querySelectorAll('[data-ggnet-inspection]')];
				    return /^\d+$/.test(selection.innerText)
				        && !host.querySelector('[data-ggnet-interaction]')
				        && cursors.length === 5 && cursors.every(cursor =>
				            cursor.hasAttribute('data-ggnet-committed')
				            && cursor.dataset.ggnetCommitted === cursor.dataset.ggnetSelection);
				}
				""");
		}

		public async Task EnableNativeActionsAsync()
		{
			var previous = await Page.Locator("[data-qa=overview] [data-ggnet-frame], [data-qa=detail] [data-ggnet-frame]")
				.EvaluateAllAsync<string[]>("panels => panels.map(panel => panel.dataset.ggnetFrame.split(':')[0])");

			await Page.Locator("[data-qa=native-actions]").ClickAsync();
			await Page.WaitForFunctionAsync("""
				previous => {
				    const panels = [...document.querySelectorAll('[data-qa=overview] [data-ggnet-frame], [data-qa=detail] [data-ggnet-frame]')];
				    return panels.length === 5 && panels.every(panel => {
				        const stamp = panel.dataset.ggnetFrame;
				        return stamp && !previous.includes(stamp.split(':')[0])
				            && panel.querySelector('[data-ggnet-marks-frame]')?.dataset.ggnetMarksFrame === stamp;
				    });
				}
				""", previous);
		}

		public async Task<(double x, double y)> PanelCentreAsync(string chart, int panel)
		{
			var box = await BoxAsync(Page.Locator($"[data-qa={chart}] rect.panel").Nth(panel));

			return (box.X + box.Width / 2, box.Y + box.Height / 2);
		}

		// A point where the topmost element is one matching the selector, so the click lands on it.
		public async Task<(double x, double y)> PointOnAsync(string chart, string selector)
		{
			await BoxAsync(Page.Locator($"[data-qa={chart}] rect.panel").First);

			var point = await Page.EvaluateAsync<double[]>("""
				([chart, selector]) => {
				    const root = document.querySelector(`[data-qa=${chart}]`);
				    for (const element of root.querySelectorAll(selector)) {
				        const box = element.getBoundingClientRect();
				        for (const fx of [0.5, 0.25, 0.75, 0.1, 0.9]) {
				            for (const fy of [0.5, 0.25, 0.75, 0.1, 0.9]) {
				                const x = box.x + box.width * fx, y = box.y + box.height * fy;
				                if (document.elementFromPoint(x, y) === element) {
				                    return [x, y];
				                }
				            }
				        }
				    }
				    return null;
				}
				""", new[] { chart, selector });

			point.Should().NotBeNull($"some {selector} in {chart} must be clickable");

			return (point[0], point[1]);
		}

		private async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
		{
			await locator.ScrollIntoViewIfNeededAsync();
			await Page.WaitForTimeoutAsync(200);

			return (await locator.BoundingBoxAsync())!;
		}

		public Task<string> TextAsync(string name) => Page.Locator($"[data-qa={name}]").InnerTextAsync();

		// Assertions report the final state; this only gives the circuit time to reach it.
		public async Task WaitForTextAsync(string name, string text)
		{
			for (var i = 0; i < 50 && await TextAsync(name) != text; i++)
			{
				await Task.Delay(100);
			}
		}

		// The attachment is flushed first: it writes to the test's output, which closes when the test ends.
		public async ValueTask DisposeAsync()
		{
			try
			{
				await ServerOutput.DisposeAsync();
			}
			finally
			{
				await Context.DisposeAsync();
			}
		}
	}
}

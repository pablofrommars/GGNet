using static GGNet.E2ETests.Fixtures.TouchInput;

namespace GGNet.E2ETests;

// The demo's touch pages under real touch input: the gesture module, the Blazor
// circuit and the page around the chart, together.
public class TouchDemoTests(DemoAppFixture fixture) : IClassFixture<DemoAppFixture>
{
	private const string Marks = "[data-ggnet-pan] g[transform] > circle";

	[Theory]
	[InlineData(0)]
	[InlineData(6)]
	public async Task TapOnAMarkRaisesItsCallbackAndTheAncestorOnceWithoutPanning(int jitter)
	{
		// Arrange

		await using var session = await OpenAsync("/touch");
		var (x, y) = await session.MarkAsync("tap");
		var before = await session.XLabelsAsync("tap");

		// Act

		await TapAsync(session.Page, session.Cdp, x, y, jitter);
		await session.WaitForTextAsync("mark-clicks", "1");

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("mark-clicks")).Should().Be("1");
		(await session.TextAsync("ancestor-clicks")).Should().Be("1");
		(await session.XLabelsAsync("tap")).Should().Be(before);
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task PreviewStillFollowsTheFingerAfterTheHostResetsTheView(bool panFirst)
	{
		// Arrange

		await using var session = await OpenAsync("/touch");
		var (x, y) = await session.MarkAsync("tap");
		if (panFirst)
		{
			var before = await session.XLabelsAsync("tap");
			await SwipeAsync(session.Page, session.Cdp, x, y, 120, 0);
			await session.WaitForLabelsAsync("tap", before);
		}
		await session.Page.Locator("[data-qa=reset-view]").TapAsync();
		await session.Page.WaitForTimeoutAsync(1000);
		var panel = await session.PanelAsync("tap");
		x = panel.X + panel.Width * 0.5;
		y = panel.Y + panel.Height * 0.5;

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		for (var i = 1; i <= 5; i++)
		{
			await TouchAsync(session.Cdp, "touchMove", Point(x + i * 16, y));
			await session.Page.WaitForTimeoutAsync(30);
		}
		var preview = await session.Page.Locator("[data-qa=tap] [data-ggnet-pan]").GetAttributeAsync("transform");
		var count = await session.Page.Locator("[data-qa=tap] [data-ggnet-pan]").CountAsync();
		await TouchAsync(session.Cdp, "touchEnd");

		// Assert

		using var _ = new AssertionScope();
		count.Should().Be(1);
		preview.Should().StartWith("translate(", "a re-render that replaces the marks group must not strand the preview");
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(14, 0)]
	[InlineData(0, -14)]
	[InlineData(10, 10)]
	public async Task TapNearAMarkOpensItsTooltipAndPinsTheCrosshairUnderTheFinger(int dx, int dy)
	{
		// Arrange — 14 px misses an 8 px mark by a finger's width but stays within reach.

		await using var session = await OpenAsync("/touch");
		var (x, y) = await session.MarkAsync("tooltip");
		var bubble = session.Page.Locator("div.container[popover] .bubble");
		var panel = await session.PanelAsync("tooltip");
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.9, panel.Y + panel.Height * 0.1);

		// Act

		await TapAsync(session.Page, session.Cdp, x + dx, y + dy);
		await bubble.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
		await session.Page.WaitForTimeoutAsync(300);
		var text = await bubble.InnerTextAsync();
		var crosshair = await session.CrosshairXAsync("tooltip");

		// Assert

		using var _ = new AssertionScope();
		text.Should().Be(await session.MarkTextAsync("tooltip"));
		crosshair.Should().NotBeNull();
		crosshair!.Value.Should().BeApproximately(x + dx, 1.5, "the crosshair is the tapped position; only the tooltip belongs to the mark");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task TapFarFromEveryMarkClosesTheTooltipAndMovesTheCrosshairToTheFinger()
	{
		// Arrange

		await using var session = await OpenAsync("/touch");
		var (x, y) = await session.MarkAsync("tooltip");
		var popover = session.Page.Locator("div.container[popover]");
		await TapAsync(session.Page, session.Cdp, x, y);
		await popover.Locator(".bubble").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
		var panel = await session.PanelAsync("tooltip");
		var bx = panel.X + panel.Width * 0.9;

		// Act

		await TapAsync(session.Page, session.Cdp, bx, panel.Y + panel.Height * 0.1);
		await popover.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 10_000 });
		await session.Page.WaitForTimeoutAsync(300);

		// Assert

		using var _ = new AssertionScope();
		(await popover.CountAsync()).Should().Be(0);
		(await session.CrosshairXAsync("tooltip"))!.Value.Should().BeApproximately(bx, 6);
	}

	[Fact]
	public async Task SameMarkReopensAfterAPanDismissedItsTooltip()
	{
		// Arrange

		await using var session = await OpenAsync("/touch");
		var popover = session.Page.Locator("div.container[popover]");
		var (x, y) = await session.MarkAsync("tooltip");
		await TapAsync(session.Page, session.Cdp, x, y);
		await popover.Locator(".bubble").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
		var panel = await session.PanelAsync("tooltip");
		await SwipeAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.9, panel.Y + panel.Height * 0.1, -60, 0);
		await popover.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 10_000 });
		(x, y) = await session.MarkAsync("tooltip");

		// Act

		await TapAsync(session.Page, session.Cdp, x, y);
		await popover.Locator(".bubble").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });

		// Assert

		(await popover.EvaluateAsync<bool>("el => el.matches(':popover-open')")).Should().BeTrue();
	}

	[Fact]
	public async Task InspectingAnotherChartReleasesTheFirstTooltipAndKeepsItsPin()
	{
		// Arrange

		await using var session = await OpenAsync("/touch-layout");
		await session.Page.Locator("[data-qa=width-45]").TapAsync();
		await session.Page.WaitForTimeoutAsync(300);
		var (x, y) = await session.MarkAsync("responsive");
		await TapAsync(session.Page, session.Cdp, x, y);
		await session.Page.WaitForTimeoutAsync(300);
		var before = await session.CrosshairXAsync("responsive");

		// Act

		(x, y) = await session.MarkAsync("mount");
		await TapAsync(session.Page, session.Cdp, x, y);
		await session.Page.WaitForTimeoutAsync(500);

		// Assert

		using var _ = new AssertionScope();
		before.Should().NotBeNull();
		(await session.CrosshairXAsync("responsive")).Should().Be(before, "a pin ends only when it is cleared");
		(await session.Page.Locator("[data-qa=responsive] div.container[popover]").CountAsync()).Should().Be(0);
		(await session.Page.Locator("[data-qa=mount] div.container[popover]").CountAsync()).Should().Be(1);
	}

	[Fact]
	public async Task MouseHoverKeepsNativeTooltipAndCrosshairBehaviour()
	{
		// Arrange

		await using var session = await OpenAsync("/touch");
		var (x, y) = await session.MarkAsync("tooltip");
		var panel = await session.PanelAsync("tooltip");
		var bubble = session.Page.Locator("div.container[popover] .bubble");

		// Act

		await session.Page.Mouse.MoveAsync((float)x, (float)y);
		await bubble.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
		await session.Page.Mouse.MoveAsync((float)(panel.X + panel.Width * 0.9), (float)(panel.Y + panel.Height * 0.1));
		await bubble.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 10_000 });
		await session.Page.WaitForTimeoutAsync(300);

		// Assert

		(await session.CrosshairXAsync("tooltip"))!.Value.Should().BeApproximately(panel.X + panel.Width * 0.9, 6);
	}

	[Theory]
	[InlineData("tooltip", "none")]
	[InlineData("pan-x", "none")]
	[InlineData("classic", "auto")]
	public async Task InteractiveChartTextIsNotSelectableByLongPress(string chart, string expected)
	{
		// Arrange — headless Chromium does not run the long-press selection gesture
		// from DevTools touch input, so the policy itself is what can be observed.

		await using var session = await OpenAsync("/touch");
		var label = session.Page.Locator($"[data-qa={chart}] text.x-break-label").First;

		// Act

		var userSelect = await label.EvaluateAsync<string>("el => getComputedStyle(el).userSelect");

		// Assert

		userSelect.Should().Be(expected);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(16)]
	public async Task TapThatInspectsAMarkDeliversOneClickToThatMark(int offset)
	{
		// Arrange — 16 px is just outside the mark's 12 px radius at this viewport.

		await using var session = await OpenAsync("/touch-layout");
		var (x, y) = await session.MarkAsync("mount");
		await session.Page.EvaluateAsync("() => { window.ancestor=0; document.querySelector('[data-qa=mount]').addEventListener('click', () => window.ancestor++); }");

		// Act

		await TapAsync(session.Page, session.Cdp, x + offset, y);
		await session.WaitForTextAsync("mount-clicks", "1");
		await session.Page.WaitForTimeoutAsync(300);

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("mount-clicks")).Should().Be("1");
		(await session.Page.EvaluateAsync<int>("() => window.ancestor")).Should().Be(1);
		(await session.Page.Locator("[data-qa=mount] div.container[popover]").CountAsync()).Should().Be(1);
	}

	[Fact]
	public async Task WithoutInspectionANearMissStaysABackgroundClick()
	{
		// Arrange — the tap chart has pan only: no tooltip, no crosshair.

		await using var session = await OpenAsync("/touch");
		var (x, y) = await session.MarkAsync("tap");

		// Act

		await TapAsync(session.Page, session.Cdp, x + 16, y);
		await session.WaitForTextAsync("ancestor-clicks", "1");

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("ancestor-clicks")).Should().Be("1");
		(await session.TextAsync("mark-clicks")).Should().Be("0");
	}

	[Fact]
	public async Task BackgroundTapReachesOnlyTheAncestor()
	{
		// Arrange

		await using var session = await OpenAsync("/touch");
		var panel = await session.PanelAsync("tap");

		// Act — the series falls left to right, so the upper right corner holds no mark.

		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.9, panel.Y + panel.Height * 0.1);
		await session.WaitForTextAsync("ancestor-clicks", "1");

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("ancestor-clicks")).Should().Be("1");
		(await session.TextAsync("mark-clicks")).Should().Be("0");
	}

	[Fact]
	public async Task DragStartingOnAMarkPansWithoutAnyClick()
	{
		// Arrange

		await using var session = await OpenAsync("/touch");
		var (x, y) = await session.MarkAsync("tap");
		var before = await session.XLabelsAsync("tap");

		// Act

		await SwipeAsync(session.Page, session.Cdp, x, y, 120, 0);
		await session.WaitForLabelsAsync("tap", before);
		await session.Page.WaitForTimeoutAsync(500);

		// Assert

		using var _ = new AssertionScope();
		(await session.XLabelsAsync("tap")).Should().NotBe(before);
		(await session.TextAsync("mark-clicks")).Should().Be("0");
		(await session.TextAsync("ancestor-clicks")).Should().Be("0");
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData("/touch", "tooltip", null, true)]
	[InlineData("/touch-layout", "responsive", "width-45", true)]
	[InlineData("/touch-layout", "nested", null, false)]
	public async Task TapOpensTheTooltipAtTheContactAndTappingElsewhereDismissesIt(string path, string chart, string? before, bool crosshair)
	{
		// Arrange

		await using var session = await OpenAsync(path);
		if (before is not null)
		{
			await session.Page.Locator($"[data-qa={before}]").TapAsync();
			await session.Page.WaitForTimeoutAsync(300);
		}
		var (x, y) = await session.MarkAsync(chart);
		var popover = session.Page.Locator("div.container[popover]");

		// Act

		await TapAsync(session.Page, session.Cdp, x, y);
		await popover.Locator(".bubble").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
		var position = await popover.EvaluateAsync<double[]>("el => [parseFloat(el.style.left), parseFloat(el.style.top), el.matches(':popover-open') ? 1 : 0, Number(el.dataset.offset)]");
		var (ex, ey) = await session.EmptyPointAsync(chart);
		await TapAsync(session.Page, session.Cdp, ex, ey);
		await popover.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 10_000 });
		var pinned = await session.CrosshairXAsync(chart);

		// Assert

		using var _ = new AssertionScope();
		position[0].Should().BeApproximately(x + position[3], 1);
		position[1].Should().BeApproximately(y + position[3], 1);
		position[2].Should().Be(1);
		if (crosshair)
		{
			pinned!.Value.Should().BeApproximately(ex, 1.5, "a background tap pins the crosshair under it");
		}
		else
		{
			pinned.Should().BeNull();
		}
		(await popover.CountAsync()).Should().Be(0);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task NestedScrollerKeepsItsVerticalScrollAndTheChartOwnsHorizontalDrags()
	{
		// Arrange

		await using var session = await OpenAsync("/touch-layout");
		var scroller = session.Page.Locator("[data-qa=scroller]");
		var panel = await session.PanelAsync("nested");
		var x = panel.X + panel.Width * 0.5;
		var y = panel.Y + panel.Height * 0.5;
		var before = await session.XLabelsAsync("nested");

		// Act

		await SwipeAsync(session.Page, session.Cdp, x, y, 0, -100);
		var scrolled = await scroller.EvaluateAsync<double[]>("el => [el.scrollLeft, el.scrollTop]");
		var afterScroll = await session.XLabelsAsync("nested");
		panel = await session.PanelAsync("nested");
		await SwipeAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.5, panel.Y + panel.Height * 0.5, panel.Width * 0.3, 0);
		await session.WaitForLabelsAsync("nested", before);
		var panned = await scroller.EvaluateAsync<double[]>("el => [el.scrollLeft, el.scrollTop]");

		// Assert

		using var _ = new AssertionScope();
		scrolled[1].Should().BeGreaterThan(50, "a vertical swipe starting on an x-pan chart scrolls the container");
		afterScroll.Should().Be(before);
		(await session.XLabelsAsync("nested")).Should().NotBe(before);
		panned[0].Should().Be(scrolled[0], "a horizontal drag on the chart belongs to the chart");
	}

	[Fact]
	public async Task DialogChartPansShowsItsTooltipAboveTheDialogAndSurvivesReopening()
	{
		// Arrange

		await using var session = await OpenAsync("/touch-layout");
		var popover = session.Page.Locator("div.container[popover]");

		// Act

		await session.Page.Locator("[data-qa=open-dialog]").TapAsync();
		var (x, y) = await session.MarkAsync("dialog-chart");
		await TapAsync(session.Page, session.Cdp, x, y);
		await popover.Locator(".bubble").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
		var open = await popover.EvaluateAsync<bool>("el => el.matches(':popover-open')");
		var before = await session.XLabelsAsync("dialog-chart");
		await SwipeAsync(session.Page, session.Cdp, x, y, 100, 0);
		await session.WaitForLabelsAsync("dialog-chart", before);
		var panned = await session.XLabelsAsync("dialog-chart");
		await session.Page.Locator("[data-qa=close-dialog]").TapAsync();
		await session.Page.Locator("[data-qa=dialog-chart]").WaitForAsync(new() { State = WaitForSelectorState.Detached });
		await session.Page.Locator("[data-qa=open-dialog]").TapAsync();
		(x, y) = await session.MarkAsync("dialog-chart");
		await TapAsync(session.Page, session.Cdp, x, y);
		await popover.Locator(".bubble").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });

		// Assert

		using var _ = new AssertionScope();
		open.Should().BeTrue();
		panned.Should().NotBe(before);
		(await session.XLabelsAsync("dialog-chart")).Should().Be(before, "a reopened dialog mounts a fresh chart");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task VerticalSwipeInsideTheDialogScrollsTheDialogAndNotThePage()
	{
		// Arrange

		await using var session = await OpenAsync("/touch-layout");
		await session.Page.Locator("[data-qa=open-dialog]").TapAsync();
		await session.Page.Locator("[data-qa=dialog-chart] rect.panel").WaitForAsync();
		await session.Page.WaitForTimeoutAsync(500);
		var dialog = session.Page.Locator("[data-qa=dialog]");
		var box = (await dialog.BoundingBoxAsync())!;
		var page = await session.Page.EvaluateAsync<double>("() => scrollY");

		// Act

		await SwipeAsync(session.Page, session.Cdp, box.X + box.Width / 2, box.Y + box.Height - 60, 0, -300);

		// Assert

		using var _ = new AssertionScope();
		(await dialog.EvaluateAsync<double>("el => el.scrollTop")).Should().BeGreaterThan(50);
		(await session.Page.EvaluateAsync<double>("() => scrollY")).Should().Be(page);
	}

	[Fact]
	public async Task RemountingAndNavigatingAwayAndBackLeaveExactlyOneHandlerSet()
	{
		// Arrange

		await using var session = await OpenAsync("/touch-layout");
		var toggle = session.Page.Locator("[data-qa=toggle-mount]");

		// Act

		await toggle.TapAsync();
		await session.Page.Locator($"[data-qa=mount] {Marks}").First.WaitForAsync(new() { State = WaitForSelectorState.Detached });
		await toggle.TapAsync();
		var (x, y) = await session.MarkAsync("mount");
		await TapAsync(session.Page, session.Cdp, x, y);
		await session.WaitForTextAsync("mount-clicks", "1");
		var afterRemount = await session.TextAsync("mount-clicks");
		await session.Page.Locator("nav a[href='/touch']").TapAsync();
		await session.Page.Locator("[data-qa=tap]").WaitForAsync();
		await session.Page.Locator("nav a[href='/touch-layout']").TapAsync();
		await session.Page.Locator("[data-qa=mount]").WaitForAsync();
		await session.Page.WaitForTimeoutAsync(1000);
		(x, y) = await session.MarkAsync("mount");
		var before = await session.XLabelsAsync("mount");
		await TapAsync(session.Page, session.Cdp, x, y);
		await session.WaitForTextAsync("mount-clicks", "1");
		var afterReturn = await session.TextAsync("mount-clicks");
		var panel = await session.PanelAsync("mount");
		await SwipeAsync(session.Page, session.Cdp, x, y, panel.Width * 0.3, 0);
		await session.WaitForLabelsAsync("mount", before);

		// Assert

		using var _ = new AssertionScope();
		afterRemount.Should().Be("1");
		afterReturn.Should().Be("1");
		(await session.XLabelsAsync("mount")).Should().NotBe(before);
		(await session.TextAsync("mount-clicks")).Should().Be("1");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task DoubleTapRestoresTheAuthoredView()
	{
		// Arrange

		await using var session = await OpenAsync("/touch");
		var panel = await session.PanelAsync("pan-x");
		var x = panel.X + panel.Width * 0.9;
		var y = panel.Y + panel.Height * 0.1;
		var before = await session.XLabelsAsync("pan-x");
		await SwipeAsync(session.Page, session.Cdp, x, y, -120, 0);
		await session.WaitForLabelsAsync("pan-x", before);
		var panned = await session.XLabelsAsync("pan-x");

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		await TouchAsync(session.Cdp, "touchEnd");
		await session.Page.WaitForTimeoutAsync(80);
		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		await TouchAsync(session.Cdp, "touchEnd");
		await session.WaitForLabelsAsync("pan-x", panned);

		// Assert

		using var _ = new AssertionScope();
		panned.Should().NotBe(before);
		(await session.XLabelsAsync("pan-x")).Should().Be(before);
	}

	[Fact]
	public async Task PinchOnAChartScalesThePageAndLeavesTheDomainAlone()
	{
		// Arrange

		await using var session = await OpenAsync("/touch");
		var panel = await session.PanelAsync("pan-both");
		var x = panel.X + panel.Width * 0.5;
		var y = panel.Y + panel.Height * 0.5;
		var before = await session.XLabelsAsync("pan-both");

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(x - 40, y, 1), Point(x + 40, y, 2));
		for (var i = 1; i <= 10; i++)
		{
			await TouchAsync(session.Cdp, "touchMove", Point(x - 40 - i * 10, y, 1), Point(x + 40 + i * 10, y, 2));
			await session.Page.WaitForTimeoutAsync(30);
		}
		await TouchAsync(session.Cdp, "touchEnd");
		await session.Page.WaitForTimeoutAsync(500);

		// Assert

		using var _ = new AssertionScope();
		(await session.Page.EvaluateAsync<double>("() => visualViewport.scale")).Should().BeGreaterThan(1.1);
		(await session.XLabelsAsync("pan-both")).Should().Be(before);
		(await session.Page.Locator("[data-qa=pan-both] [data-ggnet-pan]").GetAttributeAsync("transform")).Should().BeNull();
	}

	[Fact]
	public async Task ContactDiagnosticsReportEveryActiveContact()
	{
		// Arrange

		await using var session = await OpenAsync("/touch");
		var contacts = session.Page.Locator("[data-contact-diagnostics] [data-contacts]");

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(100, 300, 1), Point(300, 300, 2), Point(500, 300, 3));
		await session.Page.WaitForTimeoutAsync(200);
		var during = await contacts.InnerTextAsync();
		var pointers = await session.Page.Locator("[data-contact-diagnostics] [data-pointers]").InnerTextAsync();
		await TouchAsync(session.Cdp, "touchEnd");
		await session.Page.WaitForTimeoutAsync(200);

		// Assert

		using var _ = new AssertionScope();
		during.Should().Be("3");
		pointers.Should().Contain("100,300").And.Contain("300,300").And.Contain("500,300");
		(await contacts.InnerTextAsync()).Should().Be("0");
		(await session.Page.Locator("[data-contact-diagnostics] [data-max]").InnerTextAsync()).Should().Be("3");
		(await session.Page.Locator("[data-contact-diagnostics] [data-touch-max]").InnerTextAsync()).Should().Be("3");
		(await session.Page.Locator("[data-contact-diagnostics] [data-touches]").InnerTextAsync()).Should().Be("0");
	}

	private async Task<Session> OpenAsync(string path)
	{
		Assert.SkipUnless(fixture.Available, fixture.Reason);

		// The QA device's shape: a 1200x1920 portrait touchscreen at device scale 1 on a desktop
		// browser, not a mobile one; 1185 CSS px is its measured width beside the scrollbar.
		var context = await fixture.Browser.NewContextAsync(new() { ViewportSize = new() { Width = 1185, Height = 1920 }, HasTouch = true });
		var page = await context.NewPageAsync();
		var session = new Session(context, page, await context.NewCDPSessionAsync(page));

		page.PageError += (_, error) => session.Errors.Add(error);
		page.Console += (_, message) =>
		{
			if (message.Type == "error")
			{
				session.Errors.Add(message.Text);
			}
		};

		await page.GotoAsync(fixture.BaseUrl + path, new() { WaitUntil = WaitUntilState.NetworkIdle });

		// Give the circuit a moment to become interactive after prerender.
		await page.WaitForTimeoutAsync(1000);

		return session;
	}

	private sealed record Session(IBrowserContext Context, IPage Page, ICDPSession Cdp) : IAsyncDisposable
	{
		public List<string> Errors { get; } = [];

		public async Task<(double x, double y)> MarkAsync(string chart)
		{
			var mark = Page.Locator($"[data-qa={chart}] {Marks}").Nth(4);
			await mark.ScrollIntoViewIfNeededAsync();
			await Page.WaitForTimeoutAsync(200);
			var box = await mark.BoundingBoxAsync();

			return (box!.X + box.Width / 2, box.Y + box.Height / 2);
		}

		public async Task<LocatorBoundingBoxResult> PanelAsync(string chart)
		{
			var panel = Page.Locator($"[data-qa={chart}] rect.panel").First;
			await panel.ScrollIntoViewIfNeededAsync();
			await Page.WaitForTimeoutAsync(200);

			return (await panel.BoundingBoxAsync())!;
		}

		// A visible point inside the panel that lies farthest from every mark.
		public async Task<(double x, double y)> EmptyPointAsync(string chart)
		{
			await PanelAsync(chart);
			var point = await Page.EvaluateAsync<double[]>("""
				chart => {
				    const root = document.querySelector(`[data-qa=${chart}]`);
				    const panel = root.querySelector('rect.panel').getBoundingClientRect();
				    const marks = [...root.querySelectorAll('[data-ggnet-pan] g[transform] > circle')]
				        .map(c => { const r = c.getBoundingClientRect(); return [r.x + r.width / 2, r.y + r.height / 2]; });
				    let best = null, score = -1;
				    for (let i = 1; i < 10; i++) {
				        for (let j = 1; j < 10; j++) {
				            const x = panel.x + panel.width * i / 10, y = panel.y + panel.height * j / 10;
				            if (x < 0 || y < 0 || x >= innerWidth || y >= innerHeight - 80) continue;
				            const e = document.elementFromPoint(x, y);
				            if (!e || !e.closest('[data-ggnet-viewport]') || !root.contains(e) || e.closest('[data-ggnet-pan]')) continue;
				            const d = Math.min(...marks.map(([mx, my]) => Math.hypot(mx - x, my - y)));
				            if (d > score) { score = d; best = [x, y]; }
				        }
				    }
				    return best;
				}
				""", chart);

			return (point[0], point[1]);
		}

		public async Task<double?> CrosshairXAsync(string chart)
		{
			var line = Page.Locator($"[data-qa={chart}] line.crosshair");
			if (await line.CountAsync() == 0 || !await line.EvaluateAsync<bool>("line => getComputedStyle(line).visibility === 'visible'"))
			{
				return null;
			}

			var box = await line.BoundingBoxAsync();
			return box!.X + box.Width / 2;
		}

		// The reference text is what a mouse hover over the mark's centre shows.
		public async Task<string> MarkTextAsync(string chart)
		{
			var (x, y) = await MarkAsync(chart);
			await Page.Mouse.MoveAsync((float)x, (float)y);
			var bubble = Page.Locator("div.container[popover] .bubble");
			await bubble.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
			var text = await bubble.InnerTextAsync();
			await Page.Mouse.MoveAsync(1, 1);

			return text;
		}

		public async Task<string> XLabelsAsync(string chart)
			=> string.Join("|", (await Page.Locator($"[data-qa={chart}] text.x-break-label").AllTextContentsAsync()).Select(label => label.Trim()));

		public Task<string> TextAsync(string name) => Page.Locator($"[data-qa={name}]").InnerTextAsync();

		public Task WaitForTextAsync(string name, string text)
			=> WaitAsync(async () => await TextAsync(name) == text);

		public Task WaitForLabelsAsync(string chart, string previous)
			=> WaitAsync(async () => await XLabelsAsync(chart) != previous);

		// Assertions report the final state; this only gives the circuit time to reach it.
		private static async Task WaitAsync(Func<Task<bool>> condition)
		{
			for (var i = 0; i < 50 && !await condition(); i++)
			{
				await Task.Delay(100);
			}
		}

		public ValueTask DisposeAsync() => Context.DisposeAsync();
	}
}

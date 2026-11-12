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
	[InlineData(0, "0 temperature", "detail")]
	[InlineData(1, "1 gravity", "overview")]
	[InlineData(2, "2 pressure", "overview")]
	public async Task ATapOnASubPanelReportsThatSubPanelAndOnlyTemperatureOpensItsDetail(int panel, string activation, string state)
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PanelCentreAsync("overview", panel);

		// Act

		await TapAsync(session.Page, session.Cdp, x, y);
		await session.WaitForTextAsync("activations", "1");
		await session.Page.WaitForTimeoutAsync(Settled);

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("activations")).Should().Be("1");
		(await session.TextAsync("last-activation")).Should().Be(activation);
		(await session.TextAsync("state")).Should().Be(state);
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
	public async Task TheResetGestureResetsTheWindowWithoutActivating(bool touch)
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PanelCentreAsync("overview", 0);
		await SwipeAsync(session.Page, session.Cdp, x, y, -160, 0);
		await session.WaitForTextAsync("overview-changes", "1");

		// Act

		if (touch)
		{
			await TouchAsync(session.Cdp, "touchStart", Point(x, y));
			await TouchAsync(session.Cdp, "touchEnd");
			await session.Page.WaitForTimeoutAsync(80);
			await TouchAsync(session.Cdp, "touchStart", Point(x, y));
			await TouchAsync(session.Cdp, "touchEnd");
		}
		else
		{
			await session.Page.Mouse.DblClickAsync((float)x, (float)y);
		}

		await session.WaitForTextAsync("overview-cause", "Reset");
		await session.Page.WaitForTimeoutAsync(Settled);

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("overview-cause")).Should().Be("Reset");
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

		await TapAsync(session.Page, session.Cdp, x, y);
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
	public async Task AnInspectingTapShowsItsTooltipAndNeverActivates()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointOnAsync("inspect", "[data-ggnet-pan] g[transform] > circle");
		var bubble = session.Page.Locator("div.container[popover] .bubble");

		// Act

		await TapAsync(session.Page, session.Cdp, x, y);
		await bubble.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
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

	private async Task<Session> OpenAsync()
	{
		Assert.SkipUnless(fixture.Available, fixture.Reason);

		// The QA device's shape: a portrait touchscreen at device scale 1 on a desktop browser.
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

		await page.GotoAsync(fixture.BaseUrl + "/overview-detail", new() { WaitUntil = WaitUntilState.NetworkIdle });

		// Give the circuit a moment to become interactive after prerender.
		await page.WaitForTimeoutAsync(1000);

		return session;
	}

	private sealed record Session(IBrowserContext Context, IPage Page, ICDPSession Cdp) : IAsyncDisposable
	{
		public List<string> Errors { get; } = [];

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

		public ValueTask DisposeAsync() => Context.DisposeAsync();
	}
}

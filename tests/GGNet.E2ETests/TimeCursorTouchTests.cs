using static GGNet.E2ETests.Fixtures.TouchInput;

namespace GGNet.E2ETests;

// The crosshair under real touch input: one finger taps to pin and drags
// sideways to scrub, the page keeps its vertical scroll, and a tap stays
// inspection input all the way.
public partial class TimeCursorTests
{
	[Theory]
	[InlineData(0, 0)]
	[InlineData(2, 6)]
	[InlineData(3, -6)]
	public async Task ATapPinsTheTappedTimeInEveryPanel(int origin, int jitter)
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", origin, 0.45);

		// Act

		await TapAsync(session.Page, session.Cdp, x, y, jitter);
		await session.WaitForTextAsync("pinned", "pinned");
		var cursors = await session.CursorsAsync("cursor");

		// Assert

		using var _ = new AssertionScope();
		cursors.Should().HaveCount(4).And.OnlyContain(cursor => cursor.Visible && cursor.Pinned);
		cursors.Should().OnlyContain(cursor => Math.Abs(cursor.Fraction - 0.45) < 0.01, "a tap that drifts a few pixels pins where the finger lifts");
		cursors.Select(cursor => Math.Round(cursor.Fraction, 3)).Distinct().Should().ContainSingle();
		(await session.TextAsync("position-changes")).Should().Be("1");
		(await session.TextAsync("view-changes")).Should().Be("0");
		(await session.RowsAsync()).Should().HaveCount(6);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task AHorizontalDragScrubsEveryPanelAndPinsWhereItEnds()
	{
		// Arrange

		await using var session = await OpenAsync();
		var panel = await session.PanelAsync("cursor", 1);
		var x = panel.X + panel.Width * 0.25;
		var y = panel.Y + panel.Height / 3;
		var travel = panel.Width * 0.4;
		var scroll = await session.ScrollAsync();

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		for (var i = 1; i <= 10; i++)
		{
			await TouchAsync(session.Cdp, "touchMove", Point(x + i * travel / 10, y + i));
			await session.Page.WaitForTimeoutAsync(30);
		}
		var during = await session.CursorsAsync("cursor");
		await session.WaitForTextAsync("pinned", "following");
		var following = await session.TextAsync("pinned");
		await TouchAsync(session.Cdp, "touchEnd");
		await session.WaitForTextAsync("pinned", "pinned");
		await session.Page.WaitForTimeoutAsync(500);
		var after = await session.CursorsAsync("cursor");

		// Assert

		using var _ = new AssertionScope();
		during.Should().OnlyContain(cursor => cursor.Visible && Math.Abs(cursor.Fraction - 0.65) < 0.002, "the line is under the finger before release");
		following.Should().Be("following");
		after.Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.65) < 0.002);
		(await session.TextAsync("view-changes")).Should().Be("0", "a scrub never pans");
		(await session.ScrollAsync()).Should().Be(scroll);
		(await session.TextAsync("ancestor-clicks")).Should().Be("0");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task ATapRightAfterAScrubPinsWhereItLands()
	{
		// Arrange

		await using var session = await OpenAsync();
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		var x = panel.X + panel.Width * 0.2;

		// Act — the tap follows the scrub at once, well inside half a second.

		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		for (var i = 1; i <= 5; i++)
		{
			await TouchAsync(session.Cdp, "touchMove", Point(x + i * 24, y));
		}
		await TouchAsync(session.Cdp, "touchEnd");
		await TouchAsync(session.Cdp, "touchStart", Point(panel.X + panel.Width * 0.8, y));
		await TouchAsync(session.Cdp, "touchEnd");
		await session.Page.WaitForTimeoutAsync(1200);

		// Assert

		using var _ = new AssertionScope();
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.8) < 0.003);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task AVerticalDragScrollsThePageAndSelectsNothing()
	{
		// Arrange

		await using var session = await OpenAsync();
		await session.Page.SetViewportSizeAsync(1185, 900);
		var (x, y) = await session.PointAsync("cursor", 0, 0.5);
		var scroll = await session.ScrollAsync();

		// Act

		await SwipeAsync(session.Page, session.Cdp, x, y, 6, -220);
		await session.Page.WaitForTimeoutAsync(500);

		// Assert

		using var _ = new AssertionScope();
		(await session.ScrollAsync()).Should().BeGreaterThan(scroll + 100);
		(await session.TextAsync("position-changes")).Should().Be("0");
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => !cursor.Visible);
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ASecondFingerAbandonsTheScrubAndRestoresWhatWasPinned(bool pinnedBefore)
	{
		// Arrange

		await using var session = await OpenAsync();
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		var time = string.Empty;
		if (pinnedBefore)
		{
			await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.2, y);
			await session.WaitForTextAsync("pinned", "pinned");
			time = await session.TextAsync("cursor-time");
		}
		var x = panel.X + panel.Width * 0.5;

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		for (var i = 1; i <= 6; i++)
		{
			await TouchAsync(session.Cdp, "touchMove", Point(x + i * 20, y));
			await session.Page.WaitForTimeoutAsync(30);
		}
		await session.WaitForTextAsync("pinned", "following");
		var scrubbing = await session.TextAsync("pinned");
		await TouchAsync(session.Cdp, "touchStart", Point(x + 120, y), Point(x - 150, y + 60, 2));
		await session.Page.WaitForTimeoutAsync(600);
		var abandoned = (time: await session.TextAsync("cursor-time"), state: await session.TextAsync("pinned"));
		await TouchAsync(session.Cdp, "touchEnd", Point(x + 120, y));
		await session.Page.WaitForTimeoutAsync(500);
		await TouchAsync(session.Cdp, "touchEnd");
		await session.Page.WaitForTimeoutAsync(700);
		var cursors = await session.CursorsAsync("cursor");

		// Assert

		using var _ = new AssertionScope();
		scrubbing.Should().Be("following");
		abandoned.Should().Be(pinnedBefore ? (time, "pinned") : (string.Empty, string.Empty));
		(await session.TextAsync("cursor-time")).Should().Be(time, "lifting one finger of two starts nothing");
		if (pinnedBefore)
		{
			cursors.Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.2) < 0.002);
		}
		else
		{
			cursors.Should().OnlyContain(cursor => !cursor.Visible);
		}
		(await session.TextAsync("view-changes")).Should().Be("0");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task ATapOnAMarkPinsWithoutReachingTheMarkOrAnAncestor()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.MarkAsync("cursor", 1, 6);

		// Act

		await TapAsync(session.Page, session.Cdp, x, y);
		await session.WaitForTextAsync("pinned", "pinned");
		await session.Page.WaitForTimeoutAsync(500);
		var touch = (mark: await session.TextAsync("mark-clicks"), ancestor: await session.TextAsync("ancestor-clicks"), state: await session.TextAsync("pinned"));
		await session.Page.Mouse.ClickAsync((float)x, (float)y);
		await session.WaitForTextAsync("mark-clicks", "1");

		// Assert

		using var _ = new AssertionScope();
		touch.Should().Be(("0", "0", "pinned"));
		(await session.TextAsync("mark-clicks")).Should().Be("1", "a mouse click keeps reaching the mark");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task ADoubleTapSelectsTwiceAndDoesNotResetTheView()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", 0, 0.6);
		await session.Page.Mouse.MoveAsync((float)x, (float)y);
		await session.Page.Mouse.DownAsync();
		await session.Page.Mouse.MoveAsync((float)x - 150, (float)y, new() { Steps = 10 });
		await session.Page.Mouse.UpAsync();
		await session.WaitForTextAsync("view-changes", "1");
		await session.Page.Mouse.MoveAsync(2, 2);
		await session.Page.WaitForTimeoutAsync(500);
		var window = await session.TextAsync("window");

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		await TouchAsync(session.Cdp, "touchEnd");
		await session.Page.WaitForTimeoutAsync(80);
		await TouchAsync(session.Cdp, "touchStart", Point(x, y));
		await TouchAsync(session.Cdp, "touchEnd");
		await session.WaitForTextAsync("pinned", "pinned");
		await session.Page.WaitForTimeoutAsync(900);

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("view-changes")).Should().Be("1");
		(await session.TextAsync("window")).Should().Be(window);
		(await session.TextAsync("pinned")).Should().Be("pinned");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task WithoutPanThePlotAreaStillAllowsPagePinch()
	{
		// Arrange

		await using var session = await OpenAsync();

		// Act

		var plain = await session.TouchActionAsync("plain");
		var free = await session.Page.Locator("[data-qa=free] svg[data-ggnet-viewport]").CountAsync();

		// Assert

		using var _ = new AssertionScope();
		plain.Should().Be("pan-y pinch-zoom");
		free.Should().Be(0, "a panel with its own x scale keeps the browser's default touch behaviour");
	}
}

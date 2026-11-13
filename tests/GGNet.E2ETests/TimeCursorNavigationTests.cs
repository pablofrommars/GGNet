using static GGNet.E2ETests.Fixtures.TouchInput;

namespace GGNet.E2ETests;

// Two fingers under the crosshair: they pan and pinch the time window of a plot
// with Pan, previewed in every panel and committed once, while the pin keeps
// its time.
public partial class TimeCursorTests
{
	[Fact]
	public async Task TwoFingersPanEveryPanelBeforeReleaseAndCommitOnce()
	{
		// Arrange

		await using var session = await OpenAsync();
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;
		await TapAsync(session.Page, session.Cdp, panel.X + panel.Width * 0.5, y);
		await session.WaitForTextAsync("pinned", "pinned");
		var time = await session.TextAsync("cursor-time");
		var changes = await session.TextAsync("position-changes");
		var ax = panel.X + panel.Width * 0.45;
		var bx = panel.X + panel.Width * 0.60;
		var travel = panel.Width * 0.2;

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(ax, y), Point(bx, y + 50, 2));
		for (var i = 1; i <= 10; i++)
		{
			await TouchAsync(session.Cdp, "touchMove", Point(ax - i * travel / 10, y), Point(bx - i * travel / 10, y + 50, 2));
			await session.Page.WaitForTimeoutAsync(30);
		}
		var previews = await session.PreviewsAsync("cursor");
		var during = await session.CursorsAsync("cursor");
		var before = await session.TextAsync("view-changes");
		await TouchAsync(session.Cdp, "touchEnd", Point(bx - travel, y + 50, 2));
		await session.WaitForTextAsync("view-changes", "1");
		await session.Page.WaitForTimeoutAsync(600);
		await TouchAsync(session.Cdp, "touchEnd");
		await session.Page.WaitForTimeoutAsync(700);
		var after = await session.CursorsAsync("cursor");

		// Assert

		using var _ = new AssertionScope();
		before.Should().Be("0", "nothing is committed while the fingers are down");
		previews.Should().HaveCount(4).And.OnlyContain(preview => preview.StartsWith("translate(-", StringComparison.Ordinal));
		previews.Select(Session.Scale).Should().OnlyContain(scale => Math.Abs(scale - 1) < 0.001);
		during.Should().OnlyContain(cursor => cursor.Visible && Math.Abs(cursor.Fraction - 0.3) < 0.003, "the pinned line rides with the marks");
		(await session.TextAsync("view-changes")).Should().Be("1");
		(await session.TextAsync("window")).Should().NotBe("—");
		(await session.PreviewsAsync("cursor")).Should().OnlyContain(preview => preview == string.Empty);
		after.Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.3) < 0.003);
		(await session.TextAsync("cursor-time")).Should().Be(time, "the pin is a time, not a place on the screen");
		(await session.TextAsync("position-changes")).Should().Be(changes, "lifting the fingers one after the other selects nothing");
		session.Errors.Should().BeEmpty();
	}

	[Theory]
	[InlineData(2.0)]
	[InlineData(0.8)]
	public async Task APinchZoomsTheTimeWindowAboutTheFingers(double spread)
	{
		// Arrange

		await using var session = await OpenAsync();
		var panel = await session.PanelAsync("cursor", 1);
		var y = panel.Y + panel.Height / 3;
		var middle = panel.X + panel.Width * 0.5;
		await TapAsync(session.Page, session.Cdp, middle, y);
		await session.WaitForTextAsync("pinned", "pinned");
		var time = await session.TextAsync("cursor-time");
		var start = panel.Width * 0.1;
		var end = start * spread;

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(middle - start, y), Point(middle + start, y, 2));
		for (var i = 1; i <= 10; i++)
		{
			var half = start + (end - start) * i / 10;
			await TouchAsync(session.Cdp, "touchMove", Point(middle - half, y), Point(middle + half, y, 2));
			await session.Page.WaitForTimeoutAsync(30);
		}
		var previews = await session.PreviewsAsync("cursor");
		await TouchAsync(session.Cdp, "touchEnd");
		await session.WaitForTextAsync("view-changes", "1");
		await session.Page.WaitForTimeoutAsync(700);
		var minutes = await session.WindowMinutesAsync();
		var cursors = await session.CursorsAsync("cursor");

		// Assert

		using var _ = new AssertionScope();
		previews.Select(Session.Scale).Should().HaveCount(4).And.OnlyContain(scale => Math.Abs(scale - spread) < 0.001);
		minutes.Should().BeApproximately(24 * 60 / spread, 24 * 60 / spread * 0.06, "the window narrows or widens by the spread of the fingers");
		cursors.Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.5) < 0.004, "the time under the fingers stays under them");
		(await session.TextAsync("cursor-time")).Should().Be(time);
		(await session.TextAsync("view-changes")).Should().Be("1");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task TwoFingersThatDoNotMoveCommitNothing()
	{
		// Arrange

		await using var session = await OpenAsync();
		var panel = await session.PanelAsync("cursor", 0);
		var y = panel.Y + panel.Height / 3;

		// Act

		await TouchAsync(session.Cdp, "touchStart", Point(panel.X + panel.Width * 0.4, y), Point(panel.X + panel.Width * 0.6, y, 2));
		await session.Page.WaitForTimeoutAsync(200);
		await TouchAsync(session.Cdp, "touchEnd");
		await session.Page.WaitForTimeoutAsync(900);

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("view-changes")).Should().Be("0");
		(await session.TextAsync("position-changes")).Should().Be("0");
		(await session.PreviewsAsync("cursor")).Should().OnlyContain(preview => preview == string.Empty);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task WithPanThePlotAreaTakesTwoFingerGesturesFromThePage()
	{
		// Arrange

		await using var session = await OpenAsync();

		// Act

		var navigating = await session.TouchActionAsync("cursor");

		// Assert

		navigating.Should().Be("pan-y");
	}
}

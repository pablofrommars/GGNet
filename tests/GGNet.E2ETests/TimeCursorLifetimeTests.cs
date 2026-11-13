using static GGNet.E2ETests.Fixtures.TouchInput;

namespace GGNet.E2ETests;

// What happens around a pin on the demo page: data arriving under it, the
// window leaving it behind, the context being replaced, the page being left.
public partial class TimeCursorTests
{
	[Fact]
	public async Task AppendedReadingsChangeTheReadoutUnderAPinThatDoesNotMove()
	{
		// Arrange — pinned at the very end of the data, where the next samples will be nearer.

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", 0, 0.999);
		await TapAsync(session.Page, session.Cdp, x, y);
		await session.WaitForTextAsync("pinned", "pinned");
		await session.Page.WaitForTimeoutAsync(300);
		var before = (time: await session.TextAsync("cursor-time"), positions: await session.TextAsync("position-changes"),
			readouts: int.Parse(await session.TextAsync("readout-changes"), System.Globalization.CultureInfo.InvariantCulture),
			temperature: (await session.RowsAsync()).Single(row => row.Name == "Temperature"), fraction: (await session.CursorsAsync("cursor"))[0].Fraction);

		// Act

		await session.Page.Locator("[data-qa=append]").TapAsync();
		await session.WaitForTextAsync("readout-changes", (before.readouts + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
		await session.Page.WaitForTimeoutAsync(500);
		var temperature = (await session.RowsAsync()).Single(row => row.Name == "Temperature");
		var cursors = await session.CursorsAsync("cursor");

		// Assert

		using var _ = new AssertionScope();
		(await session.TextAsync("cursor-time")).Should().Be(before.time);
		(await session.TextAsync("position-changes")).Should().Be(before.positions, "only the readings changed");
		(await session.TextAsync("readout-changes")).Should().Be((before.readouts + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
		(temperature.Value, temperature.Note).Should().NotBe((before.temperature.Value, before.temperature.Note));
		temperature.Kind.Should().Be("Observed");
		cursors.Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && cursor.Fraction < before.fraction - 0.005, "the axis grew and the pin stayed on its time");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task APinLeftOutsideTheWindowKeepsItsReadoutAndReturnsWithTheWindow()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", 0, 0.9);
		await session.Page.Mouse.ClickAsync((float)x, (float)y);
		await session.WaitForTextAsync("pinned", "pinned");
		var time = await session.TextAsync("cursor-time");
		var rows = await session.RowsAsync();
		var panel = await session.PanelAsync("cursor", 0);
		var (dx, dy) = await session.PointAsync("cursor", 0, 0.2);

		// Act

		await session.Page.Mouse.MoveAsync((float)dx, (float)dy);
		await session.Page.Mouse.DownAsync();
		await session.Page.Mouse.MoveAsync((float)(dx + panel.Width * 0.6), (float)dy, new() { Steps = 12 });
		await session.Page.Mouse.UpAsync();
		await session.Page.Locator("[data-qa=outside]").WaitForAsync(new() { Timeout = 10_000 });
		await session.Page.WaitForTimeoutAsync(400);
		var outside = (cursors: await session.CursorsAsync("cursor"), time: await session.TextAsync("cursor-time"), rows: await session.RowsAsync());
		await session.Page.Locator("[data-qa=reset]").ClickAsync();
		await session.Page.Locator("[data-qa=outside]").WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 10_000 });
		await session.Page.WaitForTimeoutAsync(400);
		var back = await session.CursorsAsync("cursor");

		// Assert

		using var _ = new AssertionScope();
		outside.cursors.Should().OnlyContain(cursor => !cursor.Visible, "the line is hidden, never clamped to the edge");
		outside.time.Should().Be(time);
		outside.rows.Select(row => (row.Name, row.Value)).Should().Equal(rows.Select(row => (row.Name, row.Value)));
		back.Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.9) < 0.003);
		(await session.TextAsync("cursor-time")).Should().Be(time);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task ResizingAndReplacingTheContextKeepThePin()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", 2, 0.35);
		await TapAsync(session.Page, session.Cdp, x, y);
		await session.WaitForTextAsync("pinned", "pinned");
		var time = await session.TextAsync("cursor-time");
		var positions = await session.TextAsync("position-changes");

		// Act

		await session.Page.SetViewportSizeAsync(820, 1400);
		await session.Page.WaitForTimeoutAsync(500);
		var resized = await session.CursorsAsync("cursor");
		await session.Page.Locator("[data-qa=replace]").TapAsync();
		await session.Page.WaitForTimeoutAsync(1200);
		var replaced = await session.CursorsAsync("cursor");

		// Assert

		using var _ = new AssertionScope();
		resized.Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.35) < 0.003);
		replaced.Should().HaveCount(4).And.OnlyContain(cursor => cursor.Visible && cursor.Pinned && Math.Abs(cursor.Fraction - 0.35) < 0.003);
		(await session.TextAsync("cursor-time")).Should().Be(time);
		(await session.TextAsync("position-changes")).Should().Be(positions);
		(await session.RowsAsync()).Should().HaveCount(6);
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task OnlyClearEndsAPinAndAReturningPageStartsCleanWithOneSetOfHandlers()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", 0, 0.5);
		await TapAsync(session.Page, session.Cdp, x, y);
		await session.WaitForTextAsync("pinned", "pinned");

		// Act

		await session.Page.Mouse.WheelAsync(0, 400);
		await session.Page.Locator("h1").First.TapAsync();
		await session.Page.WaitForTimeoutAsync(500);
		var afterElsewhere = await session.TextAsync("pinned");
		await session.Page.Locator("[data-qa=clear]").TapAsync();
		await session.Page.Locator("[data-qa=empty]").WaitForAsync(new() { Timeout = 10_000 });
		var cleared = await session.CursorsAsync("cursor");
		(x, y) = await session.PointAsync("cursor", 0, 0.5);
		await TapAsync(session.Page, session.Cdp, x, y);
		await session.WaitForTextAsync("pinned", "pinned");
		await session.Page.Locator("nav a[href='/crosshair']").TapAsync();
		await session.Page.Locator("h1", new() { HasText = "Crosshair" }).WaitForAsync();
		await session.Page.Locator("nav a[href='/time-cursor']").TapAsync();
		await session.Page.Locator("[data-qa=empty]").WaitForAsync(new() { Timeout = 10_000 });
		await session.Page.WaitForTimeoutAsync(1000);
		var returned = await session.CursorsAsync("cursor");
		var (mx, my) = await session.MarkAsync("cursor", 1, 6);
		await session.Page.Mouse.ClickAsync((float)mx, (float)my);
		await session.WaitForTextAsync("pinned", "pinned");
		await session.WaitForTextAsync("mark-clicks", "1");
		await session.Page.WaitForTimeoutAsync(500);

		// Assert

		using var _ = new AssertionScope();
		afterElsewhere.Should().Be("pinned", "scrolling and tapping elsewhere never clear");
		cleared.Should().OnlyContain(cursor => !cursor.Visible);
		returned.Should().HaveCount(4).And.OnlyContain(cursor => !cursor.Visible, "a page that is opened again starts with nothing inspected");
		(await session.TextAsync("mark-clicks")).Should().Be("1");
		(await session.TextAsync("ancestor-clicks")).Should().Be("1");
		(await session.TextAsync("position-changes")).Should().Be("2", "one hover and one pin, each reported once");
		session.Errors.Should().BeEmpty();
	}

	[Fact]
	public async Task ABarSeriesReadsTheIntervalThatHoldsTheTimeAndIsMissingBetweenBars()
	{
		// Arrange — the cooling valve is open while temperature is high, about five hours in, and closed about fifteen hours in.

		await using var session = await OpenAsync();
		var (ox, oy) = await session.PointAsync("cursor", 3, 4.9 / 24);
		var (cx, cy) = await session.PointAsync("cursor", 3, 14.6 / 24);

		// Act

		await TapAsync(session.Page, session.Cdp, ox, oy);
		await session.WaitForTextAsync("pinned", "pinned");
		var open = (await session.RowsAsync()).Single(row => row.Name == "Cooling");
		var time = await session.TextAsync("cursor-time");
		await TapAsync(session.Page, session.Cdp, cx, cy);
		for (var i = 0; i < 50 && await session.TextAsync("cursor-time") == time; i++)
		{
			await session.Page.WaitForTimeoutAsync(100);
		}
		var closed = (await session.RowsAsync()).Single(row => row.Name == "Cooling");

		// Assert

		using var _ = new AssertionScope();
		open.Should().BeEquivalentTo(new { Kind = "Containing", Value = "on" });
		open.Note.Should().StartWith("interval around ");
		closed.Should().BeEquivalentTo(new { Kind = "Missing", Value = "off", Note = "" });
		session.Errors.Should().BeEmpty();
	}
}

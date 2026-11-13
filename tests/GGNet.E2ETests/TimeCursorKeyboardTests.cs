namespace GGNet.E2ETests;

public partial class TimeCursorTests
{
	[Fact]
	public async Task AfterAClickTheArrowsStepThroughSampleTimesAndEscapeClears()
	{
		// Arrange

		await using var session = await OpenAsync();
		var (x, y) = await session.PointAsync("cursor", 0, 0.5);
		await session.Page.Mouse.ClickAsync((float)x, (float)y);
		await session.WaitForTextAsync("pinned", "pinned");
		await session.Page.Mouse.MoveAsync(2, 2);
		var start = await session.TextAsync("cursor-time");
		var changes = int.Parse(await session.TextAsync("position-changes"), System.Globalization.CultureInfo.InvariantCulture);

		// Act

		await session.Page.Keyboard.PressAsync("ArrowRight");
		await session.WaitForTextAsync("position-changes", (changes + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
		var forward = (time: await session.TextAsync("cursor-time"), rows: await session.RowsAsync(), cursors: await session.CursorsAsync("cursor"));
		await session.Page.Keyboard.PressAsync("ArrowLeft");
		await session.Page.Keyboard.PressAsync("ArrowLeft");
		await session.WaitForTextAsync("position-changes", (changes + 3).ToString(System.Globalization.CultureInfo.InvariantCulture));
		var back = await session.TextAsync("cursor-time");
		await session.Page.Keyboard.PressAsync("Escape");
		await session.Page.Locator("[data-qa=empty]").WaitForAsync(new() { Timeout = 10_000 });

		// Assert

		using var _ = new AssertionScope();
		forward.time.Should().NotBe(start);
		forward.rows.Should().Contain(row => row.Note == "at the cursor", "a step lands exactly on a sample of some series");
		forward.cursors.Should().OnlyContain(cursor => cursor.Visible && cursor.Pinned && cursor.Fraction > 0.5);
		string.CompareOrdinal(back, start).Should().BeLessThan(0, "two steps back pass the starting time");
		(await session.CursorsAsync("cursor")).Should().OnlyContain(cursor => !cursor.Visible);
		session.Errors.Should().BeEmpty();
	}
}

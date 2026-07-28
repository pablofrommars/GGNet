using GGNet.Scales;

namespace GGNet.Headless.Tests;

// DayMonth's trailing loop used to run to an inclusive `end` while every other loop in the
// method treats it as exclusive: on a full-range axis that reads one past the buffer, and on a
// windowed one it labels a category outside the requested window.
public class DiscreteDatesTests
{
	private sealed record Sample(LocalDate Date, double Value);

	private static readonly LocalDate[] monthBoundary =
	[
		new(2024, 1, 30),
		new(2024, 1, 31),
		new(2024, 2, 1),
		new(2024, 2, 2)
	];

	[Fact]
	public void CommitAcrossAMonthBoundary()
	{
		// Arrange

		var sut = new DiscreteDates();

		foreach (var date in monthBoundary)
		{
			sut.Train(date);
		}

		// Act

		Action act = () => sut.Commit(grid: true);

		// Assert

		using var _ = new AssertionScope();

		act.Should().NotThrow();
		sut.Breaks.Should().OnlyContain(b => b >= 0 && b < monthBoundary.Length);
	}

	[Fact]
	public async Task RenderAcrossAMonthBoundary()
	{
		// Arrange

		var source = monthBoundary.Select((date, i) => new Sample(date, i)).ToArray();

		var plot = PlotContext.Build(source, i => i.Date, i => i.Value)
			.Geom_Point()
			.Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		svg.Should().Contain("<svg");
		XDocument.Parse(svg).Should().NotBeNull();
	}

	[Theory]
	[InlineData(2024, 12, 30)] // year boundary
	[InlineData(2024, 2, 27)]  // leap-February boundary
	public void CommitAcrossABoundaryFromStart(int year, int month, int day)
	{
		// Arrange

		var start = new LocalDate(year, month, day);
		var sut = new DiscreteDates();

		for (var i = 0; i < 6; i++)
		{
			sut.Train(start.PlusDays(i));
		}

		// Act

		Action act = () => sut.Commit(grid: true);

		// Assert

		using var _ = new AssertionScope();

		act.Should().NotThrow();
		sut.Breaks.Should().OnlyContain(b => b >= 0 && b < 6);
	}

	[Fact]
	public void WindowedCommitEmitsNoTickOutsideTheWindow()
	{
		// Arrange

		// Jan 30 … Feb 3 windowed to Jan 30 … Feb 2 leaves index 4 (Feb 3) inside the buffer
		// but outside the window — the trailing loop must not reach it.
		var sut = new DiscreteDates(limits: (new LocalDate(2024, 1, 30), new LocalDate(2024, 2, 2)));

		for (var i = 0; i < 5; i++)
		{
			sut.Train(new LocalDate(2024, 1, 30).PlusDays(i));
		}

		// Act

		sut.Commit(grid: true);

		// Assert

		using var _ = new AssertionScope();

		sut.Breaks.Should().NotContain(4.0);
		sut.Breaks.Should().OnlyContain(b => b >= 0 && b <= 3);
	}
}

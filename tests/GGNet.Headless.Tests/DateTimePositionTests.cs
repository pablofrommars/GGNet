using GGNet.Scales;

namespace GGNet.Headless.Tests;

// Minute sampling used to be interpolated during Train, forward of the running maximum: a
// same-day key that arrived earlier than the current maximum inserted nothing, so Map returned
// NaN and the observation was silently dropped. Sampling is now derived at Commit.
public class DateTimePositionTests
{
	private static readonly LocalDateTime[] sameDay =
	[
		new(2026, 7, 1, 9, 0),
		new(2026, 7, 1, 9, 30),
		new(2026, 7, 1, 10, 0)
	];

	private static DateTimePosition Trained(params LocalDateTime[] keys)
	{
		var sut = new DateTimePosition();

		foreach (var key in keys)
		{
			sut.Train(key);
		}

		sut.Commit(grid: true);

		return sut;
	}

	[Fact]
	public void MapBeforeCommitUsesTheSampledGrid()
	{
		// Arrange

		// PlotContext.Render shapes before it commits, so geoms call Map while the scale has
		// only ever been trained. Deriving the minute grid at Commit left the marks on
		// observed-only indices (0, 1, 2) under an axis committed over 0..60.
		var sut = new DateTimePosition();

		foreach (var key in sameDay)
		{
			sut.Train(key);
		}

		// Act

		var mapped = sameDay.Select(sut.Map).ToArray();

		// Assert

		mapped.Should().Equal(0.0, 30.0, 60.0);
	}

	[Fact]
	public void TrainingAfterAReadResamples()
	{
		// Arrange

		var sut = new DateTimePosition();
		sut.Train(sameDay[0]);
		sut.Train(sameDay[1]);

		// Act

		// The first read finalizes the grid; a later Train must invalidate it.
		var before = sut.Map(sameDay[1]);
		sut.Train(sameDay[2]);
		var after = sut.Map(sameDay[2]);

		// Assert

		using var _ = new AssertionScope();

		before.Should().Be(30.0);
		after.Should().Be(60.0);
	}

	[Fact]
	public void OutOfOrderSameDayKeysAreRetained()
	{
		// Arrange

		// 10:00 trained before 09:00 — the order that used to lose the earlier key.
		var sut = Trained(sameDay[2], sameDay[0], sameDay[1]);

		// Act

		var mapped = sameDay.Select(sut.Map).ToArray();

		// Assert

		using var _ = new AssertionScope();

		mapped.Should().NotContain(double.NaN);
		mapped.Should().BeInAscendingOrder();
	}

	[Fact]
	public void PermutingTrainOrderYieldsIdenticalMappings()
	{
		// Arrange

		var ordered = Trained(sameDay[0], sameDay[1], sameDay[2]);
		var reversed = Trained(sameDay[2], sameDay[1], sameDay[0]);
		var shuffled = Trained(sameDay[1], sameDay[2], sameDay[0]);

		// Act

		var a = sameDay.Select(ordered.Map).ToArray();
		var b = sameDay.Select(reversed.Map).ToArray();
		var c = sameDay.Select(shuffled.Map).ToArray();

		// Assert

		using var _ = new AssertionScope();

		b.Should().Equal(a);
		c.Should().Equal(a);
	}

	[Fact]
	public void InOrderSamplingIsUnchanged()
	{
		// Arrange

		// 09:00 → 10:00 inclusive at one-minute sampling is 61 positions, so the last key
		// maps to index 60 — the same dense grid the old Train-time interpolation produced.
		var sut = Trained(sameDay[0], sameDay[1], sameDay[2]);

		// Act

		var mapped = sameDay.Select(sut.Map).ToArray();

		// Assert

		mapped.Should().Equal(0.0, 30.0, 60.0);
	}

	[Fact]
	public void EachDayIsSampledFromItsOwnExtent()
	{
		// Arrange

		var day1 = new LocalDateTime(2026, 7, 1, 23, 0);
		var day2 = new LocalDateTime(2026, 7, 2, 1, 0);

		// Cross-day keys out of order too.
		var sut = Trained(day2.PlusMinutes(30), day1, day2, day1.PlusMinutes(15));

		// Act

		var mapped = new[] { day1, day1.PlusMinutes(15), day2, day2.PlusMinutes(30) }.Select(sut.Map).ToArray();

		// Assert

		using var _ = new AssertionScope();

		mapped.Should().NotContain(double.NaN);

		// 23:00–23:15 is 16 positions; day two starts straight after and is sampled over its
		// own 01:00–01:30 extent rather than across the night.
		mapped.Should().Equal(0.0, 15.0, 16.0, 46.0);
	}

	[Fact]
	public void KeysOffTheMinuteGridAreRetained()
	{
		// Arrange

		var at = new LocalDateTime(2026, 7, 1, 9, 0, 30);
		var later = new LocalDateTime(2026, 7, 1, 9, 2, 45);

		var sut = Trained(at, later);

		// Act

		var mapped = new[] { at, later }.Select(sut.Map).ToArray();

		// Assert

		mapped.Should().NotContain(double.NaN);
	}

	private sealed record Reading(LocalDateTime At, double Value);

	[Fact]
	public async Task OutOfOrderSourceRendersEveryPoint()
	{
		// Arrange

		var ordered = new Reading[]
		{
			new(sameDay[0], 1.0), new(sameDay[1], 2.0), new(sameDay[2], 3.0)
		};

		var outOfOrder = new Reading[]
		{
			new(sameDay[2], 3.0), new(sameDay[0], 1.0), new(sameDay[1], 2.0)
		};

		// Act

		var a = await Render(ordered);
		var b = await Render(outOfOrder);

		// Assert

		using var _ = new AssertionScope();

		// Marks (and their transform wrappers) are emitted in source order, so the two
		// documents are line permutations of each other: same three positions, same axis,
		// breaks and labels. Nothing is dropped and no coordinate moves.
		var marks = MarkXs(a);
		var (panelX, panelWidth) = Panel(a);

		marks.Should().HaveCount(3);
		MarkXs(b).Should().Equal(marks);
		Lines(b).Should().Equal(Lines(a));

		// 09:00 and 10:00 are the trained extent, so the marks span the panel bar the
		// expansion. On observed-only indices they were crushed into its first 5%.
		(marks[^1] - marks[0]).Should().BeGreaterThan(0.8 * panelWidth);
		marks[0].Should().BeGreaterThan(panelX);
	}

	private static async Task<string> Render(Reading[] source)
	{
		var svg = await PlotContext.Build(source, i => i.At, i => i.Value)
			.Geom_Point()
			.Style()
			.AsStringAsync();

		return Regex.Replace(svg, @"gg(?!net-)[A-Za-z0-9_-]+", "ggID");
	}

	// Circles carry cx="0" cy="0"; the position is on the wrapping <g transform="translate(x, y)">.
	private static double[] MarkXs(string svg)
		=> [.. Regex.Matches(svg, @"<g transform=""translate\(([-\d.eE]+), ([-\d.eE]+)\)"">\s*<circle")
			.Select(m => double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
			.Order()];

	private static (double x, double width) Panel(string svg)
	{
		var panel = XDocument.Parse(svg).Descendants()
			.First(element => element.Name.LocalName == "rect" && element.Attribute("class")?.Value == "panel");

		return (double.Parse(panel.Attribute("x")!.Value, CultureInfo.InvariantCulture),
			double.Parse(panel.Attribute("width")!.Value, CultureInfo.InvariantCulture));
	}

	private static string[] Lines(string svg)
		=> [.. svg.Split('\n').Select(line => line.Trim()).Order(StringComparer.Ordinal)];
}

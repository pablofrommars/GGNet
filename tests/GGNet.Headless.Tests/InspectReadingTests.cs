using GGNet.Readings;

namespace GGNet.Headless.Tests;

public class InspectReadingTests
{
	public sealed record Row(double X, double Y, string Series = "A");

	public sealed record Sample(Instant At, double Value);

	private static readonly Row[] uneven = [new(0, 10), new(10, 11), new(11, 12), new(30, 13)];

	private static string Text(Row row) => row.Y.ToString("0", CultureInfo.InvariantCulture);

	private static Inspect<Row> Inspecting(InspectPolicy policy, double? maxDistance = null)
		=> new("Series", Text, policy) { MaxDistance = maxDistance };

	private static IReadOnlyList<ISeriesIndex> Series<T, TX>(PlotContext<T, TX, double> context)
		where TX : struct
	{
		context.Style();
		context.Render();

		return [.. context.Panels.SelectMany(panel => panel.Geoms).SelectMany(geom => geom.InspectSeries())];
	}

	private static SeriesReading Read(InspectPolicy policy, double cursor, double? maxDistance = null)
		=> Series(PlotContext.Build(uneven, row => row.X, row => row.Y).Geom_Line(inspect: Inspecting(policy, maxDistance)))[0].Read(cursor);

	[Theory]
	[InlineData(10.0, 10.0, "11")]
	[InlineData(4.0, 0.0, "10")]
	[InlineData(6.0, 10.0, "11")]
	[InlineData(5.0, 0.0, "10")]
	[InlineData(-100.0, 0.0, "10")]
	[InlineData(100.0, 30.0, "13")]
	public void NearestTakesTheClosestSampleOnEitherSide(double cursor, double sample, string value)
	{
		// Arrange

		// Act

		var reading = Read(InspectPolicy.Nearest, cursor);

		// Assert

		reading.Should().BeEquivalentTo(new { Kind = ReadingKind.Observed, Sample = (double?)sample, Value = value });
	}

	[Theory]
	[InlineData(20.0, 9.0, true)]
	[InlineData(20.5, 9.5, true)]
	[InlineData(20.0, 8.9, false)]
	[InlineData(40.0, 9.9, false)]
	public void NearestRefusesASampleBeyondItsMaximumDistance(double cursor, double maxDistance, bool found)
	{
		// Arrange

		// Act

		var reading = Read(InspectPolicy.Nearest, cursor, maxDistance);

		// Assert

		using var _ = new AssertionScope();

		reading.Kind.Should().Be(found ? ReadingKind.Observed : ReadingKind.Missing);
		reading.Value.Should().Be(found ? reading.Sample == 11 ? "12" : "13" : "No nearby reading");
		reading.Sample.HasValue.Should().Be(found);
	}

	[Theory]
	[InlineData(10.0, 10.0, "11")]
	[InlineData(10.9, 10.0, "11")]
	[InlineData(29.9, 11.0, "12")]
	[InlineData(1000.0, 30.0, "13")]
	public void PreviousNeverLooksAhead(double cursor, double sample, string value)
	{
		// Arrange

		// Act

		var reading = Read(InspectPolicy.Previous, cursor);

		// Assert

		reading.Should().BeEquivalentTo(new { Kind = ReadingKind.Previous, Sample = (double?)sample, Value = value });
	}

	[Theory]
	[InlineData(-0.1, null)]
	[InlineData(16.0, 5.0)]
	[InlineData(16.1, 5.0)]
	public void PreviousIsMissingBeforeTheFirstSampleOrPastItsMaximumAge(double cursor, double? maxAge)
	{
		// Arrange

		// Act

		var reading = Read(InspectPolicy.Previous, cursor, maxAge);

		// Assert

		reading.Kind.Should().Be(cursor == 16.0 ? ReadingKind.Previous : ReadingKind.Missing);
	}

	[Theory]
	[InlineData(-0.1, ReadingKind.Missing, "No nearby reading")]
	[InlineData(0.0, ReadingKind.Step, "10")]
	[InlineData(29.0, ReadingKind.Step, "12")]
	[InlineData(1e9, ReadingKind.Step, "13")]
	public void StepStaysInEffectHoweverOldAndIgnoresTheMaximumDistance(double cursor, ReadingKind kind, string value)
	{
		// Arrange

		// Act

		var reading = Read(InspectPolicy.Step, cursor, maxDistance: 1);

		// Assert

		reading.Should().BeEquivalentTo(new { Kind = kind, Value = value });
	}

	[Fact]
	public void EachSeriesReportsItsOwnSampleAtOneCursor()
	{
		// Arrange

		Row[] sparse = [new(2, 100), new(22, 200)];
		var series = Series(PlotContext.Build(uneven, row => row.X, row => row.Y)
			.Geom_Line(inspect: new("Dense", Text))
			.Geom_Point(sparse, row => row.X, row => row.Y, inspect: new("Sparse", Text, InspectPolicy.Previous)));

		// Act

		var readings = series.Select(index => index.Read(12)).ToArray();

		// Assert

		readings.Select(reading => (reading.Name, reading.Sample, reading.Value))
			.Should().Equal(("Dense", 11.0, "12"), ("Sparse", 2.0, "100"));
	}

	[Fact]
	public void APenLiftIsNotAReading()
	{
		// Arrange

		Row[] gapped = [new(0, 1), new(1, double.NaN), new(2, 3)];
		var series = Series(PlotContext.Build(gapped, row => row.X, row => row.Y)
			.Geom_Line(piecewise: true, inspect: Inspecting(InspectPolicy.Previous)));

		// Act

		var reading = series[0].Read(1.5);

		// Assert

		reading.Should().BeEquivalentTo(new { Sample = (double?)0.0, Value = "1" });
	}

	[Fact]
	public void TheMissingTextIsTheSeriesOwn()
	{
		// Arrange

		var series = Series(PlotContext.Build(uneven, row => row.X, row => row.Y)
			.Geom_Area(inspect: Inspecting(InspectPolicy.Nearest, 1) with { Missing = "—" }));

		// Act

		var reading = series[0].Read(20);

		// Assert

		reading.Should().BeEquivalentTo(new { Kind = ReadingKind.Missing, Value = "—", Sample = (double?)null });
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void AGroupedLayerYieldsOneSeriesPerGroup(bool renamed)
	{
		// Arrange

		Row[] grouped = [new(0, 1, "A"), new(0, 5, "B"), new(10, 2, "A"), new(4, 6, "B")];
		var inspect = Inspecting(InspectPolicy.Nearest) with { GroupName = renamed ? label => "Tank " + label : null };
		var series = Series(PlotContext.Build(grouped, row => row.X, row => row.Y)
			.Scale_Color_Discrete(row => row.Series, ["#23d0fc", "#fc9d23"])
			.Geom_Line(inspect: inspect));

		// Act

		var readings = series.Select(index => index.Read(6)).ToArray();

		// Assert

		readings.Select(reading => (reading.Series, reading.Group, reading.Name, reading.Sample, reading.Value))
			.Should().Equal(
				("Series", "A", renamed ? "Tank A" : "A", 10.0, "2"),
				("Series", "B", renamed ? "Tank B" : "B", 4.0, "6"));
	}

	[Theory]
	[InlineData(10.0, 10.0)]
	[InlineData(12.4, 10.0)]
	[InlineData(7.6, 10.0)]
	[InlineData(12.6, null)]
	[InlineData(15.0, null)]
	[InlineData(17.6, 20.0)]
	[InlineData(-2.6, null)]
	[InlineData(42.6, null)]
	public void ContainingReadsTheBarWhoseExtentHoldsTheCursor(double cursor, double? bar)
	{
		// Arrange — bars every 10 units at half width: each spans its x ± 2.5.

		Row[] bars = [new(0, 1), new(10, 2), new(20, 3), new(40, 4)];
		var series = Series(PlotContext.Build(bars, row => row.X, row => row.Y)
			.Geom_Bar(width: 0.5, inspect: Inspecting(InspectPolicy.Containing)));

		// Act

		var reading = series[0].Read(cursor);

		// Assert

		using var _ = new AssertionScope();

		reading.Kind.Should().Be(bar is null ? ReadingKind.Missing : ReadingKind.Containing);
		reading.Sample.Should().Be(bar);
	}

	[Theory]
	[InlineData(10.0, 10.0)]
	[InlineData(6.0, 10.0)]
	[InlineData(15.0, 10.0)]
	[InlineData(17.0, 10.0)]
	[InlineData(5.0, null)]
	[InlineData(20.0, null)]
	[InlineData(60.0, 100.0)]
	[InlineData(170.0, 100.0)]
	public void OnALogAxisContainingFollowsTheBarAsItIsDrawn(double cursor, double? bar)
	{
		// Arrange — decades at half width: each bar is drawn from x / 10^0.25 to x * 10^0.25, about 5.6 to 17.8 around 10.

		Row[] bars = [new(1, 1), new(10, 2), new(100, 3)];
		var series = Series(PlotContext.Build(bars, row => row.X, row => row.Y)
			.Scale_X_Log10()
			.Geom_Bar(width: 0.5, inspect: Inspecting(InspectPolicy.Containing)));

		// Act

		var reading = series[0].Read(cursor);

		// Assert

		using var _ = new AssertionScope();

		reading.Kind.Should().Be(bar is null ? ReadingKind.Missing : ReadingKind.Containing);
		reading.Sample.Should().Be(bar);
	}

	[Fact]
	public void AGroupWithNoValidSampleStillReadsAsMissing()
	{
		// Arrange

		Row[] grouped = [new(0, 1, "A"), new(10, 2, "A"), new(0, double.NaN, "B"), new(10, double.NaN, "B")];
		var series = Series(PlotContext.Build(grouped, row => row.X, row => row.Y)
			.Scale_Color_Discrete(row => row.Series, ["#23d0fc", "#fc9d23"])
			.Geom_Line(inspect: Inspecting(InspectPolicy.Nearest)));

		// Act

		var readings = series.Select(index => index.Read(4)).ToArray();

		// Assert

		readings.Select(reading => (reading.Name, reading.Kind, reading.Value))
			.Should().Equal(("A", ReadingKind.Observed, "1"), ("B", ReadingKind.Missing, "No nearby reading"));
	}

	[Fact]
	public void ABarSeriesAlsoReadsLikeASampledOne()
	{
		// Arrange

		Row[] bars = [new(0, 1), new(10, 0), new(20, 1)];
		var series = Series(PlotContext.Build(bars, row => row.X, row => row.Y)
			.Geom_Bar(width: 0.5, inspect: Inspecting(InspectPolicy.Step)));

		// Act

		var reading = series[0].Read(15);

		// Assert

		reading.Should().BeEquivalentTo(new { Kind = ReadingKind.Step, Sample = (double?)10.0, Value = "0" });
	}

	[Fact]
	public void InstantDistancesAreDurations()
	{
		// Arrange

		var start = Instant.FromUtc(2026, 11, 10, 14, 0);
		Sample[] samples = [new(start, 18.5), new(start + Duration.FromMinutes(10), 18.7)];
		var inspect = new Inspect<Sample>("Temperature", sample => sample.Value.ToString("0.0", CultureInfo.InvariantCulture))
		{
			MaxDuration = Duration.FromMinutes(2)
		};
		var series = Series(PlotContext.Build(samples, sample => sample.At, sample => sample.Value).Scale_X_Instant().Geom_Line(inspect: inspect));
		var near = InspectAxis.Coordinate(start + Duration.FromMinutes(8) + Duration.FromSeconds(1));
		var far = InspectAxis.Coordinate(start + Duration.FromMinutes(5));

		// Act

		var found = InspectAxis.Reading<Instant>(series[0].Read(near), near);
		var missing = InspectAxis.Reading<Instant>(series[0].Read(far), far);

		// Assert

		using var _ = new AssertionScope();

		found.Should().Be(new Reading<Instant>("Temperature", null, "Temperature", start + Duration.FromMinutes(8) + Duration.FromSeconds(1), start + Duration.FromMinutes(10), ReadingKind.Observed, "18.7"));
		missing.Should().Be(new Reading<Instant>("Temperature", null, "Temperature", start + Duration.FromMinutes(5), null, ReadingKind.Missing, "No nearby reading"));
	}

	[Fact]
	public void TheLimitMustFitTheAxis()
	{
		// Arrange

		Sample[] samples = [new(Instant.FromUtc(2026, 11, 10, 14, 0), 1)];
		var onInstant = PlotContext.Build(samples, sample => sample.At, sample => sample.Value).Scale_X_Instant()
			.Geom_Line(inspect: new("T", sample => "x") { MaxDistance = 5 });
		var onNumber = PlotContext.Build(uneven, row => row.X, row => row.Y)
			.Geom_Line(inspect: new("T", Text) { MaxDuration = Duration.FromMinutes(5) });
		var negative = PlotContext.Build(uneven, row => row.X, row => row.Y)
			.Geom_Line(inspect: new("T", Text) { MaxDistance = -1 });

		// Act

		Action instant = () => Series(onInstant);
		Action number = () => Series(onNumber);
		Action below = () => Series(negative);

		// Assert

		using var _ = new AssertionScope();

		instant.Should().Throw<Exceptions.GGNetUserException>().WithMessage("*MaxDuration, not MaxDistance*");
		number.Should().Throw<Exceptions.GGNetUserException>().WithMessage("*MaxDistance, not MaxDuration*");
		below.Should().Throw<Exceptions.GGNetUserException>().WithMessage("*negative*");
	}

	[Fact]
	public void TheIndexFollowsTheDataOnTheNextRender()
	{
		// Arrange

		List<Row> live = [new(0, 1), new(10, 2)];
		var context = PlotContext.Build(live, row => row.X, row => row.Y).Geom_Line(inspect: Inspecting(InspectPolicy.Previous));
		var before = Series(context)[0].Read(25);

		// Act

		live.Add(new(20, 3));
		context.Render();
		var after = context.Panels[0].Geoms[0].InspectSeries()[0].Read(25);

		// Assert

		(before.Value, after.Value).Should().Be(("2", "3"));
	}
}

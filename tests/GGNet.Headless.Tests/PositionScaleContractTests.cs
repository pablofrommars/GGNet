using GGNet.Scales;

namespace GGNet.Headless.Tests;

// PlotContext.Render calls Shape() — and therefore every geom's Map — before CommitPositions().
// A position scale that derives mapping state at Commit therefore places its marks on one set of
// coordinates and its axis on another. DateTimePosition did exactly that once; this pins the
// invariant for the whole family so the next scale to derive state cannot repeat it.
public class PositionScaleContractTests
{
	private static void MapIsStableAcrossCommit<TKey>(Position<TKey> sut, params TKey[] keys)
		where TKey : struct
	{
		// Arrange

		foreach (var key in keys)
		{
			sut.Train(key);
		}

		// Act

		var beforeCommit = keys.Select(sut.Map).ToArray();

		sut.Commit(grid: true);

		var afterCommit = keys.Select(sut.Map).ToArray();

		// Assert

		using var _ = new AssertionScope();

		beforeCommit.Should().NotContain(double.NaN);
		afterCommit.Should().Equal(beforeCommit);
	}

	[Fact]
	public void ExtendedMapsIdenticallyBeforeAndAfterCommit()
		=> MapIsStableAcrossCommit(new Extended(), 1.0, 5.0, 12.5);

	[Fact]
	public void Log10MapsIdenticallyBeforeAndAfterCommit()
		=> MapIsStableAcrossCommit(new Log10(), 1.0, 100.0, 1000.0);

	[Fact]
	public void DiscretePositionMapsIdenticallyBeforeAndAfterCommit()
		=> MapIsStableAcrossCommit(new DiscretePosition<double>(), 3.0, 1.0, 2.0);

	[Fact]
	public void DiscreteDatesMapsIdenticallyBeforeAndAfterCommit()
		=> MapIsStableAcrossCommit(
			new DiscreteDates(),
			new LocalDate(2026, 1, 30),
			new LocalDate(2026, 1, 31),
			new LocalDate(2026, 2, 1));

	[Fact]
	public void DateTimePositionMapsIdenticallyBeforeAndAfterCommit()
		=> MapIsStableAcrossCommit(
			new DateTimePosition(),
			new LocalDateTime(2026, 7, 1, 9, 0),
			new LocalDateTime(2026, 7, 1, 9, 30),
			new LocalDateTime(2026, 7, 1, 10, 0));

	[Fact]
	public void InstantPositionMapsIdenticallyBeforeAndAfterCommit()
		=> MapIsStableAcrossCommit(
			new InstantPosition(null, null),
			Instant.FromUtc(2026, 7, 1, 9, 0),
			Instant.FromUtc(2026, 7, 1, 12, 30));
}

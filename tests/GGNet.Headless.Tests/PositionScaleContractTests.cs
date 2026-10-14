using GGNet.Scales;

namespace GGNet.Headless.Tests;

// PlotContext.Render calls Shape() — and therefore every geom's Map — before CommitPositions().
// A position scale that derives mapping state at Commit therefore places its marks on one set of
// coordinates and its axis on another. DateTimePosition did exactly that once; this pins the
// invariant for the whole family so the next scale to derive state cannot repeat it.
public class PositionScaleContractTests
{
	private static Position<TKey> Trained<TKey>(Position<TKey> sut, TKey[] keys)
		where TKey : struct
	{
		foreach (var key in keys)
		{
			sut.Train(key);
		}

		return sut;
	}

	private static (double[] beforeCommit, double[] afterCommit) MapAcrossCommit<TKey>(Position<TKey> sut, TKey[] keys)
		where TKey : struct
	{
		var beforeCommit = keys.Select(sut.Map).ToArray();

		sut.Commit(grid: true);

		var afterCommit = keys.Select(sut.Map).ToArray();

		return (beforeCommit, afterCommit);
	}

	[Fact]
	public void ExtendedMapsIdenticallyBeforeAndAfterCommit()
	{
		// Arrange

		double[] keys = [1.0, 5.0, 12.5];
		var sut = Trained(new Extended(), keys);

		// Act

		var (beforeCommit, afterCommit) = MapAcrossCommit(sut, keys);

		// Assert

		using var _ = new AssertionScope();

		beforeCommit.Should().NotContain(double.NaN);
		afterCommit.Should().Equal(beforeCommit);
	}

	[Fact]
	public void Log10MapsIdenticallyBeforeAndAfterCommit()
	{
		// Arrange

		double[] keys = [1.0, 100.0, 1000.0];
		var sut = Trained(new Log10(), keys);

		// Act

		var (beforeCommit, afterCommit) = MapAcrossCommit(sut, keys);

		// Assert

		using var _ = new AssertionScope();

		beforeCommit.Should().NotContain(double.NaN);
		afterCommit.Should().Equal(beforeCommit);
	}

	[Fact]
	public void DiscretePositionMapsIdenticallyBeforeAndAfterCommit()
	{
		// Arrange

		double[] keys = [3.0, 1.0, 2.0];
		var sut = Trained(new DiscretePosition<double>(), keys);

		// Act

		var (beforeCommit, afterCommit) = MapAcrossCommit(sut, keys);

		// Assert

		using var _ = new AssertionScope();

		beforeCommit.Should().NotContain(double.NaN);
		afterCommit.Should().Equal(beforeCommit);
	}

	[Fact]
	public void DiscreteDatesMapsIdenticallyBeforeAndAfterCommit()
	{
		// Arrange

		LocalDate[] keys = [
			new LocalDate(2026, 1, 30),
			new LocalDate(2026, 1, 31),
			new LocalDate(2026, 2, 1),
		];
		var sut = Trained(new DiscreteDates(), keys);

		// Act

		var (beforeCommit, afterCommit) = MapAcrossCommit(sut, keys);

		// Assert

		using var _ = new AssertionScope();

		beforeCommit.Should().NotContain(double.NaN);
		afterCommit.Should().Equal(beforeCommit);
	}

	[Fact]
	public void DateTimePositionMapsIdenticallyBeforeAndAfterCommit()
	{
		// Arrange

		LocalDateTime[] keys = [
			new LocalDateTime(2026, 7, 1, 9, 0),
			new LocalDateTime(2026, 7, 1, 9, 30),
			new LocalDateTime(2026, 7, 1, 10, 0),
		];
		var sut = Trained(new DateTimePosition(), keys);

		// Act

		var (beforeCommit, afterCommit) = MapAcrossCommit(sut, keys);

		// Assert

		using var _ = new AssertionScope();

		beforeCommit.Should().NotContain(double.NaN);
		afterCommit.Should().Equal(beforeCommit);
	}

	[Fact]
	public void InstantPositionMapsIdenticallyBeforeAndAfterCommit()
	{
		// Arrange

		Instant[] keys = [
			Instant.FromUtc(2026, 7, 1, 9, 0),
			Instant.FromUtc(2026, 7, 1, 12, 30),
		];
		var sut = Trained(new InstantPosition(null, null), keys);

		// Act

		var (beforeCommit, afterCommit) = MapAcrossCommit(sut, keys);

		// Assert

		using var _ = new AssertionScope();

		beforeCommit.Should().NotContain(double.NaN);
		afterCommit.Should().Equal(beforeCommit);
	}
}

using GGNet.Scales;

namespace GGNet.Headless.Tests;

// The typed readout seam (implementation-blocks Block 6): Unmap is the typed
// inverse of Map — exact for continuous scales, snapped to the nearest trained
// value for discrete ones — and Label formats through the scale's own
// formatter, invariantly by default.
public class ScaleReadoutTests
{
	[Fact]
	public void ExtendedUnmapInvertsMap()
	{
		// Arrange

		var sut = new Extended();

		// Act / Assert

		sut.Unmap(sut.Map(4.2)).Should().Be(4.2);
	}

	[Fact]
	public void Log10UnmapRecoversTheDataValue()
	{
		// Arrange

		var sut = new Log10();

		// Act / Assert

		sut.Unmap(sut.Map(100.0)).Should().NotBeNull()
			.And.Subject!.Value.Should().BeApproximately(100.0, 1e-9);
	}

	[Theory]
	[InlineData(0.4, 10)]
	[InlineData(0.6, 20)]
	[InlineData(2.0, 30)]
	public void DiscreteUnmapSnapsToTheNearestCategory(double value, int expected)
	{
		// Arrange

		var sut = new DiscretePosition<int>();
		sut.Train(10);
		sut.Train(20);
		sut.Train(30);

		// Act / Assert

		sut.Unmap(value).Should().Be(expected);
	}

	[Theory]
	[InlineData(-1.0)]
	[InlineData(5.0)]
	public void DiscreteUnmapOutsideTrainedValuesIsNull(double value)
	{
		// Arrange

		var sut = new DiscretePosition<int>();
		sut.Train(10);

		// Act / Assert

		sut.Unmap(value).Should().BeNull();
	}

	[Fact]
	public void InstantUnmapRoundTrips()
	{
		// Arrange

		var sut = new InstantPosition(null, null);
		var instant = NodaTime.Instant.FromUtc(2026, 7, 1, 12, 30);

		// Act / Assert

		sut.Unmap(sut.Map(instant)).Should().Be(instant);
	}

	[Fact]
	public void LabelFormatsInvariantlyUnderCommaDecimalCulture()
	{
		// Arrange

		var current = CultureInfo.CurrentCulture;

		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("sv-SE");

			var sut = new Extended();

			// Act / Assert

			sut.Label(2.5).Should().Be("2.5");
		}
		finally
		{
			CultureInfo.CurrentCulture = current;
		}
	}
}

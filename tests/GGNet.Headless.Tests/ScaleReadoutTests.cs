using GGNet.Scales;

namespace GGNet.Headless.Tests;

// The typed readout seam: Unmap is the typed
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

		// Act

		var unmapped = sut.Unmap(sut.Map(4.2));

		// Assert

		unmapped.Should().Be(4.2);
	}

	[Fact]
	public void Log10UnmapRecoversTheDataValue()
	{
		// Arrange

		var sut = new Log10();

		// Act

		var unmapped = sut.Unmap(sut.Map(100.0));

		// Assert

		unmapped.Should().NotBeNull()
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

		// Act

		var unmapped = sut.Unmap(value);

		// Assert

		unmapped.Should().Be(expected);
	}

	[Theory]
	[InlineData(-1.0)]
	[InlineData(5.0)]
	public void DiscreteUnmapOutsideTrainedValuesIsNull(double value)
	{
		// Arrange

		var sut = new DiscretePosition<int>();
		sut.Train(10);

		// Act

		var unmapped = sut.Unmap(value);

		// Assert

		unmapped.Should().BeNull();
	}

	[Fact]
	public void InstantUnmapRoundTrips()
	{
		// Arrange

		var sut = new InstantPosition(null, null);
		var instant = NodaTime.Instant.FromUtc(2026, 7, 1, 12, 30);

		// Act

		var unmapped = sut.Unmap(sut.Map(instant));

		// Assert

		unmapped.Should().Be(instant);
	}

	[Fact]
	public void LabelFormatsInvariantlyUnderCommaDecimalCulture()
	{
		// Arrange

		var current = CultureInfo.CurrentCulture;
		var sut = new Extended();
		string? label;

		// Act

		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("sv-SE");

			label = sut.Label(2.5);
		}
		finally
		{
			CultureInfo.CurrentCulture = current;
		}

		// Assert

		label.Should().Be("2.5");
	}
}

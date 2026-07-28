using GGNet.Geoms.Boxplot;

namespace GGNet.Headless.Tests;

// Boxplot samples used to live in a SortedBuffer, whose set-like Add dropped equal values:
// [1, 1, 1, 10] collapsed to [1, 10] before the percentiles were computed.
public class BoxplotTests
{
	private static SampleBuffer<double> Samples(params double[] values)
	{
		var buffer = new SampleBuffer<double>();

		foreach (var value in values)
		{
			buffer.Add(value);
		}

		return buffer;
	}

	// Linear interpolation over the four retained samples (R type 7):
	// n = (Count - 1) * p + 1 indexes [1, 1, 1, 10].
	[Theory]
	[InlineData(0.1, 1.0)]
	[InlineData(0.25, 1.0)]
	[InlineData(0.5, 1.0)]
	[InlineData(0.75, 3.25)]
	[InlineData(0.9, 7.3)]
	public void PercentilesPreserveMultiplicity(double p, double expected)
	{
		// Arrange

		var samples = Samples(1.0, 10.0, 1.0, 1.0);

		// Act

		var percentile = Boxplot<double, double, double>.Percentile(samples, p);

		// Assert

		percentile.Should().BeApproximately(expected, 1e-9);
	}

	[Fact]
	public void PercentilesOverDistinctSamplesAreUnchanged()
	{
		// Arrange

		var samples = Samples(1.0, 2.0, 3.0, 4.0);

		// Act

		var median = Boxplot<double, double, double>.Percentile(samples, 0.5);

		// Assert

		median.Should().BeApproximately(2.5, 1e-9);
	}

	private sealed record Observation(double Group, double Value);

	[Fact]
	public async Task DuplicateObservationsChangeTheRenderedBox()
	{
		// Arrange

		// The two sources share the same distinct values and the same axis extent, so under
		// deduplicating sample storage they render the identical box.
		var withDuplicates = new Observation[]
		{
			new(1, 1.0), new(1, 1.0), new(1, 1.0), new(1, 10.0)
		};

		var deduplicated = new Observation[]
		{
			new(1, 1.0), new(1, 10.0)
		};

		// Act

		var duplicates = await Render(withDuplicates);
		var distinct = await Render(deduplicated);
		var again = await Render(withDuplicates);

		// Assert

		using var _ = new AssertionScope();

		// Plot ids are per-render, so they are scrubbed the way the goldens are; this
		// control proves the comparison below sees geometry and not the id.
		again.Should().Be(duplicates);
		distinct.Should().NotBe(duplicates);
	}

	private static async Task<string> Render(Observation[] source)
	{
		var svg = await PlotContext.Build(source, i => i.Value, i => i.Group)
			.Geom_Boxplot()
			.Style()
			.AsStringAsync();

		return Regex.Replace(svg, @"gg(?!net-)[A-Za-z0-9_-]+", "ggID");
	}
}

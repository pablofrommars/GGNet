using GGNet.Scales.Common;

namespace GGNet.Headless.Tests;

public class BreaksTest
{
	[Fact]
	public void WilkinsonExtendedNiceRange()
	{
		Assert.Equal([0, 25, 50, 75, 100], Wilkinson.Extended(0, 100));
	}

	[Fact]
	public void WilkinsonExtendedUnitSteps()
	{
		Assert.Equal([1, 2, 3, 4, 5, 6, 7], Wilkinson.Extended(1, 7));
	}

	[Fact]
	public void WilkinsonExtendedNegativeSpan()
	{
		Assert.Equal([-2.5, 0, 2.5, 5, 7.5], Wilkinson.Extended(-2.5, 7.5));
	}

	[Fact]
	public void WilkinsonExtendedCoversRangeUniformly()
	{
		var breaks = Wilkinson.Extended(0, 5.25);

		Assert.NotNull(breaks);
		Assert.True(breaks.Length >= 2);

		var step = breaks[1] - breaks[0];

		for (var i = 1; i < breaks.Length; i++)
		{
			Assert.Equal(step, breaks[i] - breaks[i - 1], 9);
		}
	}

	[Fact]
	public void PrettyFallsBackWhereWilkinsonGivesUp()
	{
		// Extended.cs relies on this chain: Wilkinson.Extended(...) ?? Pretty.Run(...).
		Assert.Null(Wilkinson.Extended(0.0031, 0.0097));
		Assert.NotNull(Pretty.Run(0.0031, 0.0097));

		Assert.Null(Wilkinson.Extended(3, 3));
		Assert.NotNull(Pretty.Run(3, 3));
	}

	[Fact]
	public void PrettyNiceRange()
	{
		Assert.Equal([0, 20, 40, 60, 80, 100], Pretty.Run(0, 100));
	}

	[Fact]
	public void MinorBreaksAreMidpoints()
	{
		var minor = Utils.MinorBreaks([0, 25, 50, 75, 100], 0, 100);

		Assert.Equal([12.5, 37.5, 62.5, 87.5], minor);
	}

	[Fact]
	public void MinorBreaksExtendPastLastMajor()
	{
		// Range max (5.25) exceeds the last break (5): one extra step is added at the
		// end and the array must be sized exactly — no zero-filled tail element.
		var minor = Utils.MinorBreaks([0, 1, 2, 3, 4, 5], 0, 5.25);

		Assert.Equal([0.5, 1.5, 2.5, 3.5, 4.5, 5.5], minor);
	}

	[Fact]
	public void MinorBreaksNullForDegenerateInput()
	{
		Assert.Null(Utils.MinorBreaks([1.0], 0, 2));
		Assert.Null(Utils.MinorBreaks(null!, 0, 2));
	}
}

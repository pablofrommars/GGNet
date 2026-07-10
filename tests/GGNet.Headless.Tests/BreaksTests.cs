namespace GGNet.Headless.Tests;

public class BreaksTests
{
	[Fact]
	public void WilkinsonExtendedNiceRange()
	{
		// Arrange

		// Act

		var breaks = Wilkinson.Extended(0, 100);

		// Assert

		breaks.Should().Equal(0, 25, 50, 75, 100);
	}

	[Fact]
	public void WilkinsonExtendedUnitSteps()
	{
		// Arrange

		// Act

		var breaks = Wilkinson.Extended(1, 7);

		// Assert

		breaks.Should().Equal(1, 2, 3, 4, 5, 6, 7);
	}

	[Fact]
	public void WilkinsonExtendedNegativeSpan()
	{
		// Arrange

		// Act

		var breaks = Wilkinson.Extended(-2.5, 7.5);

		// Assert

		breaks.Should().Equal(-2.5, 0, 2.5, 5, 7.5);
	}

	[Fact]
	public void WilkinsonExtendedCoversRangeUniformly()
	{
		// Arrange

		// Act

		var breaks = Wilkinson.Extended(0, 5.25);

		// Assert

		breaks.Should().NotBeNull();
		breaks!.Length.Should().BeGreaterThanOrEqualTo(2);

		var step = breaks[1] - breaks[0];

		using var _ = new AssertionScope();

		for (var i = 1; i < breaks.Length; i++)
		{
			(breaks[i] - breaks[i - 1]).Should().BeApproximately(step, 1e-9);
		}
	}

	[Fact]
	public void PrettyFallsBackWhereWilkinsonGivesUp()
	{
		// Arrange

		// Act

		// Assert

		// Extended.cs relies on this chain: Wilkinson.Extended(...) ?? Pretty.Run(...).
		using var _ = new AssertionScope();

		Wilkinson.Extended(0.0031, 0.0097).Should().BeNull();
		Pretty.Run(0.0031, 0.0097).Should().NotBeNull();

		Wilkinson.Extended(3, 3).Should().BeNull();
		Pretty.Run(3, 3).Should().NotBeNull();
	}

	[Fact]
	public void PrettyNiceRange()
	{
		// Arrange

		// Act

		var breaks = Pretty.Run(0, 100);

		// Assert

		breaks.Should().Equal(0, 20, 40, 60, 80, 100);
	}

	[Fact]
	public void MinorBreaksAreMidpoints()
	{
		// Arrange

		// Act

		var minor = Utils.MinorBreaks([0, 25, 50, 75, 100], 0, 100);

		// Assert

		minor.Should().Equal(12.5, 37.5, 62.5, 87.5);
	}

	[Fact]
	public void MinorBreaksExtendPastLastMajor()
	{
		// Arrange

		// Act

		// Range max (5.25) exceeds the last break (5): one extra step is added at the
		// end and the array must be sized exactly — no zero-filled tail element.
		var minor = Utils.MinorBreaks([0, 1, 2, 3, 4, 5], 0, 5.25);

		// Assert

		minor.Should().Equal(0.5, 1.5, 2.5, 3.5, 4.5, 5.5);
	}

	[Fact]
	public void MinorBreaksNullForDegenerateInput()
	{
		// Arrange

		// Act

		// Assert

		using var _ = new AssertionScope();

		Utils.MinorBreaks([1.0], 0, 2).Should().BeNull();
		Utils.MinorBreaks(null!, 0, 2).Should().BeNull();
	}
}

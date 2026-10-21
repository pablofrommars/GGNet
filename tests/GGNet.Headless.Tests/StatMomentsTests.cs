namespace GGNet.Headless.Tests;

public class StatMomentsTests
{
	[Theory]
	[InlineData(1e308, 2)]
	[InlineData(1e306, 1000)]
	[InlineData(1e100, 1000)]
	[InlineData(0, 10)]
	[InlineData(double.Epsilon, 10)]
	public void ConstantSamplesKeepTheirMeanAndBandwidthFallback(double value, int count)
	{
		// Arrange

		var values = Enumerable.Repeat(value, count).ToArray();

		// Act

		var summary = Stat.Summary(values, _ => 0, v => v)[0];
		var bandwidth = Stat.Nrd0(values);

		// Assert

		using var _ = new AssertionScope();
		summary.Center.Should().Be(value);
		summary.Lower.Should().Be(value);
		summary.Upper.Should().Be(value);
		bandwidth.Should().Be(0.9 * Math.Pow(count, -0.2));
	}

	[Theory]
	[InlineData(1e-200)]
	[InlineData(1e-160)]
	[InlineData(1e-154)]
	[InlineData(1e160)]
	[InlineData(1e308)]
	public void SymmetricSamplesPreserveRepresentableStandardDeviation(double magnitude)
	{
		// Arrange

		double[] values = [-magnitude, magnitude];
		var expected = Math.Sqrt(2) * magnitude;

		// Act

		var summary = Stat.Summary(values, _ => 0, v => v)[0];

		// Assert

		using var _ = new AssertionScope();
		summary.Center.Should().Be(0);
		WithinUlps(summary.Lower, -expected, 2);
		WithinUlps(summary.Upper, expected, 2);
	}

	[Theory]
	[InlineData(1e160)]
	[InlineData(1e-160)]
	[InlineData(1e-200)]
	public void ZeroAndNonzeroSamplesAvoidSquaredDeviationRangeLoss(double value)
	{
		// Arrange

		double[] values = [0, value];

		// Act

		var summary = Stat.Summary(values, _ => 0, v => v)[0];

		// Assert

		using var _ = new AssertionScope();
		summary.Center.Should().Be(value / 2);
		WithinUlps(summary.Lower, (0.5 - Math.Sqrt(0.5)) * value, 3);
		WithinUlps(summary.Upper, (0.5 + Math.Sqrt(0.5)) * value, 3);
	}

	[Theory]
	[InlineData(1e-20)]
	[InlineData(1e-300)]
	[InlineData(1.5e-323)]
	public void CancellationRetainsTheSmallMean(double residual)
	{
		// Arrange

		double[] values = [double.MaxValue, residual, -double.MaxValue];

		// Act

		var summary = Stat.Summary(values, _ => 0, v => v, spread: 0)[0];

		// Assert

		using var _ = new AssertionScope();
		summary.Center.Should().Be(residual / 3);
		summary.Lower.Should().Be(residual / 3);
		summary.Upper.Should().Be(residual / 3);
	}

	[Fact]
	public void SubnormalMeanAndDeviationRemainUnroundedUntilEndpointFormation()
	{
		// Arrange

		double[] values = [0, double.Epsilon];

		// Act

		var summary = Stat.Summary(values, _ => 0, v => v)[0];
		var amplified = Stat.Summary(values, _ => 0, v => v, spread: 1e300)[0];

		// Assert

		using var _ = new AssertionScope();
		summary.Center.Should().Be(0);
		summary.Lower.Should().Be(0);
		summary.Upper.Should().Be(double.Epsilon);
		WithinUlps(amplified.Upper, (double.Epsilon * 1e300) / Math.Sqrt(2), 2);
		WithinUlps(amplified.Lower, -(double.Epsilon * 1e300) / Math.Sqrt(2), 2);
	}

	[Fact]
	public void AdjacentHugeValuesUseTheMidpointResidualForVarianceAndQuartiles()
	{
		// Arrange

		var a = 1e308;
		var b = Math.BitIncrement(a);
		double[] values = [a, b];
		var difference = b - a;
		var expectedBandwidth = (difference * 0.5 / 1.34) * 0.9 * Math.Pow(2, -0.2);

		// Act

		var summary = Stat.Summary(values, _ => 0, v => v, spread: 16)[0];
		var bandwidth = Stat.Nrd0(values);

		// Assert

		using var _ = new AssertionScope();
		WithinUlps(summary.Lower, a + difference * (0.5 - 16 / Math.Sqrt(2)), 1);
		WithinUlps(summary.Upper, a + difference * (0.5 + 16 / Math.Sqrt(2)), 1);
		WithinUlps(bandwidth, expectedBandwidth, 2);
	}

	[Fact]
	public void UnrepresentableSdStillAllowsRepresentableRequiredOutputs()
	{
		// Arrange

		double[] values = [-double.MaxValue, double.MaxValue];
		var expectedWidth = (double.MaxValue * 0.1) * Math.Sqrt(2);
		var expectedBandwidth = (double.MaxValue / 1.34) * 0.9 * Math.Pow(2, -0.2);

		// Act

		var summary = Stat.Summary(values, _ => 0, v => v, spread: 0.1)[0];
		var bandwidth = Stat.Nrd0(values);

		// Assert

		using var _ = new AssertionScope();
		WithinUlps(summary.Lower, -expectedWidth, 2);
		WithinUlps(summary.Upper, expectedWidth, 2);
		WithinUlps(bandwidth, expectedBandwidth, 2);
	}

	[Fact]
	public void OverflowingInterquartileRangeStillAllowsFiniteBandwidth()
	{
		// Arrange

		double[] values = [-double.MaxValue, -double.MaxValue, double.MaxValue, double.MaxValue];
		var expected = double.MaxValue * (0.9 * (2 / Math.Sqrt(3)) * Math.Pow(4, -0.2));

		// Act

		var bandwidth = Stat.Nrd0(values);

		// Assert

		WithinUlps(bandwidth, expected, 3);
	}

	[Fact]
	public void TinyQuartilesSurviveAnExtremeOutlier()
	{
		// Arrange

		double[] values = [0, 1e-200, 2e-200, 3e-200, 4e-200, 5e-200, double.MaxValue];
		var expected = (3e-200 / 1.34) * 0.9 * Math.Pow(7, -0.2);

		// Act

		var bandwidth = Stat.Nrd0(values);

		// Assert

		WithinUlps(bandwidth, expected, 3);
	}

	[Theory]
	[InlineData(0.25)]
	[InlineData(-0.25)]
	[InlineData(2)]
	public void GroupedSummaryMatchesDirectSummary(double spread)
	{
		// Arrange

		double[] values = [-1e200, 1e200, 2e200];
		var expected = Stat.Summary(values, _ => 3, v => v, spread)[0];

		// Act

		var actual = Stat.Summary(values, _ => 3, v => v, _ => "group", spread)[0];

		// Assert

		using var _ = new AssertionScope();
		actual.X.Should().Be(expected.X);
		actual.Center.Should().Be(expected.Center);
		actual.Lower.Should().Be(expected.Lower);
		actual.Upper.Should().Be(expected.Upper);
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void NonfiniteSamplesAreRejectedEvenForSingletons(double value)
	{
		// Arrange

		double[] values = [value];

		// Act

		Action nrd0 = () => Stat.Nrd0(values);
		Action summary = () => Stat.Summary(values, _ => 0, v => v);
		Action grouped = () => Stat.Summary(values, _ => 0, v => v, _ => 0);
		Action density = () => Stat.Density(values, v => v, bandwidth: 1, n: 0);
		Action groupedDensity = () => Stat.Density(values, v => v, _ => 0, bandwidth: 1, n: 0);

		// Assert

		using var _ = new AssertionScope();
		nrd0.Should().Throw<GGNetUserException>().WithMessage("*Sample value*finite*");
		summary.Should().Throw<GGNetUserException>().WithMessage("*Sample value*finite*");
		grouped.Should().Throw<GGNetUserException>().WithMessage("*Sample value*finite*");
		density.Should().Throw<GGNetUserException>().WithMessage("*Sample value*finite*");
		groupedDensity.Should().Throw<GGNetUserException>().WithMessage("*Sample value*finite*");
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void SummaryRejectsNonfiniteKeysAndSpreads(double value)
	{
		// Arrange

		double[] values = [1];

		// Act

		Action x = () => Stat.Summary(values, _ => value, v => v);
		Action groupedX = () => Stat.Summary(values, _ => value, v => v, _ => 0);
		Action spread = () => Stat.Summary(Array.Empty<double>(), _ => 0, v => v, value);
		Action groupedSpread = () => Stat.Summary(Array.Empty<double>(), _ => 0, v => v, _ => 0, value);

		// Assert

		using var _ = new AssertionScope();
		x.Should().Throw<GGNetUserException>().WithMessage("*Summary x*finite*");
		groupedX.Should().Throw<GGNetUserException>().WithMessage("*Summary x*finite*");
		spread.Should().Throw<GGNetUserException>().WithMessage("*Summary spread*finite*");
		groupedSpread.Should().Throw<GGNetUserException>().WithMessage("*Summary spread*finite*");
	}

	[Fact]
	public void RequiredUnrepresentableResultsAreRejectedWithRemedies()
	{
		// Arrange

		double[] wide = [-double.MaxValue, double.MaxValue];
		double[] tiny = [0, double.Epsilon];
		double[] offset = [0, 1e308];

		// Act

		Action interval = () => Stat.Summary(wide, _ => 0, v => v);
		Action product = () => Stat.Summary(offset, _ => 0, v => v, spread: 3);
		Action bandwidth = () => Stat.Nrd0(tiny);

		// Assert

		using var _ = new AssertionScope();
		interval.Should().Throw<GGNetUserException>().WithMessage("*endpoints*Reduce spread*");
		product.Should().Throw<GGNetUserException>().WithMessage("*endpoints*Reduce spread*");
		bandwidth.Should().Throw<GGNetUserException>().WithMessage("*bandwidth*Rescale*explicit*");
	}

	[Fact]
	public void RecomputeUsesTheCurrentValuesAndRevalidatesThem()
	{
		// Arrange

		var values = new List<double> { 1e308, 1e308 };
		var summary = Stat.Summary(values, _ => 0, v => v);
		var grouped = Stat.Summary(values, _ => 0, v => v, _ => 0);

		// Act

		values[0] = -1e-200;
		values[1] = 1e-200;
		summary.Recompute();
		grouped.Recompute();
		var lower = summary[0].Lower;
		values[0] = double.NaN;
		Action invalid = summary.Recompute;
		Action invalidGroup = grouped.Recompute;

		// Assert

		using var _ = new AssertionScope();
		WithinUlps(lower, -Math.Sqrt(2) * 1e-200, 2);
		grouped[0].Lower.Should().Be(lower);
		invalid.Should().Throw<GGNetUserException>();
		invalidGroup.Should().Throw<GGNetUserException>();
	}

	[Fact]
	public void CancellationInNonunitEndpointsKeepsTheSmallRepresentableResult()
	{
		// Arrange

		const double value = 1e200;
		double[] values = [0, value, 2 * value];
		var spread = Math.BitDecrement(1.0);
		var expectedLower = Math.ScaleB(value, -53);

		// Act

		var summary = Stat.Summary(values, _ => 0, v => v, spread)[0];

		// Assert

		using var _ = new AssertionScope();
		summary.Center.Should().Be(value);
		WithinUlps(summary.Lower, expectedLower, 1);
		WithinUlps(summary.Upper, 2 * value, 1);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	public void ShortFiniteSamplesKeepTheUnitBandwidth(int count)
	{
		// Arrange

		var values = Enumerable.Repeat(double.MaxValue, count).ToArray();

		// Act

		var bandwidth = Stat.Nrd0(values);

		// Assert

		bandwidth.Should().Be(1);
	}

	[Fact]
	public void DensityRecomputeRejectsNewNonfiniteSamples()
	{
		// Arrange

		var values = new List<double> { 1, 2 };
		var density = Stat.Density(values, v => v, bandwidth: 1, n: 3);
		var grouped = Stat.Density(values, v => v, _ => 0, bandwidth: 1, n: 3);

		// Act

		values[0] = double.PositiveInfinity;
		Action invalid = density.Recompute;
		Action invalidGroup = grouped.Recompute;

		// Assert

		using var _ = new AssertionScope();
		invalid.Should().Throw<GGNetUserException>().WithMessage("*Sample value*finite*");
		invalidGroup.Should().Throw<GGNetUserException>().WithMessage("*Sample value*finite*");
	}

	[Fact]
	public void SubnormalQuartilesStayScaledEvenWhenTheSdIsOrdinary()
	{
		// Arrange

		double[] tooSmall = [0, 0, double.Epsilon, double.Epsilon, 1];
		double[] representable = [0, 0, 2 * double.Epsilon, 2 * double.Epsilon, 1];

		// Act

		Action invalid = () => Stat.Nrd0(tooSmall);
		var bandwidth = Stat.Nrd0(representable);

		// Assert

		using var _ = new AssertionScope();
		invalid.Should().Throw<GGNetUserException>().WithMessage("*bandwidth*Rescale*");
		bandwidth.Should().Be(double.Epsilon);
	}

	private static void WithinUlps(double actual, double expected, int ulps)
	{
		var lower = expected;
		var upper = expected;

		for (var i = 0; i < ulps; i++)
		{
			lower = Math.BitDecrement(lower);
			upper = Math.BitIncrement(upper);
		}

		actual.Should().BeInRange(lower, upper);
	}
}

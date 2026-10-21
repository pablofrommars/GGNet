namespace GGNet.Headless.Tests;

public class StatDensityRangeTests
{
	[Theory]
	[InlineData(-1.0, "bandwidth*")]
	[InlineData(1.0, "from*")]
	public void ExplicitArgumentValidationPreservesDiagnosticOrder(double bandwidth, string message)
	{
		// Arrange

		double[] values = [];

		// Act

		Action calculate = () => Stat.Density(values, value => value, bandwidth: bandwidth, n: 0, from: double.NaN, to: double.PositiveInfinity);

		// Assert

		calculate.Should().Throw<GGNetUserException>().WithMessage(message);
	}

	[Theory]
	[InlineData(-1000)]
	[InlineData(1000)]
	public void ExactPowerOfTwoScalingAndTranslationPreserveDensity(int exponent)
	{
		// Arrange

		double[] values = [-1.0, 1.0];
		var scaledValues = values.Select(value => Math.ScaleB(value, exponent)).ToArray();
		var translatedValues = values.Select(value => value + 1024.0).ToArray();

		// Act

		var original = Stat.Density(values, value => value, bandwidth: 0.5, n: 2, from: 0.25, to: 0.25)[0].Density;
		var scaled = Stat.Density(scaledValues, value => value, bandwidth: Math.ScaleB(0.5, exponent), n: 2, from: Math.ScaleB(0.25, exponent), to: Math.ScaleB(0.25, exponent))[0].Density;
		var translated = Stat.Density(translatedValues, value => value, bandwidth: 0.5, n: 2, from: 1024.25, to: 1024.25)[0].Density;

		// Assert

		using var _ = new AssertionScope();
		scaled.Should().BeGreaterThan(0.0);
		Math.Abs(BitConverter.DoubleToInt64Bits(scaled) - BitConverter.DoubleToInt64Bits(Math.ScaleB(original, -exponent))).Should().BeLessThanOrEqualTo(2);
		translated.Should().Be(original);
	}

	[Theory]
	[InlineData(5.357219070500944e-299, double.Epsilon)]
	[InlineData(5.357219070500945e-299, 0.0)]
	public void AdjacentTailCoordinatesRespectFinalSubnormalRounding(double at, double expected)
	{
		// Arrange

		// Exact-input 200-digit oracle straddles half the minimum subnormal.
		double[] values = [0.0];

		// Act

		var result = Stat.Density(values, value => value, bandwidth: 1e-300, n: 2, from: at, to: at);

		// Assert

		result[0].Density.Should().Be(expected);
	}

	[Fact]
	public void AdjacentOverflowCoordinateStillRejectsGenuineOverflow()
	{
		// Arrange

		double[] values = [0.0];
		const double at = 1.52415442e-316;

		// Act

		Action calculate = () => Stat.Density(values, value => value, bandwidth: 2.21919009793619e-309, n: 2, from: at, to: at);

		// Assert

		calculate.Should().Throw<GGNetUserException>().WithMessage("Density cannot*Increase bandwidth*");
	}

	[Theory]
	[InlineData(-1, 1.1735510892143333e308)]
	[InlineData(0, 1.1735510892143295e308)]
	[InlineData(1, 1.1735510892143333e308)]
	public void RareDifferenceBranchesAgreeWithExactInputOracle(int direction, double expected)
	{
		// Arrange

		const double h = 3e-309;
		var at = 32 * h;
		at = direction < 0 ? Math.BitDecrement(at) : direction > 0 ? Math.BitIncrement(at) : at;
		double[] values = [at - 0.5 * h];

		// Act

		var result = Stat.Density(values, value => value, bandwidth: h, n: 2, from: at, to: at);

		// Assert

		Math.Abs(BitConverter.DoubleToInt64Bits(result[0].Density) - BitConverter.DoubleToInt64Bits(expected)).Should().BeLessThanOrEqualTo(1);
	}

	[Fact]
	public void RareOppositeSignMixedSamplesKeepOracleAccuracy()
	{
		// Arrange

		const double h = 3e-309;
		var at = Math.BitIncrement(4 * h);
		var values = Enumerable.Repeat(at, 20).Append(-4 * h).ToArray();

		// Act

		var result = Stat.Density(values, value => value, bandwidth: h, n: 2, from: at, to: at);

		// Assert

		Math.Abs(BitConverter.DoubleToInt64Bits(result[0].Density) - BitConverter.DoubleToInt64Bits(1.2664834298458186e308)).Should().BeLessThanOrEqualTo(1);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void SortedGroupPreparationPinsBandwidthVersusCoordinatePrecedence(bool bandwidthFirst)
	{
		// Arrange

		double[] tiny = [0, 0, double.Epsilon, double.Epsilon, 1];
		double[] huge = [-1e308, 1e308];
		var values = tiny.Select(value => (Key: bandwidthFirst ? 0 : 1, Value: value)).Concat(huge.Select(value => (Key: bandwidthFirst ? 1 : 0, Value: value))).ToArray();

		// Act

		Action calculate = () => Stat.Density(values, value => value.Value, value => value.Key, n: 3);

		// Assert

		calculate.Should().Throw<GGNetUserException>().WithMessage(bandwidthFirst ? "Automatic bandwidth*" : "*grid coordinates*");
	}

	[Fact]
	public void GroupedAndUngroupedDefaultOverflowingSpanAgree()
	{
		// Arrange

		double[] values = [0.0];

		// Act

		var single = Stat.Density(values, value => value, bandwidth: 4e307, n: 3);
		var grouped = Stat.Density(values, value => value, _ => 0, bandwidth: 4e307, n: 3);

		// Assert

		using var _ = new AssertionScope();
		grouped.Select(point => (point.At, point.Density)).Should().Equal(single.Select(point => (point.At, point.Density)));
		grouped.Select(point => point.At).Should().Equal(-1.2e308, 0.0, 1.2e308);
	}

	[Theory]
	[InlineData(38, 4.931166881551145e-07)]
	[InlineData(40, 6.576277210950691e-41)]
	[InlineData(45, 3.391585011941546e-133)]
	[InlineData(50, 2.4291999043209354e-236)]
	[InlineData(53, 1.9362694855621862e-303)]
	public void ExactStandardizedDistancesKeepTightOracleAccuracy(int distance, double expected)
	{
		// Arrange

		// Independent 200-digit Decimal oracle; power-of-two h makes u and -u²/2 exact.
		var h = Math.ScaleB(1.0, -1022);
		var at = distance * h;
		double[] values = [0.0];

		// Act

		var result = Stat.Density(values, value => value, bandwidth: h, n: 2, from: at, to: at);

		// Assert

		Math.Abs(BitConverter.DoubleToInt64Bits(result[0].Density) - BitConverter.DoubleToInt64Bits(expected)).Should().BeLessThanOrEqualTo(8);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(10)]
	[InlineData(10000)]
	public void FiniteOverflowBoundaryUsesAccurateNormalization(int count)
	{
		// Arrange

		// Exact-input oracle rounds this density to MaxValue; binary64 normalization alone overflows.
		var values = new double[count];
		const double h = 2.21919009793619e-309;
		const double at = 1.52415447e-316;

		// Act

		var result = Stat.Density(values, value => value, bandwidth: h, n: 2, from: at, to: at);

		// Assert

		using var _ = new AssertionScope();
		result[0].Density.Should().Be(double.MaxValue);
		result[1].Should().Be(result[0]);
	}

	[Theory]
	[InlineData(0.0)]
	[InlineData(1e308)]
	[InlineData(-1e308)]
	public void NearOverflowCenterHandlesNonzeroEqualCoordinates(double value)
	{
		// Arrange

		double[] values = [value];

		// Act

		var result = Stat.Density(values, item => item, bandwidth: 3e-309, n: 2, from: value, to: value);

		// Assert

		result[0].Density.Should().BeApproximately(1.329807601338109e308, 2e292);
	}

	[Fact]
	public void OrdinaryRemoteSamplesRetainProductionBits()
	{
		// Arrange

		var values = Enumerable.Range(0, 10).Select(index => (index - 5.0) / 5).ToArray();

		// Act

		var result = Stat.Density(values, value => value, bandwidth: 0.01, n: 64, from: -2, to: 2);

		// Assert

		BitConverter.DoubleToInt64Bits(result[32].Density).Should().Be(4583108264085517282);
	}

	[Theory]
	[InlineData(0.0, 1e308, 0.0, 3.989422804014326e-309)]
	[InlineData(-1.7e308, 6e307, 1.7e308, 7.07814793e-316)]
	[InlineData(0.0, 1e-300, 4e-299, 1.4632702508383808e-48)]
	[InlineData(0.0, 1e-300, 4.01e-299, 2.6667060314895255e-50)]
	public void ExtremeDensityRetainsRepresentablePositiveValues(double sample, double bandwidth, double at, double expected)
	{
		// Arrange

		// Independent 200-digit Decimal Gaussian oracle uses exact binary64 inputs.
		double[] values = [sample];

		// Act

		var result = Stat.Density(values, value => value, bandwidth: bandwidth, n: 2, from: at, to: at);

		// Assert

		using var _ = new AssertionScope();
		result[0].Density.Should().BeGreaterThan(0.0);
		result[0].Density.Should().BeApproximately(expected, Math.Max(expected * 1e-12, 2 * double.Epsilon));
		result[1].Should().Be(result[0]);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void OverflowingSpanKeepsFinitePinnedEndpointsAndMidpoint(bool descending)
	{
		// Arrange

		double[] values = [-1e308, 1e308];
		var from = descending ? 1e308 : -1e308;

		// Act

		var result = Stat.Density(values, value => value, bandwidth: 1e307, n: 3, from: from, to: -from);

		// Assert

		using var _ = new AssertionScope();
		result.Select(point => point.At).Should().Equal(from, 0.0, -from);
		result[0].Density.Should().BeGreaterThan(0.0);
		result[1].Density.Should().Be(0.0);
		result[2].Density.Should().Be(result[0].Density);
	}

	[Fact]
	public void CancellingDefaultEndpointAvoidsIntermediateOverflow()
	{
		// Arrange

		double[] values = [-1e308];

		// Act

		var result = Stat.Density(values, value => value, bandwidth: 6e307, n: 3, from: -1e308);

		// Assert

		using var _ = new AssertionScope();
		result[0].At.Should().Be(-1e308);
		result[2].At.Should().Be(7.999999999999999e307);
		result.Should().OnlyContain(point => double.IsFinite(point.At) && point.Density > 0.0);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(3)]
	public void InvalidExplicitBoundsAreRejectedEvenWithoutOutput(int count)
	{
		// Arrange

		double[] empty = [];
		double[] values = [0.0];

		// Act

		Action emptyFrom = () => Stat.Density(empty, value => value, n: count, from: double.NaN);
		Action shortTo = () => Stat.Density(values, value => value, n: count, to: double.PositiveInfinity);

		// Assert

		using var _ = new AssertionScope();
		emptyFrom.Should().Throw<GGNetUserException>().WithMessage("*from*finite*Supply*");
		shortTo.Should().Throw<GGNetUserException>().WithMessage("*to*finite*Supply*");
	}

	[Fact]
	public void RoundedRepeatedCoordinatesAndSignedZeroEndpointsRemainAccepted()
	{
		// Arrange

		double[] large = [1e308];
		double[] zero = [0.0];

		// Act

		var repeated = Stat.Density(large, value => value, bandwidth: 1.0, n: 4);
		var signed = Stat.Density(zero, value => value, bandwidth: 1.0, n: 3, from: -0.0, to: 0.0);

		// Assert

		using var _ = new AssertionScope();
		repeated.Select(point => point.At).Should().Equal(1e308, 1e308, 1e308, 1e308);
		BitConverter.DoubleToInt64Bits(signed[0].At).Should().Be(long.MinValue);
		BitConverter.DoubleToInt64Bits(signed[2].At).Should().Be(0L);
	}

	[Fact]
	public void UnrepresentableGridAndDensityHaveActionableDistinctErrors()
	{
		// Arrange

		double[] values = [0.0];

		// Act

		Action grid = () => Stat.Density(values, value => value, bandwidth: 1e308, n: 3);
		Action density = () => Stat.Density(values, value => value, bandwidth: 1e-310, n: 3);

		// Assert

		using var _ = new AssertionScope();
		grid.Should().Throw<GGNetUserException>().WithMessage("*grid coordinates*finite*rescale*");
		density.Should().Throw<GGNetUserException>().WithMessage("Density cannot*Increase bandwidth*");
	}

	[Fact]
	public void LaterGroupCoordinatesAreValidatedBeforeEarlierGroupDensity()
	{
		// Arrange

		(int Key, double Value)[] values = [(0, 0.0), (0, 1e-310), (1, -1e308), (1, 1e308)];

		// Act

		Action calculate = () => Stat.Density(values, value => value.Value, value => value.Key, n: 3);

		// Assert

		calculate.Should().Throw<GGNetUserException>().WithMessage("*grid coordinates*finite*");
	}

	[Fact]
	public void FailedGroupedDensityRefreshPreservesAllItemsAndCanRecover()
	{
		// Arrange

		var values = new List<(int Key, double Value)> { (0, 0), (0, 1), (1, 2), (1, 3) };
		var result = Stat.Density(values, value => value.Value, value => value.Key, n: 3);
		var before = result.ToArray();
		values[2] = (1, 0.0);
		values[3] = (1, 1e-310);

		// Act

		var failure = Record.Exception(result.Recompute);
		var failedItems = result.ToArray();
		values[2] = (1, 2);
		values[3] = (1, 3);
		result.Recompute();
		var recoveredItems = result.ToArray();

		// Assert

		using var _ = new AssertionScope();
		failure.Should().BeOfType<GGNetUserException>().Which.Message.Should().StartWith("Density cannot");
		failedItems.Should().Equal(before);
		recoveredItems.Should().Equal(before);
	}

	[Fact]
	public void FailedGridRefreshPreservesOutputAndCanRecover()
	{
		// Arrange

		var values = new List<double> { 0.0 };
		var result = Stat.Density(values, value => value, bandwidth: 1e307, n: 3);
		var before = result.ToArray();
		values[0] = double.MaxValue;

		// Act

		var failure = Record.Exception(result.Recompute);
		var failedItems = result.ToArray();
		values[0] = 0.0;
		result.Recompute();
		var recoveredItems = result.ToArray();

		// Assert

		using var _ = new AssertionScope();
		failure.Should().BeOfType<GGNetUserException>().Which.Message.Should().Contain("grid coordinates");
		failedItems.Should().Equal(before);
		recoveredItems.Should().Equal(before);
	}

	[Fact]
	public void DuplicatingAndPermutingTenThousandSamplesPreservesScaledDensity()
	{
		// Arrange

		double[] values = [0.0, 1e308];
		var duplicated = Enumerable.Range(0, 10000).Select(index => values[index % 2]).ToArray();

		// Act

		var original = Stat.Density(values, value => value, bandwidth: 1e308, n: 2, from: 0, to: 1);
		var repeated = Stat.Density(duplicated, value => value, bandwidth: 1e308, n: 2, from: 0, to: 1);
		Array.Reverse(duplicated);
		repeated.Recompute();

		// Assert

		using var _ = new AssertionScope();
		repeated[0].Density.Should().BeGreaterThan(0.0);
		Math.Abs(BitConverter.DoubleToInt64Bits(original[0].Density) - BitConverter.DoubleToInt64Bits(repeated[0].Density)).Should().BeLessThanOrEqualTo(2);
	}

	[Fact]
	public void OrdinaryDensityRetainsRecordedProductionBits()
	{
		// Arrange

		var values = Enumerable.Range(0, 10).Select(index => (index - 5.0) / 5).ToArray();
		(int Index, long Bits)[] expected = [(0, 4580698962738362812), (1, 4582295650428791007), (16, 4599005381265925056), (31, 4602263866753095483), (32, 4602214430965104788), (48, 4596069987730403461), (63, 4574367519604929117)];

		// Act

		var result = Stat.Density(values, value => value, bandwidth: 0.5, n: 64, from: -2, to: 2);

		// Assert

		using var _ = new AssertionScope();
		foreach (var (index, bits) in expected)
		{
			BitConverter.DoubleToInt64Bits(result[index].Density).Should().Be(bits);
		}
	}
}

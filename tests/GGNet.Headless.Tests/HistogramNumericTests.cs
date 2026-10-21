namespace GGNet.Headless.Tests;

public class HistogramNumericTests
{
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(17)]
	public void WideFiniteRangesPreserveMassAndMembership(int count)
	{
		// Arrange

		double[] values = [-1e308, -5e307, 0, 5e307, 1e308];

		// Act

		var bins = Stat.Bin(values, v => v, bins: count);

		// Assert

		AssertHistogram(bins, values, count);
	}

	[Fact]
	public void OneWideBinHasRepresentablePositiveDensity()
	{
		// Arrange

		double[] values = [-1e308, 1e308];

		// Act

		var bin = Stat.Bin(values, v => v, bins: 1)[0];

		// Assert

		using var _ = new AssertionScope();

		bin.Mid.Should().Be(0);
		bin.Density.Should().Be(5e-309);
		(bin.Density * 1e308 * 2).Should().BeApproximately(1, 1e-14);
	}

	[Theory]
	[InlineData(1e20)]
	[InlineData(-1e20)]
	[InlineData(1e308)]
	[InlineData(-1e308)]
	public void AdjacentLargeDoublesSupportOneBin(double lower)
	{
		// Arrange

		double[] values = [lower, Math.BitIncrement(lower)];

		// Act

		var bins = Stat.Bin(values, v => v, bins: 1);

		// Assert

		AssertHistogram(bins, values, 1);
	}

	[Theory]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	public void ImpossibleAdjacentDoubleSubdivisionsAreRejected(int count)
	{
		// Arrange

		double[] values = [1e20, Math.BitIncrement(1e20)];

		// Act

		var act = () => Stat.Bin(values, v => v, bins: count);

		// Assert

		act.Should().Throw<GGNetUserException>().WithMessage("Stat.Bin*distinct finite intervals*Use fewer bins*");
	}

	[Theory]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	public void ImpossibleSubnormalSubdivisionsAreRejected(int count)
	{
		// Arrange

		double[] values = [0, double.Epsilon];

		// Act

		var act = () => Stat.Bin(values, v => v, bins: count);

		// Assert

		act.Should().Throw<GGNetUserException>().WithMessage("Stat.Bin*distinct finite intervals*Use fewer bins*");
	}

	[Theory]
	[InlineData(0, double.Epsilon)]
	[InlineData(double.Epsilon, 4 * double.Epsilon)]
	[InlineData(0, 1e-310)]
	public void UnrepresentableRequiredDensitiesAreRejected(double lower, double upper)
	{
		// Arrange

		double[] values = [lower, upper];

		// Act

		var act = () => Stat.Bin(values, v => v, bins: 1);

		// Assert

		act.Should().Throw<GGNetUserException>().WithMessage("Stat.Bin*positive bin density*Rescale*");
	}

	[Fact]
	public void SubnormalWidthsWithRepresentableDensitiesAreAccepted()
	{
		// Arrange

		double[] values = [0, 1e-308];

		// Act

		var bins = Stat.Bin(values, v => v, bins: 2);

		// Assert

		AssertHistogram(bins, values, 2);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(2)]
	[InlineData(-2)]
	[InlineData(double.Epsilon)]
	[InlineData(9007199254740992)]
	[InlineData(-9007199254740992)]
	[InlineData(9007199254740991.0)]
	[InlineData(-9007199254740991.0)]
	[InlineData(4503599627370495.5)]
	[InlineData(-4503599627370495.5)]
	[InlineData(1152921504606846848.0)]
	[InlineData(-1152921504606846848.0)]
	[InlineData(1e20)]
	[InlineData(-1e20)]
	[InlineData(1e308)]
	[InlineData(-1e308)]
	[InlineData(double.MaxValue)]
	[InlineData(double.MinValue)]
	public void ConstantPaddingIsFiniteBoundedAndPreservesRequestedCount(double value)
	{
		// Arrange

		double[] values = [value, value, value];
		const int count = 30;
		var magnitude = Math.Abs(value);
		var spacing = magnitude == double.MaxValue
			? magnitude - Math.BitDecrement(magnitude)
			: Math.BitIncrement(magnitude) - magnitude;
		var bound = count * Math.Max(1, 2 * spacing);

		// Act

		var bins = Stat.Bin(values, v => v, bins: count);

		// Assert

		using var _ = new AssertionScope();

		AssertHistogram(bins, values, count, constant: true);
		bins.Count(b => b.Count > 0).Should().Be(1);
		(value - bins[0].Min).Should().BeLessThanOrEqualTo(bound);
		(bins[^1].Max - value).Should().BeLessThanOrEqualTo(bound);
	}

	[Fact]
	public void OrdinaryConstantPaddingRetainsUnitIntervalsAboveValue()
	{
		// Arrange

		double[] values = [2, 2];

		// Act

		var bins = Stat.Bin(values, v => v, bins: 3);

		// Assert

		bins.Should().Equal(new Bin(2, 2.5, 3, 2, 1), new Bin(3, 3.5, 4, 0, 0), new Bin(4, 4.5, 5, 0, 0));
	}

	[Fact]
	public void CollapsedNominalEdgesAreRejectedEvenWhenUnevenIntervalsExist()
	{
		// Arrange

		double[] values = [9007199254740989.0, 9007199254740996.0];

		// Act

		var act = () => Stat.Bin(values, v => v, bins: 5);

		// Assert

		act.Should().Throw<GGNetUserException>().WithMessage("Stat.Bin*distinct finite intervals*Use fewer bins*");
	}

	[Theory]
	[InlineData(-0.0, 1.0)]
	[InlineData(-1.0, -0.0)]
	public void ExactNonconstantEndpointsPreserveSignedZero(double lower, double upper)
	{
		// Arrange

		double[] values = [lower, upper];

		// Act

		var bins = Stat.Bin(values, v => v, bins: 2);

		// Assert

		using var _ = new AssertionScope();

		BitConverter.DoubleToInt64Bits(bins[0].Min).Should().Be(BitConverter.DoubleToInt64Bits(lower));
		BitConverter.DoubleToInt64Bits(bins[^1].Max).Should().Be(BitConverter.DoubleToInt64Bits(upper));
	}

	[Fact]
	public void EmittedBoundariesDefineMembershipIncludingMaximum()
	{
		// Arrange

		var values = new List<double> { 0.1, 0.9 };
		var bins = Stat.Bin(values, v => v, bins: 7);
		var boundaries = bins.Skip(1).Select(b => b.Min).ToArray();
		values.AddRange(boundaries);
		values.AddRange(boundaries.Select(Math.BitDecrement));
		values.AddRange(boundaries.Select(Math.BitIncrement));

		// Act

		bins.Recompute();

		// Assert

		AssertHistogram(bins, values, 7);
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void NonfiniteSelectedValuesAreRejected(double value)
	{
		// Arrange

		double[] values = [0, value, 1];

		// Act

		var act = () => Stat.Bin(values, v => v, bins: 2);

		// Assert

		act.Should().Throw<GGNetUserException>().WithMessage("Stat.Bin*finite selected values*Remove NaN and infinity*");
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	[InlineData(1e20)]
	public void InvalidGroupFailsConstructionAndRefreshWithoutPublishingPartialOutput(double invalid)
	{
		// Arrange

		var values = new List<(string Group, double Value)> { ("a", 0), ("a", 2), ("b", 10), ("b", 12) };
		var bins = Stat.Bin(values, v => v.Value, v => v.Group, bins: 2);
		var before = bins.ToArray();
		values[2] = ("b", invalid);
		values[3] = ("b", Math.BitIncrement(invalid));

		// Act

		var refresh = bins.Recompute;
		var construct = () => Stat.Bin(values, v => v.Value, v => v.Group, bins: 2);

		// Assert

		using var _ = new AssertionScope();

		construct.Should().Throw<GGNetUserException>().WithMessage("Stat.Bin*");
		refresh.Should().Throw<GGNetUserException>().WithMessage("Stat.Bin*");
		bins.Should().Equal(before);
	}

	[Fact]
	public void FailedRefreshCanRetryWithRepairedValues()
	{
		// Arrange

		var values = new List<double> { 0, 2 };
		var bins = Stat.Bin(values, v => v, bins: 2);
		values[1] = double.NaN;
		var invalidRefresh = bins.Recompute;
		invalidRefresh.Should().Throw<GGNetUserException>();
		values[1] = 4;

		// Act

		bins.Recompute();

		// Assert

		AssertHistogram(bins, values, 2);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void NonpositiveBinRequestsRemainEmptyEvenForNonfiniteInput(int count)
	{
		// Arrange

		double[] values = [double.NaN];

		// Act

		var bins = Stat.Bin(values, v => v, bins: count);
		var grouped = Stat.Bin(values, v => v, _ => "a", bins: count);

		// Assert

		using var _ = new AssertionScope();

		bins.Should().BeEmpty();
		grouped.Should().BeEmpty();
	}

	[Fact]
	public void EmptySourcesRemainEmpty()
	{
		// Arrange

		double[] values = [];

		// Act

		var bins = Stat.Bin(values, v => v, bins: 3);
		var grouped = Stat.Bin(values, v => v, _ => "a", bins: 3);

		// Assert

		using var _ = new AssertionScope();

		bins.Should().BeEmpty();
		grouped.Should().BeEmpty();
	}

	[Fact]
	public void SelectionIsCapturedOncePerComputation()
	{
		// Arrange

		double[] values = [0, 1, 2];
		var calls = 0;

		// Act

		var bins = Stat.Bin(values, v => { calls++; return v; }, bins: 2);
		bins.Recompute();

		// Assert

		calls.Should().Be(2 * values.Length);
	}

	[Theory]
	[InlineData("en-US")]
	[InlineData("sv-SE")]
	[InlineData("ar-EG")]
	public void NumericResultsAndActionableErrorsAreCultureInvariant(string culture)
	{
		// Arrange

		var previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
		double[] valid = [-1e308, 1e308];
		double[] invalid = [1e20, Math.BitIncrement(1e20)];

		try
		{
			// Act

			var bins = Stat.Bin(valid, v => v, bins: 1);
			var act = () => Stat.Bin(invalid, v => v, bins: 4);

			// Assert

			using var _ = new AssertionScope();

			bins[0].Density.Should().Be(5e-309);
			act.Should().Throw<GGNetUserException>().WithMessage("Stat.Bin cannot represent the requested number of distinct finite intervals over this range. Use fewer bins or rescale the selected values.");
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	private static void AssertHistogram(StatSource<Bin> bins, IReadOnlyList<double> values, int count, bool constant = false)
	{
		using var _ = new AssertionScope();

		bins.Should().HaveCount(count);
		bins.Sum(b => b.Count).Should().Be(values.Count);

		if (!constant)
		{
			bins[0].Min.Should().Be(values.Min());
			bins[^1].Max.Should().Be(values.Max());
		}

		for (var i = 0; i < bins.Count; i++)
		{
			var bin = bins[i];
			var final = i == bins.Count - 1;
			var contained = values.Count(v => v >= bin.Min && (v < bin.Max || (final && v == bin.Max)));
			var exponent = Math.ILogB(Math.Max(Math.Abs(bin.Min), Math.Abs(bin.Max)));
			var scaledWidth = Math.ScaleB(bin.Max, -exponent) - Math.ScaleB(bin.Min, -exponent);
			var mass = Math.ScaleB(bin.Density, exponent) * scaledWidth;

			double.IsFinite(bin.Min).Should().BeTrue();
			double.IsFinite(bin.Mid).Should().BeTrue();
			double.IsFinite(bin.Max).Should().BeTrue();
			double.IsFinite(bin.Density).Should().BeTrue();
			bin.Min.Should().BeLessThan(bin.Max);
			bin.Mid.Should().BeInRange(bin.Min, bin.Max);
			bin.Count.Should().Be(contained);
			mass.Should().BeApproximately((double)contained / values.Count, 1e-13);

			if (bin.Count > 0)
			{
				bin.Density.Should().BePositive();
			}

			if (!final)
			{
				bin.Max.Should().Be(bins[i + 1].Min);
			}
		}
	}
}

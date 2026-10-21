namespace GGNet.Headless.Tests;

public class DensityBandwidthTests
{
	public static TheoryData<double, bool, int> InvalidBandwidthCases()
	{
		var cases = new TheoryData<double, bool, int>();
		double[] bandwidths = [0.0, -0.0, -1.25, double.NaN, double.PositiveInfinity, double.NegativeInfinity];
		int[] gridSizes = [-1, 0, 1, 9];

		foreach (var bandwidth in bandwidths)
		{
			foreach (var n in gridSizes)
			{
				cases.Add(bandwidth, false, n);
				cases.Add(bandwidth, true, n);
			}
		}

		return cases;
	}

	public static TheoryData<double?, bool, int> ValidBandwidthCases()
	{
		var cases = new TheoryData<double?, bool, int>();
		double?[] bandwidths = [null, 0.5];
		int[] gridSizes = [-1, 0, 1, 9];

		foreach (var bandwidth in bandwidths)
		{
			foreach (var n in gridSizes)
			{
				cases.Add(bandwidth, false, n);
				cases.Add(bandwidth, true, n);
			}
		}

		return cases;
	}

	[Theory]
	[MemberData(nameof(InvalidBandwidthCases))]
	public void UngroupedRejectsInvalidBandwidthAtCallTime(double bandwidth, bool empty, int n)
	{
		// Arrange

		double[] values = empty ? [] : [-1.0, 1.0];

		// Act

		Action create = () => Stat.Density(values, v => v, bandwidth: bandwidth, n: n);

		// Assert

		create.Should().Throw<GGNetUserException>().WithMessage("*bandwidth*finite*strictly positive*");
	}

	[Theory]
	[MemberData(nameof(InvalidBandwidthCases))]
	public void GroupedRejectsInvalidBandwidthAtCallTime(double bandwidth, bool empty, int n)
	{
		// Arrange

		double[] values = empty ? [] : [-1.0, 1.0];

		// Act

		Action create = () => Stat.Density(values, v => v, v => v < 0, bandwidth: bandwidth, n: n);

		// Assert

		create.Should().Throw<GGNetUserException>().WithMessage("*bandwidth*finite*strictly positive*");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void RejectsInvalidBandwidthBeforeReadingSource(bool grouped)
	{
		// Arrange

		var source = new Mock<IReadOnlyList<double>>(MockBehavior.Strict);
		Action create = grouped
			? () => Stat.Density(source.Object, v => v, v => v, bandwidth: -1.0)
			: () => Stat.Density(source.Object, v => v, bandwidth: -1.0);

		// Act

		var exception = Record.Exception(create);

		// Assert

		exception.Should().BeOfType<GGNetUserException>().Which.Message.Should().Contain("bandwidth");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void DiagnosticIsActionableAndCultureInvariant(bool grouped)
	{
		// Arrange

		double[] values = [];
		var previousCulture = CultureInfo.CurrentCulture;
		Action create = grouped
			? () => Stat.Density(values, v => v, v => v, bandwidth: -1.25)
			: () => Stat.Density(values, v => v, bandwidth: -1.25);

		// Act

		Exception? exception;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");
			exception = Record.Exception(create);
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
		}

		// Assert

		exception.Should().BeOfType<GGNetUserException>().Which.Message.Should().Be(
			"bandwidth must be finite and strictly positive; received -1.25. Supply a positive finite value or omit bandwidth to use automatic selection.");
	}

	[Theory]
	[MemberData(nameof(ValidBandwidthCases))]
	public void UngroupedAcceptsPositiveAndAutomaticBandwidth(double? bandwidth, bool empty, int n)
	{
		// Arrange

		double[] values = empty ? [] : [-1.0, 1.0];

		// Act

		var density = Stat.Density(values, v => v, bandwidth: bandwidth, n: n);

		// Assert

		using var _ = new AssertionScope();
		density.Should().HaveCount(empty || n < 2 ? 0 : n);
		density.Should().OnlyContain(p => double.IsFinite(p.At) && double.IsFinite(p.Density) && p.Density > 0.0);
	}

	[Theory]
	[MemberData(nameof(ValidBandwidthCases))]
	public void GroupedAcceptsPositiveAndAutomaticBandwidth(double? bandwidth, bool empty, int n)
	{
		// Arrange

		double[] values = empty ? [] : [-1.0, 1.0];

		// Act

		var density = Stat.Density(values, v => v, v => v < 0, bandwidth: bandwidth, n: n);

		// Assert

		using var _ = new AssertionScope();
		density.Should().HaveCount(empty || n < 2 ? 0 : 2 * n);
		density.Should().OnlyContain(p => double.IsFinite(p.At) && double.IsFinite(p.Density) && p.Density > 0.0);
	}
}

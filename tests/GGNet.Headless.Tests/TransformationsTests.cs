using Identity = GGNet.Transformations.Identity<double>;
using Log10 = GGNet.Transformations.Log10;
using Sqrt = GGNet.Transformations.Sqrt;

namespace GGNet.Headless.Tests;

public class TransformationsTests
{
	[Theory]
	[InlineData(1.0)]
	[InlineData(10.0)]
	[InlineData(0.001)]
	[InlineData(12345.678)]
	public void Log10RoundTrip(double value)
	{
		// Arrange

		// Act

		var roundTripped = Log10.Instance.Inverse(Log10.Instance.Apply(value));

		// Assert

		roundTripped.Should().BeApproximately(value, 1e-9);
	}

	[Fact]
	public void Log10Applies()
	{
		// Arrange

		// Act

		var applied = Log10.Instance.Apply(1000.0);

		// Assert

		applied.Should().BeApproximately(3.0, 1e-9);
	}

	[Theory]
	[InlineData(0.0)]
	[InlineData(4.0)]
	[InlineData(2.25)]
	public void SqrtRoundTrip(double value)
	{
		// Arrange

		// Act

		var roundTripped = Sqrt.Instance.Inverse(Sqrt.Instance.Apply(value));

		// Assert

		roundTripped.Should().BeApproximately(value, 1e-9);
	}

	[Fact]
	public void SqrtApplies()
	{
		// Arrange

		// Act

		var applied = Sqrt.Instance.Apply(9.0);

		// Assert

		applied.Should().BeApproximately(3.0, 1e-9);
	}

	[Fact]
	public void IdentityIsIdentity()
	{
		// Arrange

		// Act

		// Assert

		using var _ = new AssertionScope();

		Identity.Instance.Apply(42.0).Should().Be(42.0);
		Identity.Instance.Inverse(42.0).Should().Be(42.0);
	}
}

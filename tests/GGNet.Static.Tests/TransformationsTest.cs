using GGNet.Transformations;

namespace GGNet.Static.Tests;

public class TransformationsTest
{
  [Theory]
  [InlineData(1.0)]
  [InlineData(10.0)]
  [InlineData(0.001)]
  [InlineData(12345.678)]
  public void Log10RoundTrip(double value)
  {
    Assert.Equal(value, Log10.Instance.Inverse(Log10.Instance.Apply(value)), 9);
  }

  [Fact]
  public void Log10Applies()
  {
    Assert.Equal(3.0, Log10.Instance.Apply(1000.0), 9);
  }

  [Theory]
  [InlineData(0.0)]
  [InlineData(4.0)]
  [InlineData(2.25)]
  public void SqrtRoundTrip(double value)
  {
    Assert.Equal(value, Sqrt.Instance.Inverse(Sqrt.Instance.Apply(value)), 9);
  }

  [Fact]
  public void SqrtApplies()
  {
    Assert.Equal(3.0, Sqrt.Instance.Apply(9.0), 9);
  }

  [Fact]
  public void IdentityIsIdentity()
  {
    Assert.Equal(42.0, Identity<double>.Instance.Apply(42.0));
    Assert.Equal(42.0, Identity<double>.Instance.Inverse(42.0));
  }
}

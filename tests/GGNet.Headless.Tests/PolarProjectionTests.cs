namespace GGNet.Headless.Tests;

public class PolarProjectionTests
{
	private const double start = -Math.PI / 2.0;

	[Fact]
	public void ProjectTwelveOClock()
	{
		// Arrange

		// Act

		var (x, y) = Polar.Project(0, 1, 0, 0, 100, start, clockwise: true);

		// Assert

		using var _ = new AssertionScope();

		x.Should().BeApproximately(0, 1e-9);
		y.Should().BeApproximately(-100, 1e-9);
	}

	[Fact]
	public void ProjectQuarterTurnClockwise()
	{
		// Arrange

		// Act

		var (x, y) = Polar.Project(0.25, 1, 0, 0, 100, start, clockwise: true);

		// Assert

		using var _ = new AssertionScope();

		x.Should().BeApproximately(100, 1e-9);
		y.Should().BeApproximately(0, 1e-9);
	}

	[Fact]
	public void ProjectHalfTurn()
	{
		// Arrange

		// Act

		var (x, y) = Polar.Project(0.5, 1, 0, 0, 100, start, clockwise: true);

		// Assert

		using var _ = new AssertionScope();

		x.Should().BeApproximately(0, 1e-9);
		y.Should().BeApproximately(100, 1e-9);
	}

	[Fact]
	public void ProjectQuarterTurnCounterClockwise()
	{
		// Arrange

		// Act

		var (x, y) = Polar.Project(0.25, 1, 0, 0, 100, start, clockwise: false);

		// Assert

		using var _ = new AssertionScope();

		x.Should().BeApproximately(-100, 1e-9);
		y.Should().BeApproximately(0, 1e-9);
	}

	[Theory]
	[InlineData(0.0)]
	[InlineData(0.3)]
	[InlineData(0.7)]
	public void ProjectZeroRadiusIsCenter(double cx)
	{
		// Arrange

		// Act

		var (x, y) = Polar.Project(cx, 0, 42, 17, 100, start, clockwise: true);

		// Assert

		using var _ = new AssertionScope();

		x.Should().BeApproximately(42, 1e-9);
		y.Should().BeApproximately(17, 1e-9);
	}

	[Fact]
	public void ProjectOffsetCenterScalesRadius()
	{
		// Arrange

		// Act

		var (x, y) = Polar.Project(0.25, 0.5, 10, 20, 100, start, clockwise: true);

		// Assert

		using var _ = new AssertionScope();

		x.Should().BeApproximately(60, 1e-9);
		y.Should().BeApproximately(20, 1e-9);
	}
}

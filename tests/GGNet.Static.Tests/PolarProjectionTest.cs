using GGNet.Coords;

namespace GGNet.Static.Tests;

public class PolarProjectionTest
{
	private const double start = -Math.PI / 2.0;

	[Fact]
	public void ProjectTwelveOClock()
	{
		var (x, y) = Polar.Project(0, 1, 0, 0, 100, start, clockwise: true);

		Assert.Equal(0, x, 9);
		Assert.Equal(-100, y, 9);
	}

	[Fact]
	public void ProjectQuarterTurnClockwise()
	{
		var (x, y) = Polar.Project(0.25, 1, 0, 0, 100, start, clockwise: true);

		Assert.Equal(100, x, 9);
		Assert.Equal(0, y, 9);
	}

	[Fact]
	public void ProjectHalfTurn()
	{
		var (x, y) = Polar.Project(0.5, 1, 0, 0, 100, start, clockwise: true);

		Assert.Equal(0, x, 9);
		Assert.Equal(100, y, 9);
	}

	[Fact]
	public void ProjectQuarterTurnCounterClockwise()
	{
		var (x, y) = Polar.Project(0.25, 1, 0, 0, 100, start, clockwise: false);

		Assert.Equal(-100, x, 9);
		Assert.Equal(0, y, 9);
	}

	[Theory]
	[InlineData(0.0)]
	[InlineData(0.3)]
	[InlineData(0.7)]
	public void ProjectZeroRadiusIsCenter(double cx)
	{
		var (x, y) = Polar.Project(cx, 0, 42, 17, 100, start, clockwise: true);

		Assert.Equal(42, x, 9);
		Assert.Equal(17, y, 9);
	}

	[Fact]
	public void ProjectOffsetCenterScalesRadius()
	{
		var (x, y) = Polar.Project(0.25, 0.5, 10, 20, 100, start, clockwise: true);

		Assert.Equal(60, x, 9);
		Assert.Equal(20, y, 9);
	}
}

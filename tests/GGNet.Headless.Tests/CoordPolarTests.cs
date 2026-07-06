using DiscretePosition = GGNet.Scales.DiscretePosition<double>;

namespace GGNet.Headless.Tests;

public class CoordPolarTests
{
	[Fact]
	public void DiscreteAngularSpacing()
	{
		// Arrange

		var scale = new DiscretePosition(expand: (0.0, 0.0, 0.0, 1.0));

		foreach (var key in new[] { 10.0, 20.0, 30.0, 40.0, 50.0 })
		{
			scale.Train(key);
		}

		// Act

		scale.Shape(0, 4);
		scale.Commit(true);

		// Assert

		using var _ = new AssertionScope();

		scale.Range.Should().Be((0.0, 5.0));

		for (var i = 0; i < 5; i++)
		{
			scale.Coord(i).Should().BeApproximately(i / 5.0, 1e-9);
		}
	}

	[Fact]
	public void FlipWithPolarThrows()
	{
		// Arrange

		var plot = PlotContext.Build([0, 1], o => o, o => o)
		  .Geom_Line()
		  .Flip()
		  .Coord_Polar();

		// Act

		Action act = () => plot.Init();

		// Assert

		act.Should().Throw<GGNetUserException>();
	}

	[Fact]
	public void BarWithPolarThrows()
	{
		// Arrange

		var plot = PlotContext.Build([0, 1], o => o, o => o)
		  .Geom_Bar()
		  .Coord_Polar();

		plot.Init();

		// Act

		Action act = () => plot.Render();

		// Assert

		act.Should().Throw<GGNetUserException>().WithMessage("*Bar*");
	}

	[Fact]
	public void LineWithPolarRenders()
	{
		// Arrange

		var plot = PlotContext.Build([0, 1], o => o, o => o)
		  .Geom_Line()
		  .Coord_Polar();

		plot.Init();

		// Act

		Action act = () => plot.Render();

		// Assert

		act.Should().NotThrow();
	}
}

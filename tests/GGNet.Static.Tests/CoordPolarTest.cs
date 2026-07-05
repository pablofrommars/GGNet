using GGNet.Exceptions;
using GGNet.Scales;

namespace GGNet.Static.Tests;

public class CoordPolarTest
{
  [Fact]
  public void DiscreteAngularSpacing()
  {
    var scale = new DiscretePosition<double>(expand: (0.0, 0.0, 0.0, 1.0));

    foreach (var key in new[] { 10.0, 20.0, 30.0, 40.0, 50.0 })
    {
      scale.Train(key);
    }

    scale.Shape(0, 4);
    scale.Set(true);

    Assert.Equal((0.0, 5.0), scale.Range);

    for (var i = 0; i < 5; i++)
    {
      Assert.Equal(i / 5.0, scale.Coord(i), 9);
    }
  }

  [Fact]
  public void FlipWithPolarThrows()
  {
    var plot = PlotContext.Build([0, 1], o => o, o => o)
      .Geom_Line()
      .Flip()
      .Coord_Polar();

    Assert.Throws<GGNetUserException>(() => plot.Init());
  }

  [Fact]
  public void BarWithPolarThrows()
  {
    var plot = PlotContext.Build([0, 1], o => o, o => o)
      .Geom_Bar()
      .Coord_Polar();

    plot.Init();

    var exception = Assert.Throws<GGNetUserException>(() => plot.Render(true));

    Assert.Contains("Bar", exception.Message);
  }

  [Fact]
  public void LineWithPolarRenders()
  {
    var plot = PlotContext.Build([0, 1], o => o, o => o)
      .Geom_Line()
      .Coord_Polar();

    plot.Init();
    plot.Render(true);
  }
}

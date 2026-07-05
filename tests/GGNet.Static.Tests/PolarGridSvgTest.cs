using System.Text.RegularExpressions;

namespace GGNet.Static.Tests;

public class PolarGridSvgTest
{
  public enum Metric
  {
    Speed,
    Power,
    Range,
    Weight,
    Cost
  }

  public sealed record Item(Metric Metric, double Value);

  private static readonly Item[] items =
  [
    new(Metric.Speed, 3.0),
    new(Metric.Power, 4.0),
    new(Metric.Range, 2.0),
    new(Metric.Weight, 5.0),
    new(Metric.Cost, 1.0)
  ];

  [Fact]
  public async Task RenderPolarPointPlot()
  {
    var plot = PlotContext.Build(items, i => i.Metric, i => i.Value)
      .Geom_Point()
      .Coord_Polar()
      .Style();

    var svg = await plot.AsStringAsync();

    Assert.Equal(5, Regex.Matches(svg, "class=\"x-break\"").Count);

    Assert.True(Regex.Matches(svg, "<path class=\"y-break\"").Count >= 1);

    Assert.Equal(5, Regex.Matches(svg, "class=\"x-break-label\"").Count);

    Assert.DoesNotContain("class=\"x-break-title\"", svg);
    Assert.DoesNotContain("class=\"x-title\"", svg);
  }

  [Fact]
  public async Task RenderCircleRings()
  {
    var plot = PlotContext.Build(items, i => i.Metric, i => i.Value)
      .Geom_Point()
      .Coord_Polar()
      .Style(style: Style.Default(init: s => s.Polar.Rings = PolarRingType.Circle));

    var svg = await plot.AsStringAsync();

    Assert.True(Regex.Matches(svg, "<circle class=\"y-break\"").Count >= 1);

    Assert.DoesNotContain("<path class=\"y-break\"", svg);
  }
}

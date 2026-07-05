using System.Text.RegularExpressions;

namespace GGNet.Static.Tests;

public class RadarSvgTest
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

  private static readonly Item[] widget =
  [
    new(Metric.Speed, 3.0),
    new(Metric.Power, 4.0),
    new(Metric.Range, 2.0),
    new(Metric.Weight, 5.0),
    new(Metric.Cost, 1.0)
  ];

  private static readonly Item[] gadget =
  [
    new(Metric.Speed, 4.5),
    new(Metric.Power, 2.0),
    new(Metric.Range, 4.0),
    new(Metric.Weight, 2.5),
    new(Metric.Cost, 3.5)
  ];

  [Fact]
  public async Task RenderTwoSeries()
  {
    var plot = PlotContext.Build(widget, i => i.Metric, i => i.Value)
      .Geom_Radar(tooltip: i => b => b.AddContent(0, i.Value))
      .Geom_Radar(gadget, i => i.Metric, i => i.Value, fill: "#fc9d23", tooltip: i => b => b.AddContent(0, i.Value))
      .Style();

    var svg = await plot.AsStringAsync();

    var polygons = Regex.Matches(svg, "<path d=\"([^\"]*)\"");

    Assert.Equal(2, polygons.Count);

    foreach (Match polygon in polygons)
    {
      Assert.EndsWith("Z", polygon.Groups[1].Value.Trim());
    }

    Assert.Equal(10, Regex.Matches(svg, "fill=\"transparent\"").Count);

    Assert.Equal(5, Regex.Matches(svg, "class=\"x-break\"").Count);

    Assert.Equal(5, Regex.Matches(svg, "class=\"x-break-label\"").Count);

    Assert.True(Regex.Matches(svg, "<path class=\"y-break\"").Count >= 1);
  }

  [Fact]
  public async Task ZeroBasedRadialScale()
  {
    // All values sit well above zero; the radial scale must still start at 0,
    // so a "0" break label renders at the web center.
    var plot = PlotContext.Build(widget, i => i.Metric, i => i.Value)
      .Geom_Radar()
      .Style();

    var svg = await plot.AsStringAsync();

    Assert.Matches("class=\"y-break-label\"[^>]*>0</text>", svg);
  }
}

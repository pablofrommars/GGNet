namespace GGNet.Static.Tests;

public class StaticSvgTest
{
  [Fact]
  public async Task RenderAsync()
  {
    var plot = PlotContext.Build([0, 1], o => o, o => o)
          .Title("Title")
          .Geom_Line()
          .Style();

    var value = await plot.AsStringAsync();

    Console.WriteLine(value);
  }
}

namespace GGNet.Headless.Tests;

public class SmokeSvgTest
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

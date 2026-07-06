namespace GGNet.Headless.Tests;

public class SmokeSvgTests
{
	[Fact]
	public async Task RenderAsync()
	{
		// Arrange

		var plot = PlotContext.Build([0, 1], o => o, o => o)
			  .Title("Title")
			  .Geom_Line()
			  .Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		svg.Should().Contain("<svg");
	}
}

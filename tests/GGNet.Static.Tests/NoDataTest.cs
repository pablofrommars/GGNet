using GGNet.Exceptions;

namespace GGNet.Static.Tests;

public class NoDataTest
{
	[Fact]
	public void SourcelessGeomOverloadThrows()
	{
		var plot = PlotContext.Build();

		var exception = Assert.Throws<GGNetUserException>(() => plot.Geom_Line());

		Assert.Contains("without a source", exception.Message);
	}

	[Fact]
	public async Task ExplicitSourceGeomRenders()
	{
		var items = new[] { (x: 0.0, y: 1.0), (x: 1.0, y: 2.0), (x: 2.0, y: 1.5) };

		var plot = PlotContext.Build()
		  .Geom_Line(items, i => i.x, i => i.y)
		  .Style();

		var svg = await plot.AsStringAsync();

		Assert.Contains("<path", svg);
	}
}

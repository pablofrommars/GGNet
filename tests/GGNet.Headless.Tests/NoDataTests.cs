namespace GGNet.Headless.Tests;

public class NoDataTests
{
	[Fact]
	public void SourcelessGeomOverloadThrows()
	{
		// Arrange

		var plot = PlotContext.Build();

		// Act

		Action act = () => plot.Geom_Line();

		// Assert

		act.Should().Throw<GGNetUserException>().WithMessage("*without a source*");
	}

	[Fact]
	public async Task ExplicitSourceGeomRenders()
	{
		// Arrange

		var items = new[] { (x: 0.0, y: 1.0), (x: 1.0, y: 2.0), (x: 2.0, y: 1.5) };

		var plot = PlotContext.Build()
		  .Geom_Line(items, i => i.x, i => i.y)
		  .Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		svg.Should().Contain("<path");
	}
}

namespace GGNet.Headless.Tests;

public class IPlotContextExtensionsTests
{
	private sealed record XY(double X, double Y);

	private static readonly XY[] xy = [new(1, 2.0), new(2, 3.5)];

	[Fact]
	public void PassesTheExportParameterSet()
	{
		// Arrange

		var plot = PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Point().Style();

		// Act

		var parameters = IPlotContextExtensions.Parameters(plot, 640, 480, "dotnet-interactive");

		// Assert

		using var _ = new AssertionScope();

		// Plot.RenderMode is `required`, but component activation does not enforce
		// it: an unnamed render mode silently exports as Interactive.
		parameters.Should().ContainKey("RenderMode").WhoseValue.Should().Be(RenderMode.Static);

		parameters.Keys.Should().BeEquivalentTo(["Context", "Width", "Height", "Theme", "RenderMode"]);
		parameters["Context"].Should().BeSameAs(plot);
		parameters["Width"].Should().Be(640.0);
		parameters["Height"].Should().Be(480.0);
		parameters["Theme"].Should().Be("dotnet-interactive");
	}
}

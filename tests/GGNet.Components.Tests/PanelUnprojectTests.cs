namespace GGNet.Components.Tests;

// Panel composes coord.Unproject with the scales' Invert — the end-to-end
// pixel→data seam a gesture handler will call. Verified through a real
// rendered plot so the layout zones are the production ones.
public class PanelUnprojectTests : BunitContext
{
	private sealed record P(double X, double Y);

	private static readonly P[] data =
	[
		new(1.0, 2.0),
		new(2.0, 3.5),
		new(3.0, 2.8),
		new(4.0, 4.2)
	];

	[Fact]
	public void UnprojectInvertsProject()
	{
		// Arrange

		var context = PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Point()
			.Style();

		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto));

		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;

		// Act

		var (px, py) = panel.Project(3.0, 3.5);
		var (x, y) = panel.Unproject(px, py);

		// Assert

		using var _ = new AssertionScope();

		x.Should().BeApproximately(3.0, 1e-9);
		y.Should().BeApproximately(3.5, 1e-9);
	}
}

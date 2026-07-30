using Microsoft.AspNetCore.Components.Web;

namespace GGNet.Components.Tests;

// The Tier-0 crosshair (implementation-blocks Block 6): invisible hover strips
// sample the pointer, the readout snaps through Unmap/Label, and leaving the
// panel clears it. Emitted only under the Crosshair opt-in.
public class CrosshairTests : BunitContext
{
	public CrosshairTests()
	{
		// Any opt-in initializes the interactivity module.
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	private sealed record P(double X, double Y);

	private static readonly P[] data =
	[
		new(1.0, 2.0),
		new(2.0, 3.5),
		new(3.0, 2.8),
		new(4.0, 4.2)
	];

	private static PlotContext<P, double, double> PointPlot()
		=> PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Point()
			.Style();

	private IRenderedComponent<Plot<P, double, double>> Render(PlotContext<P, double, double> context, InteractivityOptions interactivity)
		=> Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, interactivity));

	[Fact]
	public void StripsEmittedOnlyWithTheCrosshairOptIn()
	{
		// Arrange / Act

		var without = Render(PointPlot(), new InteractivityOptions());
		var with = Render(PointPlot(), new InteractivityOptions { Crosshair = true });

		// Assert

		using var _ = new AssertionScope();

		without.FindAll("rect[pointer-events=all]").Should().BeEmpty();
		with.FindAll("rect[pointer-events=all]").Count.Should().BeGreaterThan(10);
	}

	[Fact]
	public async Task HoverShowsTheReadout()
	{
		// Arrange

		var cut = Render(PointPlot(), new InteractivityOptions { Crosshair = true });

		var strips = cut.FindAll("rect[pointer-events=all]");

		// Act

		await strips[strips.Count / 2].TriggerEventAsync("onmouseover", new MouseEventArgs());

		// Assert

		using var _ = new AssertionScope();

		cut.FindAll("line.crosshair").Should().ContainSingle();
		cut.Find("text.crosshair-label").TextContent.Trim().Should().MatchRegex("^[0-9]+(\\.[0-9]+)?$");
	}

	[Fact]
	public async Task LeavingThePanelClearsTheCrosshair()
	{
		// Arrange

		var cut = Render(PointPlot(), new InteractivityOptions { Crosshair = true });

		await cut.FindAll("rect[pointer-events=all]")[3].TriggerEventAsync("onmouseover", new MouseEventArgs());

		// Act

		await cut.Find("svg > g").TriggerEventAsync("onmouseleave", new MouseEventArgs());

		// Assert

		cut.FindAll("line.crosshair").Should().BeEmpty();
	}
}

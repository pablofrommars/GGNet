using Microsoft.AspNetCore.Components.Web;

namespace GGNet.Components.Tests;

public class CrosshairKeyboardTests : BunitContext
{
	public sealed record P(double X, double Y);

	private static readonly P[] dense = [new(0, 1), new(10, 2), new(20, 3), new(30, 4)];

	private static readonly P[] sparse = [new(4, 9), new(26, 8)];

	private readonly List<Inspection<double>?> positions = [];

	public CrosshairKeyboardTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	private IRenderedComponent<Plot<P, double, double>> Mount(bool crosshair = true, bool inspected = true, Inspection<double>? inspection = null)
	{
		var context = PlotContext.Build(dense, i => i.X, i => i.Y)
			.Geom_Line(inspect: inspected ? new("Dense", i => "d") : null)
			.Geom_Point(sparse, i => i.X, i => i.Y, inspect: inspected ? new("Sparse", i => "s") : null)
			.Style();

		return Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, new InteractivityOptions { Crosshair = crosshair })
			.Add(p => p.Inspection, inspection)
			.Add(p => p.InspectionChanged, positions.Add));
	}

	private static Task PressAsync(IRenderedComponent<Plot<P, double, double>> cut, string key)
		=> cut.Find("div.ggnet").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = key });

	[Fact]
	public void ThePlotTakesFocusOnlyUnderCrosshair()
	{
		// Arrange

		// Act

		var with = Mount();
		var without = Mount(crosshair: false);

		// Assert

		using var _ = new AssertionScope();

		with.Find("div.ggnet").GetAttribute("tabindex").Should().Be("0");
		without.Find("div.ggnet").HasAttribute("tabindex").Should().BeFalse();
	}

	[Fact]
	public async Task RightAndLeftStepThroughTheSampleTimesOfEverySeries()
	{
		// Arrange

		var cut = Mount(inspection: new(10));

		// Act

		await PressAsync(cut, "ArrowRight");
		await PressAsync(cut, "ArrowRight");
		await PressAsync(cut, "ArrowRight");
		await PressAsync(cut, "ArrowLeft");
		await PressAsync(cut, "ArrowLeft");
		await PressAsync(cut, "ArrowLeft");
		await PressAsync(cut, "ArrowLeft");

		// Assert

		positions.Should().Equal(new(20), new(26), new(30), new(26), new(20), new(10), new(4));
	}

	[Fact]
	public async Task SteppingPastTheLastSampleStaysOnIt()
	{
		// Arrange

		var cut = Mount(inspection: new(30));

		// Act

		await PressAsync(cut, "ArrowRight");

		// Assert

		using var _ = new AssertionScope();

		positions.Should().BeEmpty();
		cut.Instance.Readings!.Inspection.Should().Be(new Inspection<double>(30));
	}

	[Fact]
	public async Task FromBetweenSamplesAStepLandsOnTheNeighbour()
	{
		// Arrange

		var cut = Mount(inspection: new(12.5));

		// Act

		await PressAsync(cut, "ArrowLeft");

		// Assert

		positions.Should().Equal(new Inspection<double>(10));
	}

	[Fact]
	public async Task WithNothingInspectedTheFirstStepEntersFromTheEdgeOfTheView()
	{
		// Arrange

		var cut = Mount();

		// Act

		await PressAsync(cut, "ArrowLeft");

		// Assert

		positions.Should().Equal(new Inspection<double>(30));
	}

	[Fact]
	public async Task EscapeClears()
	{
		// Arrange

		var cut = Mount(inspection: new(10));

		// Act

		await PressAsync(cut, "Escape");

		// Assert

		using var _ = new AssertionScope();

		positions.Should().Equal([null]);
		cut.Find("[data-ggnet-cursor]").GetAttribute("visibility").Should().Be("hidden");
	}

	[Fact]
	public async Task TheArrowsStepThroughPlottedSamplesWithoutAReadout()
	{
		// Arrange

		var cut = Mount(inspected: false, inspection: new(10));

		// Act

		await PressAsync(cut, "ArrowRight");
		await PressAsync(cut, "ArrowRight");
		await PressAsync(cut, "ArrowLeft");
		await PressAsync(cut, "Enter");

		// Assert

		using var _ = new AssertionScope();

		positions.Should().Equal(new Inspection<double>(20), new Inspection<double>(26), new Inspection<double>(20));
		cut.Instance.Readings!.Values.Should().BeEmpty("stepping needs no readout, and adds none");
	}

	[Fact]
	public async Task LayersThatAreNotSampledSeriesOfferNoSteps()
	{
		// Arrange

		var context = PlotContext.Build(dense, i => i.X, i => i.Y)
			.Geom_Ribbon(ymin: i => i.Y - 1, ymax: i => i.Y + 1)
			.Style();
		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, new InteractivityOptions { Crosshair = true })
			.Add(p => p.Inspection, new Inspection<double>(10))
			.Add(p => p.InspectionChanged, positions.Add));

		// Act

		await PressAsync(cut, "ArrowRight");

		// Assert

		positions.Should().BeEmpty();
	}
}

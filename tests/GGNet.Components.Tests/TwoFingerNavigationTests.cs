using System.Globalization;

namespace GGNet.Components.Tests;

// The browser module owns the two-finger gesture; bUnit pins the arithmetic of
// its single commit: where the fingers' midpoint started, where it ended, and
// how far the fingers spread.
public class TwoFingerNavigationTests : BunitContext
{
	public sealed record P(double X, double Y);

	private static readonly P[] data = [new(0, 1), new(25, 9), new(50, 2), new(75, 4), new(100, 3)];

	private readonly List<ViewChange<double, double>> changes = [];

	private readonly List<Inspection<double>?> positions = [];

	public TwoFingerNavigationTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	private IRenderedComponent<Plot<P, double, double>> Mount(InteractivityOptions interactivity, Inspection<double>? inspection = null)
	{
		var context = PlotContext.Build(data, i => i.X, i => i.Y).Geom_Point().Style();

		context.SetXView(0, 100);

		return Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, interactivity)
			.Add(p => p.Inspection, inspection)
			.Add(p => p.InspectionChanged, positions.Add)
			.Add(p => p.ViewChanged, changes.Add));
	}

	private static double At(IRenderedComponent<Panel<P, double, double>> panel, double fraction)
	{
		var area = panel.Find("rect.panel");

		return double.Parse(area.GetAttribute("x")!, CultureInfo.InvariantCulture)
			+ fraction * double.Parse(area.GetAttribute("width")!, CultureInfo.InvariantCulture);
	}

	[Theory]
	[InlineData(0.5, 0.3, 1.0, 20.0, 120.0)]
	[InlineData(0.5, 0.5, 2.0, 25.0, 75.0)]
	[InlineData(0.5, 0.5, 0.5, -50.0, 150.0)]
	[InlineData(0.25, 0.75, 2.0, -12.5, 37.5)]
	public async Task TheValueUnderTheMidpointStaysUnderItWhileTheWindowScales(double from, double to, double scale, double min, double max)
	{
		// Arrange

		var cut = Mount(new() { Crosshair = true, Pan = true });
		var panel = cut.FindComponent<Panel<P, double, double>>();

		// Act

		await cut.InvokeAsync(() => panel.Instance.OnNavigateEndAsync(At(panel, from), At(panel, to), scale));

		// Assert

		var change = changes.Should().ContainSingle().Subject;

		using var _ = new AssertionScope();

		change.Cause.Should().Be(ViewChangeCause.Gesture);
		change.X!.Value.min.Should().BeApproximately(min, 1e-6);
		change.X!.Value.max.Should().BeApproximately(max, 1e-6);
	}

	[Fact]
	public async Task ThePinKeepsItsValueAndFollowsTheWindow()
	{
		// Arrange

		var cut = Mount(new() { Crosshair = true, Pan = true }, new(50));
		var panel = cut.FindComponent<Panel<P, double, double>>();
		var before = cut.Find("line.crosshair").GetAttribute("x1");

		// Act

		await cut.InvokeAsync(() => panel.Instance.OnNavigateEndAsync(At(panel, 0.5), At(panel, 0.3), 1.0));

		// Assert

		using var _ = new AssertionScope();

		positions.Should().BeEmpty();
		cut.Instance.Readings!.Inspection.Should().Be(new Inspection<double>(50));
		cut.Find("line.crosshair").GetAttribute("x1").Should().Be(Fixed(At(panel, 0.3))).And.NotBe(before);
	}

	[Theory]
	[InlineData(true, false, ZoomAxis.X)]
	[InlineData(false, true, ZoomAxis.X)]
	[InlineData(true, true, ZoomAxis.Y)]
	public async Task WithoutCrosshairPanAndAnXAxisToDriveNothingIsCommitted(bool crosshair, bool pan, ZoomAxis zoom)
	{
		// Arrange

		var cut = Mount(new() { Crosshair = crosshair, Pan = pan, Zoom = zoom });
		var panel = cut.FindComponent<Panel<P, double, double>>();

		// Act

		await cut.InvokeAsync(() => panel.Instance.OnNavigateEndAsync(At(panel, 0.5), At(panel, 0.3), 2.0));

		// Assert

		changes.Should().BeEmpty();
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(0.0)]
	[InlineData(-1.0)]
	[InlineData(double.PositiveInfinity)]
	public async Task AMeaninglessScaleIsIgnored(double scale)
	{
		// Arrange

		var cut = Mount(new() { Crosshair = true, Pan = true });
		var panel = cut.FindComponent<Panel<P, double, double>>();

		// Act

		await cut.InvokeAsync(() => panel.Instance.OnNavigateEndAsync(At(panel, 0.5), At(panel, 0.5), scale));

		// Assert

		changes.Should().BeEmpty();
	}

	[Fact]
	public async Task AutoFitYRefitsInTheSameChange()
	{
		// Arrange

		var cut = Mount(new() { Crosshair = true, Pan = true, AutoFitY = true });
		var panel = cut.FindComponent<Panel<P, double, double>>();

		// Act — zoom onto the stretch around x = 25, where y peaks at 9.

		await cut.InvokeAsync(() => panel.Instance.OnNavigateEndAsync(At(panel, 0.25), At(panel, 0.5), 5.0));

		// Assert

		var change = changes.Should().ContainSingle().Subject;

		using var _ = new AssertionScope();

		change.X!.Value.Should().Be((15.0, 35.0));
		change.Y.Should().NotBeNull();
		change.Y!.Value.max.Should().BeGreaterThanOrEqualTo(9.0).And.BeLessThan(11.0);
	}

	private static string Fixed(double value)
		=> value.ToString("0.###############", CultureInfo.InvariantCulture);
}

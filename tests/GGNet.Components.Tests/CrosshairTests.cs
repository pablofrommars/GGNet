using System.Globalization;

using AngleSharp.Dom;

using Microsoft.AspNetCore.Components;

namespace GGNet.Components.Tests;

// The crosshair is drawn by the server from one x value held by the plot, in
// every panel sharing the x scale. The browser module moves it between frames;
// bUnit drives the two handlers that module calls.
public class CrosshairTests : BunitContext
{
	private const string ModulePath = "./_content/GGNet/Components/Panel.razor.js";

	public sealed record P(double X, double Y, string Group = "a");

	private enum Phase
	{
		Lag,
		Growth,
		Rest
	}

	private static readonly P[] data = [new(0, 2.0), new(10, 3.5), new(20, 2.8), new(40, 4.2)];

	private static readonly InteractivityOptions crosshair = new() { Crosshair = true };

	private readonly List<Inspection<double>?> positions = [];

	public CrosshairTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	private static PlotContext<P, double, double> PointPlot()
		=> PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Point()
			.Style();

	private static PlotContext<P, double, double> Stacked()
		=> PlotContext.Build(data, i => i.X, i => i.Y)
			.Panel(panel => panel.Geom_Line().YLab("first"))
			.Panel(panel => panel.Geom_Point(y: i => i.Y * 1000).YLab("second, wider labels"))
			.Panel(panel => panel.Geom_Area())
			.Style();

	private IRenderedComponent<Plot<P, double, double>> Mount(PlotContext<P, double, double> context, InteractivityOptions? interactivity = null)
		=> Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, interactivity ?? crosshair)
			.Add(p => p.InspectionChanged, positions.Add));

	private static double Number(IElement element, string attribute)
		=> double.Parse(element.GetAttribute(attribute)!, CultureInfo.InvariantCulture);

	private static (double x, double width) Area<T, TX, TY>(IRenderedComponent<Panel<T, TX, TY>> panel)
		where TX : struct
		where TY : struct
	{
		var area = panel.Find("rect.panel");

		return (Number(area, "x"), Number(area, "width"));
	}

	private static double At<T, TX, TY>(IRenderedComponent<Panel<T, TX, TY>> panel, double fraction)
		where TX : struct
		where TY : struct
	{
		var (x, width) = Area(panel);

		return x + fraction * width;
	}

	private static double Fraction<T, TX, TY>(IRenderedComponent<Panel<T, TX, TY>> panel)
		where TX : struct
		where TY : struct
	{
		var (x, width) = Area(panel);

		return (Number(panel.Find("line.crosshair"), "x1") - x) / width;
	}

	private static bool Shown<TComponent>(IRenderedComponent<TComponent> panel)
		where TComponent : IComponent
		=> !panel.Find("[data-ggnet-cursor]").HasAttribute("visibility");

	[Fact]
	public void TheCursorIsEmittedOnlyWithTheCrosshairOptInAndStartsHidden()
	{
		// Arrange

		var plain = new InteractivityOptions();

		// Act

		var without = Mount(PointPlot(), plain);
		var with = Mount(PointPlot());

		// Assert

		using var _ = new AssertionScope();

		without.FindAll("[data-ggnet-cursor]").Should().BeEmpty();
		with.FindAll("[data-ggnet-cursor]").Should().ContainSingle().Which.GetAttribute("visibility").Should().Be("hidden");
		with.FindAll("rect[pointer-events=all]").Should().BeEmpty("the hover strips and their round trip per strip are gone");
	}

	[Fact]
	public void TheModuleIsToldTheCursorIsSharedAndContinuous()
	{
		// Arrange

		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("initialize", _ => true).SetVoidResult();

		// Act

		var cut = Mount(PointPlot());

		// Assert

		cut.WaitForAssertion(() => module.Invocations.Should().ContainSingle(i => i.Identifier == "initialize"));

		module.Invocations.Single(i => i.Identifier == "initialize").Arguments[^1]
			.Should().BeEquivalentTo(new { Cursor = "shared", CursorContinuous = true, Crosshair = true });
	}

	[Fact]
	public async Task AHoverPositionShowsAnUnpinnedLineWithItsLabel()
	{
		// Arrange

		var cut = Mount(PointPlot());
		var panel = cut.FindComponent<Panel<P, double, double>>();

		// Act

		await cut.InvokeAsync(() => panel.Instance.OnCursorAsync(At(panel, 0.5), false));

		// Assert

		using var _ = new AssertionScope();

		Shown(panel).Should().BeTrue();
		Fraction(panel).Should().BeApproximately(0.5, 1e-6);
		cut.Find("line.crosshair").ClassList.Should().NotContain("pinned");
		cut.Find("[data-ggnet-cursor]").HasAttribute("data-ggnet-pinned").Should().BeFalse();
		cut.Find("text.crosshair-label").TextContent.Trim().Should().MatchRegex("^[0-9]+(\\.[0-9]+)?$");
		positions.Should().ContainSingle().Which!.Pinned.Should().BeFalse();
	}

	[Fact]
	public async Task LeavingClearsAnUnpinnedLine()
	{
		// Arrange

		var cut = Mount(PointPlot());
		var panel = cut.FindComponent<Panel<P, double, double>>();
		await cut.InvokeAsync(() => panel.Instance.OnCursorAsync(At(panel, 0.5), false));

		// Act

		await cut.InvokeAsync(() => panel.Instance.OnCursorLeaveAsync());

		// Assert

		using var _ = new AssertionScope();

		Shown(panel).Should().BeFalse();
		positions.Should().HaveCount(2).And.HaveElementAt(1, null);
	}

	[Fact]
	public async Task LeavingKeepsAPinnedLine()
	{
		// Arrange

		var cut = Mount(PointPlot());
		var panel = cut.FindComponent<Panel<P, double, double>>();
		await cut.InvokeAsync(() => panel.Instance.OnCursorAsync(At(panel, 0.25), true));

		// Act

		await cut.InvokeAsync(() => panel.Instance.OnCursorLeaveAsync());

		// Assert

		using var _ = new AssertionScope();

		Shown(panel).Should().BeTrue();
		cut.Find("line.crosshair").ClassList.Should().Contain("pinned");
		cut.Find("[data-ggnet-cursor]").HasAttribute("data-ggnet-pinned").Should().BeTrue();
		positions.Should().ContainSingle().Which!.Pinned.Should().BeTrue();
	}

	[Fact]
	public async Task APositionOutsideThePlotAreaIsIgnored()
	{
		// Arrange

		var cut = Mount(PointPlot());
		var panel = cut.FindComponent<Panel<P, double, double>>();

		// Act

		await cut.InvokeAsync(() => panel.Instance.OnCursorAsync(At(panel, 1.2), true));

		// Assert

		using var _ = new AssertionScope();

		Shown(panel).Should().BeFalse();
		positions.Should().BeEmpty();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public async Task APositionInAnyPanelIsDrawnInEveryPanelAtTheSameValue(int origin)
	{
		// Arrange

		var cut = Mount(Stacked());
		var panels = cut.FindComponents<Panel<P, double, double>>();

		// Act

		await cut.InvokeAsync(() => panels[origin].Instance.OnCursorAsync(At(panels[origin], 0.3), true));

		// Assert

		using var _ = new AssertionScope();

		panels.Should().HaveCount(3).And.OnlyContain(panel => Shown(panel));
		panels.Select(Fraction).Should().OnlyContain(fraction => Math.Abs(fraction - 0.3) < 1e-6);
		cut.FindAll("line.crosshair").Should().HaveCount(3);
		positions.Should().ContainSingle();
	}

	[Fact]
	public void EachLineIsClippedToItsOwnPlotArea()
	{
		// Arrange

		var context = Stacked();

		// Act

		var cut = Mount(context);

		// Assert

		var clips = cut.FindAll("[data-ggnet-cursor]").Select(cursor => cursor.ParentElement!.GetAttribute("clip-path")).ToArray();

		using var _ = new AssertionScope();

		clips.Should().HaveCount(3).And.OnlyHaveUniqueItems();
		clips.Should().OnlyContain(clip => clip!.EndsWith("-area)", StringComparison.Ordinal));
	}

	[Fact]
	public void ASampleMarkerSitsAtTheSampleNotAtTheCursor()
	{
		// Arrange

		var context = PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Line(inspect: new("Series", i => "v", InspectPolicy.Previous))
			.Style();
		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, crosshair)
			.Add(p => p.Inspection, new Inspection<double>(17)));
		var between = (line: Number(cut.Find("line.crosshair"), "x1"), marker: Number(cut.Find("circle.crosshair-marker"), "cx"));

		// Act

		cut.Render(parameters => parameters.Add(p => p.Inspection, new Inspection<double>(10)));

		// Assert

		var onSample = Number(cut.Find("line.crosshair"), "x1");

		using var _ = new AssertionScope();

		between.marker.Should().BeApproximately(onSample, 1e-6);
		between.line.Should().BeGreaterThan(between.marker + 1);
		cut.FindAll("circle.crosshair-marker").Should().ContainSingle();
	}

	[Fact]
	public void AMissingReadingDrawsNoMarker()
	{
		// Arrange

		var context = PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Line(inspect: new("Series", i => "v") { MaxDistance = 1 })
			.Style();

		// Act

		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, crosshair)
			.Add(p => p.Inspection, new Inspection<double>(30)));

		// Assert

		using var _ = new AssertionScope();

		cut.FindAll("line.crosshair").Should().ContainSingle();
		cut.FindAll("circle.crosshair-marker").Should().BeEmpty();
	}

	[Fact]
	public async Task OnADiscreteAxisTheLineSnapsToTheCategoryUnderThePointer()
	{
		// Arrange

		(Phase Phase, double Value)[] rows = [(Phase.Lag, 1), (Phase.Growth, 2), (Phase.Rest, 3)];
		var context = PlotContext.Build(rows, row => row.Phase, row => row.Value).Geom_Bar().Style();
		var cut = Render<Plot<(Phase Phase, double Value), Phase, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, crosshair));
		var panel = cut.FindComponent<Panel<(Phase Phase, double Value), Phase, double>>();

		// Act

		await cut.InvokeAsync(() => panel.Instance.OnCursorAsync(At(panel, 0.45), false));
		var left = Fraction(panel);
		await cut.InvokeAsync(() => panel.Instance.OnCursorAsync(At(panel, 0.55), false));
		var right = Fraction(panel);

		// Assert

		using var _ = new AssertionScope();

		left.Should().BeApproximately(0.5, 0.02);
		right.Should().BeApproximately(left, 1e-9);
		cut.Find("text.crosshair-label").TextContent.Trim().Should().Be("Growth");
	}

	[Fact]
	public async Task WithFreeXFacetsTheLineStaysInItsPanelAndNeverPins()
	{
		// Arrange

		P[] faceted = [new(0, 1, "a"), new(10, 2, "a"), new(100, 3, "b"), new(200, 4, "b")];
		var context = PlotContext.Build(faceted, i => i.X, i => i.Y)
			.Geom_Point()
			.Facet_Wrap(i => i.Group, freeX: true)
			.Style();
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("initialize", _ => true).SetVoidResult();
		var cut = Mount(context);
		var panels = cut.FindComponents<Panel<P, double, double>>();

		// Act

		await cut.InvokeAsync(() => panels[1].Instance.OnCursorAsync(At(panels[1], 0.5), true));
		var shown = panels.Select(panel => Shown(panel)).ToArray();
		var pinned = panels[1].Find("[data-ggnet-cursor]").HasAttribute("data-ggnet-pinned");
		await cut.InvokeAsync(() => panels[1].Instance.OnCursorLeaveAsync());

		// Assert

		using var _ = new AssertionScope();

		shown.Should().Equal(false, true);
		pinned.Should().BeFalse();
		Shown(panels[1]).Should().BeFalse();
		positions.Should().BeEmpty();
		module.Invocations.Where(i => i.Identifier == "initialize").Select(i => i.Arguments[^1])
			.Should().AllBeEquivalentTo(new { Cursor = "local", CursorContinuous = false });
	}

	[Fact]
	public async Task ASingleFreeXFacetIsStillLocalAndNeverPins()
	{
		// Arrange — one facet leaves one x scale, which must not make the plot share.

		P[] faceted = [new(0, 1, "a"), new(10, 2, "a")];
		var context = PlotContext.Build(faceted, i => i.X, i => i.Y)
			.Geom_Point()
			.Facet_Wrap(i => i.Group, freeX: true)
			.Style();
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("initialize", _ => true).SetVoidResult();
		var cut = Mount(context);
		var panel = cut.FindComponent<Panel<P, double, double>>();

		// Act

		await cut.InvokeAsync(() => panel.Instance.OnCursorAsync(At(panel, 0.5), true));

		// Assert

		using var _ = new AssertionScope();

		Shown(panel).Should().BeTrue();
		panel.Find("[data-ggnet-cursor]").HasAttribute("data-ggnet-pinned").Should().BeFalse();
		positions.Should().BeEmpty();
		module.Invocations.Single(i => i.Identifier == "initialize").Arguments[^1]
			.Should().BeEquivalentTo(new { Cursor = "local", CursorContinuous = false });
	}
}

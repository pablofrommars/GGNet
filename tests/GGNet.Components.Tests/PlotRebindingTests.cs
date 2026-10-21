using System.Text.RegularExpressions;

using GGNet.Headless;

namespace GGNet.Components.Tests;

public class PlotRebindingTests : BunitContext
{
	private sealed record P(double X, double Y);

	private static PlotContext<P, double, double> Context(double start, bool polar = false)
	{
		var context = PlotContext.Build(new[] { new P(start, start), new P(start + 1, start + 1) }, p => p.X, p => p.Y)
			.Geom_Point(tooltip: p => builder => builder.AddContent(0, p.X))
			.Title("A title").XLab("x").YLab("y");
		if (polar)
		{
			context.Coord_Polar();
		}

		return context.Style();
	}

	private IRenderedComponent<Plot<P, double, double>> Mount(PlotContext<P, double, double> context,
		RenderMode mode = RenderMode.InteractiveAuto, double width = 720, double height = 576)
		=> Render<Plot<P, double, double>>(p => p.Add(x => x.Context, context).Add(x => x.RenderMode, mode)
			.Add(x => x.Width, width).Add(x => x.Height, height));

	private static string Normalize(string markup, string id)
		=> Regex.Replace(markup.Replace(id, "ID"), "blazor:on\\w+=\"\\d+\"", "blazor:evt");

	[Theory]
	[InlineData(RenderMode.InteractiveAuto)]
	[InlineData(RenderMode.Interactive)]
	public async Task ExportedContextCanReplaceAnExistingPlotAndSwitchBack(RenderMode mode)
	{
		// Arrange

		var a = Context(0);
		var b = Context(100);
		await b.AsStringAsync();
		var expectedB = await b.AsStringAsync();
		var cut = Mount(a, mode);
		var baseline = Normalize(cut.Markup, a.Id);
		var oldPanel = cut.FindComponent<Panel<P, double, double>>().Instance;

		// Act

		cut.Render(p => p.Add(x => x.Context, b));
		cut.WaitForAssertion(() => cut.Find("svg").Id.Should().Be(b.Id));
		var replacement = cut.FindComponent<Panel<P, double, double>>().Instance;
		await cut.InvokeAsync(() => cut.Instance.ZoomToXAsync(100, 100.5));
		await cut.InvokeAsync(() => cut.Instance.ResetViewAsync());
		cut.Render(p => p.Add(x => x.Context, a));

		// Assert

		cut.WaitForAssertion(() => Normalize(cut.Markup, a.Id).Should().Be(baseline));
		using var _ = new AssertionScope();
		replacement.Should().NotBeSameAs(oldPanel);
		(await b.AsStringAsync()).Should().Be(expectedB);
	}

	[Fact]
	public async Task ReplacementMatchesFreshLayoutAndRoutesTooltips()
	{
		// Arrange

		var b = Context(100);
		await b.AsStringAsync();
		var fresh = Mount(Context(100));
		var cut = Mount(Context(0));

		// Act

		cut.Render(p => p.Add(x => x.Context, b));
		var actual = Normalize(cut.Markup, b.Id);
		await cut.Find("circle").MouseOverAsync(new());

		// Assert

		using var _ = new AssertionScope();
		actual.Should().Be(Normalize(fresh.Markup, fresh.Instance.Context.Id));
		cut.Markup.Should().Contain("100");
		cut.Find("foreignObject .bubble").TextContent.Should().Be("100");
	}

	[Theory]
	[InlineData(RenderMode.InteractiveAuto, 1200, 576)]
	[InlineData(RenderMode.InteractiveAuto, 720, 900)]
	[InlineData(RenderMode.Interactive, 1200, 900)]
	public void DimensionUpdatesMatchFreshLayoutAndPreserveTheView(RenderMode mode, double width, double height)
	{
		// Arrange

		var context = Context(0);
		context.SetXView(0, 0.5);
		var cut = Mount(context, mode);
		var expectedContext = Context(0);
		expectedContext.SetXView(0, 0.5);
		var fresh = Mount(expectedContext, width: width, height: height);

		// Act

		cut.Render(p => p.Add(x => x.Width, width).Add(x => x.Height, height));

		// Assert

		cut.WaitForAssertion(() => Normalize(cut.Markup, context.Id)
			.Should().Be(Normalize(fresh.Markup, expectedContext.Id)));
	}

	[Fact]
	public void StaticPlotKeepsItsInitialFrame()
	{
		// Arrange

		var cut = Mount(Context(0), RenderMode.Static);
		var initial = cut.Markup;

		// Act

		cut.Render(p => p.Add(x => x.Width, 1200).Add(x => x.Context, Context(100)));

		// Assert

		cut.Markup.Should().Be(initial);
	}

	[Fact]
	public void ReplacingCartesianWithPolarDisposesAndRecreatesInterop()
	{
		// Arrange

		var module = JSInterop.SetupModule("./_content/GGNet/Components/Panel.razor.js");
		module.SetupVoid("initialize", _ => true).SetVoidResult();
		module.SetupVoid("dispose", _ => true).SetVoidResult();
		var cut = Render<Plot<P, double, double>>(p => p
			.Add(x => x.Context, Context(0)).Add(x => x.RenderMode, RenderMode.InteractiveAuto)
			.Add(x => x.Interactivity, new InteractivityOptions { Pan = true }));

		// Act

		cut.Render(p => p.Add(x => x.Context, Context(0, polar: true)));
		cut.Render(p => p.Add(x => x.Context, Context(100)));

		// Assert

		cut.WaitForAssertion(() =>
		{
			using var _ = new AssertionScope();
			module.Invocations.Count(i => i.Identifier == "initialize").Should().Be(2);
			module.Invocations.Count(i => i.Identifier == "dispose").Should().Be(1);
		});
	}
}

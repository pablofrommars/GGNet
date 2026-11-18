using Microsoft.AspNetCore.Components.Web;

namespace GGNet.Components.Tests;

public class PanelActivationTests : BunitContext
{
	private sealed record P(double X, double Y);

	private static readonly P[] data =
	[
		new(1.0, 2.0),
		new(2.0, 3.5),
		new(3.0, 2.8),
		new(4.0, 4.2)
	];

	// Longer than the wait for a possible second click, so "nothing fired" is a real observation.
	private static readonly TimeSpan settled = TimeSpan.FromMilliseconds(700);

	private readonly List<PanelActivation> activations = [];

	public PanelActivationTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	private Task Activated(PanelActivation activation)
	{
		activations.Add(activation);

		return Task.CompletedTask;
	}

	private PlotContext<P, double, double> Stacked()
		=> PlotContext.Build(data, i => i.X, i => i.Y)
			.Panel(p => p.Geom_Point(), height: 0.5, onActivate: Activated, key: "top")
			.Panel(p => p.Geom_Point(), height: 0.3, onActivate: Activated)
			.Panel(p => p.Geom_Point(), height: 0.2, onActivate: Activated, key: "bottom")
			.Style();

	private PlotContext<P, double, double> Single()
		=> PlotContext.Build(data, i => i.X, i => i.Y)
			.Panel(p => p.Geom_Point(), onActivate: Activated, key: "only")
			.Style();

	private IRenderedComponent<Plot<P, double, double>> Render(PlotContext<P, double, double> context, InteractivityOptions? interactivity = null)
		=> Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, interactivity));

	[Theory]
	[InlineData("circle")]
	[InlineData("line")]
	[InlineData("rect.panel")]
	public void AClickAnywhereInThePlotAreaActivatesAtOnce(string target)
	{
		// Arrange

		var cut = Render(Single());

		// Act

		cut.Find($"[data-ggnet-activate] {target}").Click();

		// Assert

		activations.Should().ContainSingle().Which.Should().Be(new PanelActivation(0, "only"));
	}

	[Fact]
	public void TheActivationNamesItsSubPanel()
	{
		// Arrange

		var cut = Render(Stacked());

		var backgrounds = cut.FindAll("rect.panel");

		// Act

		backgrounds[2].Click();
		backgrounds[1].Click();
		backgrounds[0].Click();

		// Assert

		activations.Should().Equal(
			new PanelActivation(2, "bottom"),
			new PanelActivation(1, null),
			new PanelActivation(0, "top"));
	}

	[Fact]
	public void AMarkHandlerRunsBeforeTheActivation()
	{
		// Arrange

		var order = new List<string>();

		var context = PlotContext.Build(data, i => i.X, i => i.Y)
			.Panel(
				p => p.Geom_Point(onclick: (_, _) =>
				{
					order.Add("mark");

					return Task.CompletedTask;
				}),
				onActivate: _ =>
				{
					order.Add("panel");

					return Task.CompletedTask;
				})
			.Style();

		var cut = Render(context);

		// Act

		cut.Find("circle").Click();

		// Assert

		order.Should().Equal("mark", "panel");
	}

	[Fact]
	public void WithoutTheCallbackNoActivationTargetIsRendered()
	{
		// Arrange

		var context = PlotContext.Build(data, i => i.X, i => i.Y)
			.Panel(p => p.Geom_Point())
			.Style();

		// Act

		var cut = Render(context);

		// Assert

		cut.FindAll("[data-ggnet-activate]").Should().BeEmpty();
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UnderInteractivityAClickActivatesOnceItCannotBeAReset(bool pan)
	{
		// Arrange

		var cut = Render(Single(), new InteractivityOptions { Pan = pan });

		// Act

		var click = cut.Find("[data-ggnet-activate] circle").ClickAsync(new MouseEventArgs { Detail = 1 });

		var immediate = activations.Count;

		await click;

		// Assert

		using var _ = new AssertionScope();

		immediate.Should().Be(0);
		activations.Should().ContainSingle().Which.Should().Be(new PanelActivation(0, "only"));
	}

	[Fact]
	public async Task AResetGestureCancelsThePendingActivation()
	{
		// Arrange

		var context = Single();
		var cut = Render(context, new InteractivityOptions());

		await cut.InvokeAsync(() => cut.Instance.ZoomToXAsync(2.0, 3.0));

		// Act

		var first = cut.Find("rect.panel").ClickAsync(new MouseEventArgs { Detail = 1 });
		var second = cut.Find("rect.panel").ClickAsync(new MouseEventArgs { Detail = 2 });
		await cut.Find("g[style*='user-select']").DoubleClickAsync(new MouseEventArgs { Detail = 2 });

		await Task.WhenAll(first, second);
		await Task.Delay(settled, Xunit.TestContext.Current.CancellationToken);

		// Assert

		using var _ = new AssertionScope();

		activations.Should().BeEmpty();
		context.XView.Should().BeNull();
	}

	[Fact]
	public async Task TheSecondClickOfADoubleClickNeverActivates()
	{
		// Arrange

		var cut = Render(Single(), new InteractivityOptions());

		// Act

		await cut.Find("rect.panel").ClickAsync(new MouseEventArgs { Detail = 2 });
		await Task.Delay(settled, Xunit.TestContext.Current.CancellationToken);

		// Assert

		activations.Should().BeEmpty();
	}

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public async Task UnderAnInspectionModeClicksNeverActivate(bool cursorTooltip, bool crosshair)
	{
		// Arrange

		var cut = Render(Single(), new InteractivityOptions { CursorTooltip = cursorTooltip, Crosshair = crosshair });

		// Act

		await cut.Find("rect.panel").ClickAsync(new MouseEventArgs { Detail = 1 });
		await Task.Delay(settled, Xunit.TestContext.Current.CancellationToken);

		// Assert

		using var _ = new AssertionScope();

		activations.Should().BeEmpty();
		cut.FindAll("[data-ggnet-activate]").Should().BeEmpty();
	}
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task NativeActionsUseThePanelCallbackAndResetRetainsThePin(bool reset)
	{
		// Arrange

		var context = Single();
		var cut = Render(context, new() { Crosshair = true, Touch = new() { Inspect = true, TapActivatesPanel = true, DoubleTapResetsView = true } });
		await cut.InvokeAsync(() => cut.Instance.ZoomToXAsync(2, 3));
		cut.Render(p => p.Add(plot => plot.Inspection, new Inspection<double>(2.5)));
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var stamp = cut.Find("[data-ggnet-inspection]").GetAttribute("data-ggnet-inspection")!.Split(' ');
		var epoch = long.Parse(stamp[1], System.Globalization.CultureInfo.InvariantCulture);

		// Act

		await cut.InvokeAsync(() => panel.OnTouchActionAsync(stamp[0], epoch, 1, reset));
		await cut.InvokeAsync(() => panel.OnTouchActionAsync(stamp[0], epoch, 1, reset));

		// Assert

		using var _ = new AssertionScope();
		activations.Should().HaveCount(reset ? 0 : 1);
		cut.Instance.Readings!.Inspection.Should().Be(new Inspection<double>(2.5));
		context.XView.Should().Be(reset ? null : (2.0, 3.0));
		cut.FindAll("[data-ggnet-activate]").Should().BeEmpty("native touch does not dispatch mouse or mark clicks");
	}

	[Theory]
	[InlineData("clear")]
	[InlineData("view")]
	[InlineData("context")]
	[InlineData("dispose")]
	public async Task HostChangesRejectStaleNativeActions(string command)
	{
		// Arrange

		var cut = Render(Single(), new() { Crosshair = true, Touch = new() { TapActivatesPanel = true, DoubleTapResetsView = true } });
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var stamp = cut.Find("[data-ggnet-inspection]").GetAttribute("data-ggnet-inspection")!.Split(' ');
		var epoch = long.Parse(stamp[1], System.Globalization.CultureInfo.InvariantCulture);
		var session = stamp[0];

		// Act

		switch (command)
		{
			case "clear": await cut.InvokeAsync(cut.Instance.ClearInspectionAsync); break;
			case "view": await cut.InvokeAsync(() => cut.Instance.ZoomToXAsync(2, 3)); break;
			case "context": cut.Render(p => p.Add(plot => plot.Context, Single())); break;
			case "dispose": await cut.Instance.DisposeAsync(); break;
		}
		await cut.InvokeAsync(() => panel.OnTouchActionAsync(session, epoch, 1, false));

		// Assert

		activations.Should().BeEmpty();
	}

	[Fact]
	public async Task ActionOnlyPolicyAcknowledgesClearAndAcceptsTheNextTap()
	{
		// Arrange

		var cut = Render(Single(), new() { Touch = new() { TapActivatesPanel = true } });
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		await cut.InvokeAsync(cut.Instance.ClearInspectionAsync);
		var stamp = cut.Find("[data-ggnet-inspection]").GetAttribute("data-ggnet-inspection")!.Split(' ');

		// Act

		await cut.InvokeAsync(() => panel.OnTouchActionAsync(stamp[0], long.Parse(stamp[1], System.Globalization.CultureInfo.InvariantCulture), 1, false));

		// Assert

		using var _ = new AssertionScope();
		activations.Should().ContainSingle();
		cut.FindAll("[data-ggnet-cursor]").Should().BeEmpty();
	}

}

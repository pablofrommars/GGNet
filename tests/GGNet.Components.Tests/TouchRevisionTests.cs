using System.Globalization;

namespace GGNet.Components.Tests;

public class TouchRevisionTests : BunitContext
{
	public sealed record P(double X, double Y);

	public TouchRevisionTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	private IRenderedComponent<Plot<P, double, double>> Mount(Func<InspectionReadings<double>, Task>? readout = null)
	{
		var context = PlotContext.Build(new[] { new P(0, 1), new P(100, 2) }, point => point.X, point => point.Y).Geom_Point().Style();
		context.SetXView(0, 100);
		return Render<Plot<P, double, double>>(parameters => parameters
			.Add(plot => plot.Context, context).Add(plot => plot.RenderMode, RenderMode.InteractiveAuto)
			.Add(plot => plot.Interactivity, new() { Touch = new() { Inspect = true, Navigation = TouchNavigation.PanAndPinchX } })
			.Add(plot => plot.ReadingsChanged, readout ?? (_ => Task.CompletedTask)));
	}

	private static (string session, long epoch) Identity(IRenderedComponent<Plot<P, double, double>> cut)
	{
		var stamp = cut.Find("[data-ggnet-inspection]").GetAttribute("data-ggnet-inspection")!.Split(' ');
		return (stamp[0], long.Parse(stamp[1], CultureInfo.InvariantCulture));
	}

	[Fact]
	public async Task RestoreAndNewerPinRejectLateScrubWithoutAVisibleCrosshair()
	{
		// Arrange

		var cut = Mount();
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var (session, epoch) = Identity(cut);

		// Act

		await cut.InvokeAsync(() => panel.OnTouchInspectAsync(session, epoch, 1, 25, true, false));
		await cut.InvokeAsync(() => panel.OnTouchInspectAsync(session, epoch, 2, 75, false, false));
		await cut.InvokeAsync(() => panel.OnTouchInspectAsync(session, epoch, 3, 0, false, true));
		await cut.InvokeAsync(() => panel.OnTouchInspectAsync(session, epoch, 2, 90, false, false));

		// Assert

		using var _ = new AssertionScope();
		cut.Instance.Readings!.Inspection.Should().Be(new Inspection<double>(25));
		cut.Find("[data-ggnet-inspection]").GetAttribute("data-ggnet-committed").Should().Be("25");
		cut.FindAll("[data-ggnet-cursor]").Should().BeEmpty();
	}

	[Fact]
	public async Task ClearInvalidatesQueuedTouchEvenWhenItTargetsTheSameValue()
	{
		// Arrange

		var cut = Mount();
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var (session, epoch) = Identity(cut);
		await cut.InvokeAsync(() => panel.OnTouchInspectAsync(session, epoch, 1, 25, true, false));

		// Act

		await cut.InvokeAsync(cut.Instance.ClearInspectionAsync);
		await cut.InvokeAsync(() => panel.OnTouchInspectAsync(session, epoch, 2, 25, true, false));

		// Assert

		using var _ = new AssertionScope();
		cut.Instance.Readings!.Inspection.Should().BeNull();
		Identity(cut).epoch.Should().BeGreaterThan(epoch);
		cut.Find("[data-ggnet-inspection]").HasAttribute("data-ggnet-committed").Should().BeFalse();
	}

	[Fact]
	public async Task RepeatedPinAcknowledgesItsNewRevisionWithoutChangingSelection()
	{
		// Arrange

		var cut = Mount();
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var (session, epoch) = Identity(cut);
		await cut.InvokeAsync(() => panel.OnTouchInspectAsync(session, epoch, 1, 25, true, false));

		// Act

		await cut.InvokeAsync(() => panel.OnTouchInspectAsync(session, epoch, 2, 25, true, false));

		// Assert

		cut.WaitForAssertion(() => cut.Find("[data-ggnet-inspection]").GetAttribute("data-ggnet-inspection").Should().Be(FormattableString.Invariant($"{session} {epoch} 2")));
		cut.Instance.Readings!.Inspection.Should().Be(new Inspection<double>(25));
	}

	public enum Stage
	{
		Mash,
		Boil,
		Ferment
	}

	public sealed record Phase(Stage Stage, double Hours);

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task AnInspectionBeyondTheCategoriesIsAcknowledgedWithoutSelectingAnything(bool pinned)
	{
		// Arrange — a discrete axis pads its range, so a contact can land where no category maps.

		List<Inspection<Stage>?> positions = [];
		var context = PlotContext.Build(new Phase[] { new(Stage.Mash, 1), new(Stage.Boil, 2), new(Stage.Ferment, 3) }, phase => phase.Stage, phase => phase.Hours).Geom_Bar().Style();
		var cut = Render<Plot<Phase, Stage, double>>(parameters => parameters
			.Add(plot => plot.Context, context).Add(plot => plot.RenderMode, RenderMode.InteractiveAuto)
			.Add(plot => plot.Interactivity, new() { Touch = new() { Inspect = true } })
			.Add(plot => plot.InspectionChanged, positions.Add));
		var panel = cut.FindComponent<Panel<Phase, Stage, double>>().Instance;
		var stamp = cut.Find("[data-ggnet-inspection]").GetAttribute("data-ggnet-inspection")!.Split(' ');
		var (session, epoch) = (stamp[0], long.Parse(stamp[1], CultureInfo.InvariantCulture));
		await cut.InvokeAsync(() => panel.OnTouchInspectAsync(session, epoch, 1, 1, true, false));
		await cut.InvokeAsync(() => panel.OnTouchInspectAsync(session, epoch, 2, 2, false, false));

		// Act

		await cut.InvokeAsync(() => panel.OnTouchInspectAsync(session, epoch, 3, -1, pinned, false));

		// Assert — a release selects nothing and returns to the pin; a scrub position leaves the last one.

		using var _ = new AssertionScope();
		cut.WaitForAssertion(() => cut.Find("[data-ggnet-inspection]").GetAttribute("data-ggnet-inspection").Should().Be(FormattableString.Invariant($"{session} {epoch} 3")));
		cut.Find("[data-ggnet-inspection]").GetAttribute("data-ggnet-committed").Should().Be("1");
		cut.Find("[data-ggnet-inspection]").GetAttribute("data-ggnet-selection").Should().Be(pinned ? "1" : "2");
		positions.Should().Equal(pinned
			? [new(Stage.Boil), new(Stage.Ferment, false), new(Stage.Boil)]
			: [new(Stage.Boil), new(Stage.Ferment, false)]);
	}

	[Fact]
	public async Task InspectionAcknowledgementWaitsForTheReadoutCallback()
	{
		// Arrange

		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var cut = Mount(async _ =>
		{
			started.TrySetResult();
			await release.Task;
		});
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var (session, epoch) = Identity(cut);

		// Act

		var operation = cut.InvokeAsync(() => panel.OnTouchInspectAsync(session, epoch, 1, 25, true, false));
		await started.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
		var premature = cut.FindAll("[data-ggnet-inspection]").Any(element => element.GetAttribute("data-ggnet-inspection")!.EndsWith(" 1", StringComparison.Ordinal));
		release.SetResult();
		await operation;

		// Assert

		premature.Should().BeFalse();
		cut.WaitForAssertion(() => cut.Find("[data-ggnet-inspection]").GetAttribute("data-ggnet-inspection").Should().Be(FormattableString.Invariant($"{session} {epoch} 1")));
	}

	[Fact]
	public async Task ReentrantClearPublishesTheClearedReadoutLast()
	{
		// Arrange

		IRenderedComponent<Plot<P, double, double>>? cut = null;
		var published = new List<double?>();
		cut = Mount(async reading =>
		{
			if (reading.Inspection is not null)
			{
				await cut!.Instance.ClearInspectionAsync();
			}
			published.Add(reading.Inspection?.X);
		});
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		var (session, epoch) = Identity(cut);

		// Act

		await cut.InvokeAsync(() => panel.OnTouchInspectAsync(session, epoch, 1, 25, true, false));

		// Assert

		published.Should().Equal(25, null);
		cut.Instance.Readings!.Inspection.Should().BeNull();
	}
}

using System.Globalization;

using GGNet.Exceptions;

using NodaTime;

namespace GGNet.Components.Tests;

public class PlotInspectionTests : BunitContext
{
	public sealed record P(double X, double Y);

	private enum Phase
	{
		Lag,
		Growth
	}

	private static readonly InteractivityOptions crosshair = new() { Crosshair = true };

	private readonly List<Inspection<double>?> positions = [];

	private readonly List<InspectionReadings<double>> readouts = [];

	public PlotInspectionTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	private static string Text(P point) => point.Y.ToString("0.0", CultureInfo.InvariantCulture);

	private static List<P> Samples() => [new(0, 1), new(10, 2), new(20, 3)];

	private static PlotContext<P, double, double> Line(IReadOnlyList<P> data, string name = "Series")
		=> PlotContext.Build(data, point => point.X, point => point.Y)
			.Geom_Line(inspect: new(name, Text, InspectPolicy.Previous))
			.Style();

	private IRenderedComponent<Plot<P, double, double>> Mount(
		PlotContext<P, double, double> context,
		Inspection<double>? inspection = null,
		InteractivityOptions? interactivity = null,
		RenderMode mode = RenderMode.InteractiveAuto)
		=> Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, mode)
			.Add(p => p.Interactivity, interactivity ?? crosshair)
			.Add(p => p.Inspection, inspection)
			.Add(p => p.InspectionChanged, positions.Add)
			.Add(p => p.ReadingsChanged, readouts.Add));

	[Fact]
	public void AHostSuppliedPositionIsReadWithoutBeingEchoedBack()
	{
		// Arrange

		var context = Line(Samples());

		// Act

		var cut = Mount(context, new(15));

		// Assert

		using var _ = new AssertionScope();

		positions.Should().BeEmpty();
		readouts.Should().ContainSingle().Which.Should().Be(new InspectionReadings<double>(
			new(15),
			[new("Series", null, "Series", 15, 10, ReadingKind.Previous, "2.0")],
			false));
		cut.Instance.Readings.Should().Be(readouts[0]);
	}

	[Fact]
	public void TheHostMovesThePositionByChangingTheParameter()
	{
		// Arrange

		var cut = Mount(Line(Samples()), new(15));

		// Act

		cut.Render(parameters => parameters.Add(p => p.Inspection, new Inspection<double>(25)));

		// Assert

		using var _ = new AssertionScope();

		positions.Should().BeEmpty();
		readouts.Should().HaveCount(2);
		readouts[1].Values.Should().ContainSingle().Which.Value.Should().Be("3.0");
	}

	[Fact]
	public async Task ClearingReportsBothThePositionAndTheEmptyReadout()
	{
		// Arrange

		var cut = Mount(Line(Samples()), new(15));
		readouts.Clear();

		// Act

		await cut.Instance.ClearInspectionAsync();

		// Assert

		using var _ = new AssertionScope();

		positions.Should().Equal([null]);
		readouts.Should().ContainSingle().Which.Should().Be(new InspectionReadings<double>(null, [], false));
	}

	[Fact]
	public async Task ClearingWithNothingInspectedReportsNothing()
	{
		// Arrange

		var cut = Mount(Line(Samples()));

		// Act

		await cut.Instance.ClearInspectionAsync();

		// Assert

		using var _ = new AssertionScope();

		positions.Should().BeEmpty();
		readouts.Should().BeEmpty();
	}

	[Theory]
	[InlineData(RenderMode.InteractiveAuto)]
	[InlineData(RenderMode.Interactive)]
	public async Task AppendedDataChangesTheReadingsAndNotThePosition(RenderMode mode)
	{
		// Arrange

		var data = Samples();
		var cut = Mount(Line(data), new(25), mode: mode);
		readouts.Clear();

		// Act

		data.Add(new(22, 9));
		await cut.InvokeAsync(() => cut.Instance.RefreshAsync(RenderTarget.Render, CancellationToken.None));

		// Assert

		cut.WaitForAssertion(() => readouts.Should().ContainSingle());

		using var _ = new AssertionScope();

		positions.Should().BeEmpty();
		readouts[0].Inspection.Should().Be(new Inspection<double>(25));
		readouts[0].Values.Should().ContainSingle().Which.Should().Be(
			new Reading<double>("Series", null, "Series", 25, 22, ReadingKind.Previous, "9.0"));
	}

	[Fact]
	public async Task ARefreshThatChangesNoReadingReportsNothing()
	{
		// Arrange

		var data = Samples();
		var cut = Mount(Line(data), new(15));
		readouts.Clear();

		// Act

		data.Add(new(30, 9));
		await cut.InvokeAsync(() => cut.Instance.RefreshAsync(RenderTarget.Render, CancellationToken.None));

		// Assert

		readouts.Should().BeEmpty();
	}

	[Fact]
	public void ANewContextKeepsThePositionAndIsReadAfresh()
	{
		// Arrange

		var cut = Mount(Line(Samples()), new(15));
		readouts.Clear();
		var replacement = Line([new(0, 7), new(12, 8), new(20, 9)], "Other");

		// Act

		cut.Render(parameters => parameters.Add(p => p.Context, replacement));

		// Assert

		cut.WaitForAssertion(() => readouts.Should().ContainSingle());

		using var _ = new AssertionScope();

		positions.Should().BeEmpty();
		readouts[0].Should().Be(new InspectionReadings<double>(
			new(15),
			[new("Other", null, "Other", 15, 12, ReadingKind.Previous, "8.0")],
			false));
	}

	[Fact]
	public async Task APositionOutsideTheWindowKeepsItsReadingsAndIsFlagged()
	{
		// Arrange

		var cut = Mount(Line(Samples()), new(15));
		readouts.Clear();

		// Act

		await cut.InvokeAsync(() => cut.Instance.ZoomToXAsync(0, 5));

		// Assert

		cut.WaitForAssertion(() => readouts.Should().ContainSingle());

		using var _ = new AssertionScope();

		positions.Should().BeEmpty();
		readouts[0].OutsideView.Should().BeTrue();
		readouts[0].Inspection.Should().Be(new Inspection<double>(15));
		readouts[0].Values.Should().ContainSingle().Which.Value.Should().Be("2.0");
	}

	[Fact]
	public void WithoutCrosshairAPositionIsIgnored()
	{
		// Arrange

		var context = Line(Samples());

		// Act

		var cut = Mount(context, new(15), new InteractivityOptions());

		// Assert

		using var _ = new AssertionScope();

		readouts.Should().BeEmpty();
		cut.Instance.Readings.Should().BeNull();
	}

	[Fact]
	public void ReadingsOnALocalDateTimeAxisAreRefusedUnderCrosshair()
	{
		// Arrange

		(LocalDateTime At, double Value)[] data = [(new(2026, 11, 10, 14, 0), 1), (new(2026, 11, 10, 14, 1), 2)];
		var context = PlotContext.Build(data, row => row.At, row => row.Value)
			.Geom_Line(inspect: new("Series", row => "x"))
			.Style();

		// Act

		Action mount = () => Render<Plot<(LocalDateTime At, double Value), LocalDateTime, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, crosshair));

		// Assert

		mount.Should().Throw<GGNetUserException>().WithMessage("*Instant or numeric x axis*LocalDateTime*");
	}

	[Fact]
	public void ReadingsOnADiscreteAxisAreRefusedUnderCrosshair()
	{
		// Arrange

		(Phase Phase, double Value)[] data = [(Phase.Lag, 1), (Phase.Growth, 2)];
		var context = PlotContext.Build(data, row => row.Phase, row => row.Value)
			.Geom_Bar(inspect: new("Series", row => "x"))
			.Style();

		// Act

		Action mount = () => Render<Plot<(Phase Phase, double Value), Phase, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, crosshair));

		// Assert

		mount.Should().Throw<GGNetUserException>().WithMessage("*Instant or numeric x axis*Phase*");
	}

	[Fact]
	public void TheSameLayerMountsWithoutCrosshair()
	{
		// Arrange

		(LocalDateTime At, double Value)[] data = [(new(2026, 11, 10, 14, 0), 1), (new(2026, 11, 10, 14, 1), 2)];
		var context = PlotContext.Build(data, row => row.At, row => row.Value)
			.Geom_Line(inspect: new("Series", row => "x"))
			.Style();

		// Act

		Action mount = () => Render<Plot<(LocalDateTime At, double Value), LocalDateTime, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, new InteractivityOptions()));

		// Assert

		mount.Should().NotThrow();
	}

	[Fact]
	public void ALimitThatDoesNotFitTheAxisIsRefusedWhenThePlotMounts()
	{
		// Arrange

		var context = PlotContext.Build(Samples(), point => point.X, point => point.Y)
			.Geom_Line(inspect: new("Series", Text) { MaxDuration = Duration.FromMinutes(1) })
			.Style();

		// Act

		Action mount = () => Mount(context);

		// Assert

		mount.Should().Throw<GGNetUserException>().WithMessage("*MaxDistance, not MaxDuration*");
	}

	[Fact]
	public void AReplacementContextIsCheckedToo()
	{
		// Arrange

		var cut = Mount(Line(Samples()));
		var unfit = PlotContext.Build(Samples(), point => point.X, point => point.Y)
			.Geom_Line(inspect: new("Series", Text) { MaxDuration = Duration.FromMinutes(1) })
			.Style();

		// Act

		Action replace = () => cut.Render(parameters => parameters.Add(p => p.Context, unfit));

		// Assert

		replace.Should().Throw<GGNetUserException>().WithMessage("*MaxDistance, not MaxDuration*");
	}
}

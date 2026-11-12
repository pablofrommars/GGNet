namespace GGNet.Headless.Tests;

// The imperative view commands end to end through headless export:
// programmatic windows need no component and no prior render — the
// server-side "zoom to / show last" export path.
public class ViewCommandTests
{
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

	[Fact]
	public async Task SetXViewBeforeFirstRenderWindowsTheExport()
	{
		// Arrange

		var context = PointPlot();

		// Act

		// No render has happened yet: the command maps through a throwaway
		// factory instance and the first render already honors the window.
		context.SetXView(2.0, 3.0);

		var svg = await context.AsStringAsync();

		// Assert

		XDocument.Parse(svg);

		await Verifier.Verify(svg, extension: "svg");
	}

	[Fact]
	public async Task ResetViewRestoresTheBaselineExport()
	{
		// Arrange

		var context = PointPlot();

		var baseline = await context.AsStringAsync();

		// Act

		context.SetXView(2.0, 3.0);

		var windowed = await context.AsStringAsync();

		context.ResetView();

		var restored = await context.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		windowed.Should().NotBe(baseline);
		restored.Should().Be(baseline);
	}

	[Fact]
	public async Task FitYToXViewWindowsYToTheVisibleData()
	{
		// Arrange

		var context = PointPlot();

		// Act

		// Visible inside x ∈ [2, 3]: (2, 3.5) and (3, 2.8).
		context.SetXView(2.0, 3.0);
		context.FitYToXView();

		await context.AsStringAsync();

		// Assert

		// Fit is visible-y [2.8, 3.5] with a 5% margin (0.035).
		var scale = context.Positions.Y.Scales[0];

		using var _ = new AssertionScope();

		scale.Range.min.Should().BeApproximately(2.765, 1e-9);
		scale.Range.max.Should().BeApproximately(3.535, 1e-9);
	}

	[Fact]
	public async Task FitYKeepsThePreviousWindowWhenNothingIsVisible()
	{
		// Arrange

		var context = PointPlot();

		// Act

		// A window past the data: no visible items, the fit must not collapse y.
		context.SetXView(10.0, 11.0);
		context.FitYToXView();

		await context.AsStringAsync();

		// Assert

		context.Positions.Y.Scales[0].ViewRange.Should().BeNull();
	}

	[Fact]
	public async Task FitYMeasuresDrawnSegmentsBetweenDataPoints()
	{
		// Arrange

		// A window strictly between vertices: the source scan sees no points,
		// but the drawn line crosses it — the fit interpolates the segment.
		var context = PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Line()
			.Style();

		var baseline = await context.AsStringAsync();

		// Act

		context.SetXView(2.2, 2.8);
		context.FitYToXView();

		await context.AsStringAsync();

		// Assert

		// Segment (2, 3.5) → (3, 2.8): y(2.2) = 3.36, y(2.8) = 2.94.
		// Fit = [2.94, 3.36] with a 5% margin (0.021).
		var scale = context.Positions.Y.Scales[0];

		using var _ = new AssertionScope();

		scale.Range.min.Should().BeApproximately(2.919, 1e-9);
		scale.Range.max.Should().BeApproximately(3.381, 1e-9);
	}

	private sealed record Timed(Instant Ts, double Value);

	[Fact]
	public async Task ShowLastWindowsToTheTrailingSpan()
	{
		// Arrange

		var start = Instant.FromUtc(2026, 7, 1, 0, 0);

		var timed = new Timed[8];

		for (var i = 0; i < timed.Length; i++)
		{
			timed[i] = new(start + Duration.FromHours(12 * i), 1.0 + i);
		}

		var context = PlotContext.Build(timed, t => t.Ts, t => t.Value)
			.Scale_X_Instant()
			.Geom_Point()
			.Style();

		// Act

		context.ShowLast(Duration.FromHours(48));

		await context.AsStringAsync();

		// Assert

		// The window is exact (no expansion), anchored at the latest sample.
		var end = start + Duration.FromHours(12 * (timed.Length - 1));

		var scale = context.Positions.X.Scales[0];

		using var _ = new AssertionScope();

		scale.Range.min.Should().Be((end - Duration.FromHours(48)).ToUnixTimeMilliseconds());
		scale.Range.max.Should().Be(end.ToUnixTimeMilliseconds());
	}

	[Fact]
	public void ViewsAreNullUntilAWindowIsSet()
	{
		// Arrange

		var context = PointPlot();

		// Act

		var (x, y) = (context.XView, context.YView);

		// Assert

		using var _ = new AssertionScope();

		x.Should().BeNull();
		y.Should().BeNull();
	}

	[Fact]
	public void ViewsReadBackInDataUnitsBeforeTheFirstRender()
	{
		// Arrange

		var context = PointPlot();

		// Act

		context.SetXView(2.0, 3.0);
		context.SetYView(2.5, 4.0);

		// Assert

		using var _ = new AssertionScope();

		context.XView.Should().Be((2.0, 3.0));
		context.YView.Should().Be((2.5, 4.0));
	}

	[Fact]
	public void ReversedBoundsReadBackInAxisOrder()
	{
		// Arrange

		var context = PointPlot();

		// Act

		context.SetXView(3.0, 2.0);

		// Assert

		context.XView.Should().Be((2.0, 3.0));
	}

	[Fact]
	public async Task ResetViewClearsBothReadings()
	{
		// Arrange

		var context = PointPlot();

		context.SetXView(2.0, 3.0);
		context.FitYToXView();

		await context.AsStringAsync();

		// Act

		context.ResetView();

		// Assert

		using var _ = new AssertionScope();

		context.XView.Should().BeNull();
		context.YView.Should().BeNull();
	}

	[Fact]
	public async Task FittedYViewReadsTheWindowShown()
	{
		// Arrange

		var context = PointPlot();

		// Act

		context.SetXView(2.0, 3.0);
		context.FitYToXView();

		await context.AsStringAsync();

		// Assert

		var shown = context.Positions.Y.Scales[0].Range;

		context.YView.Should().Be(shown);
	}

	[Fact]
	public async Task TransformedAxesReadBackInDataUnits()
	{
		// Arrange

		var context = PlotContext.Build(data, i => i.X, i => i.Y)
			.Scale_X_Log10()
			.Scale_Y_Sqrt()
			.Geom_Point()
			.Style();

		// Act

		context.SetXView(1.0, 100.0);
		context.SetYView(4.0, 9.0);

		await context.AsStringAsync();

		// Assert

		var (x, y) = (context.XView, context.YView);

		using var _ = new AssertionScope();

		x.Should().NotBeNull();
		x!.Value.min.Should().BeApproximately(1.0, 1e-9);
		x.Value.max.Should().BeApproximately(100.0, 1e-9);
		y.Should().NotBeNull();
		y!.Value.min.Should().BeApproximately(4.0, 1e-9);
		y.Value.max.Should().BeApproximately(9.0, 1e-9);
	}

	[Fact]
	public void InstantViewReadsBackTheTrailingWindow()
	{
		// Arrange

		var start = Instant.FromUtc(2026, 7, 1, 0, 0);

		var timed = new Timed[8];

		for (var i = 0; i < timed.Length; i++)
		{
			timed[i] = new(start + Duration.FromHours(12 * i), 1.0 + i);
		}

		var context = PlotContext.Build(timed, t => t.Ts, t => t.Value)
			.Scale_X_Instant()
			.Geom_Point()
			.Style();

		// Act

		context.ShowLast(Duration.FromHours(48));

		// Assert

		var end = start + Duration.FromHours(12 * (timed.Length - 1));

		context.XView.Should().Be((end - Duration.FromHours(48), end));
	}

	private enum Kind
	{
		A,
		B,
		C
	}

	private sealed record Counted(Kind Kind, double Value);

	[Fact]
	public async Task DiscreteViewHasNoDataUnitReading()
	{
		// Arrange

		Counted[] counted = [new(Kind.A, 1.0), new(Kind.B, 2.0), new(Kind.C, 3.0)];

		var context = PlotContext.Build(counted, c => c.Kind, c => c.Value)
			.Geom_Point()
			.Style();

		// Discrete bounds map only after the categories are trained.
		await context.AsStringAsync();

		// Act

		context.SetXView(Kind.A, Kind.B);

		// Assert

		using var _ = new AssertionScope();

		context.Positions.X.View.Should().NotBeNull();
		context.XView.Should().BeNull();
	}

	private sealed record Sampled(LocalDateTime Ts, double Value);

	[Fact]
	public async Task LocalDateTimeViewHasNoDataUnitReading()
	{
		// Arrange

		var start = new LocalDateTime(2026, 7, 1, 9, 0);

		Sampled[] sampled = [new(start, 1.0), new(start.PlusMinutes(30), 2.0), new(start.PlusMinutes(60), 3.0)];

		var context = PlotContext.Build(sampled, s => s.Ts, s => s.Value)
			.Geom_Point()
			.Style();

		// Sampled minutes map only after the axis is trained.
		await context.AsStringAsync();

		// Act

		context.SetXView(start, start.PlusMinutes(30));

		// Assert

		using var _ = new AssertionScope();

		context.Positions.X.View.Should().NotBeNull();
		context.XView.Should().BeNull();
	}
}

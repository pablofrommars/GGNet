using GGNet.Components;

using NodaTime.Text;

namespace GGNet.Demo.Components.Pages;

public partial class TimeCursor
{
	private enum Stage
	{
		Mash,
		Boil,
		Ferment,
		Condition
	}

	private sealed record Step(Stage Stage, double Hours);

	private sealed record Batch(string Name, double Hours, double Gravity);

	private static readonly InstantPattern time = InstantPattern.CreateWithInvariantCulture("ddd HH:mm:ss");

	private static readonly InstantPattern clock = InstantPattern.CreateWithInvariantCulture("HH:mm");

	private readonly RenderMode renderMode = RenderMode.Interactive;

	private readonly InteractivityOptions interactivity = new() { Crosshair = true, Pan = true };

	private readonly InteractivityOptions crosshairOnly = new() { Crosshair = true };

	private readonly List<Sample> temperature = Process.Temperature();

	private readonly List<Sample> pressure = Process.Pressure();

	private PlotContext<Sample, Instant, double> context = default!;

	private PlotContext<Sample, Instant, double> plainContext = default!;

	private PlotContext<Step, Stage, double> snapContext = default!;

	private PlotContext<Batch, double, double> freeContext = default!;

	private Plot<Sample, Instant, double> plot = default!;

	private Inspection<Instant>? inspection;

	private InspectionReadings<Instant>? readings;

	private int positionChanges;

	private int readoutChanges;

	private int viewChanges;

	private string window = "—";

	private string windowMinutes = "—";

	private int markClicks;

	private int ancestorClicks;

	protected override void OnInitialized()
	{
		context = Build();

		plainContext = PlotContext.Build(Process.Gravity, s => s.At, s => s.Value)
			.Scale_X_Instant()
			.Geom_Line(color: "#2563eb")
			.Geom_Point(color: "#2563eb", size: 5)
			.YLab("SG")
			.Style();

		Step[] steps = [new(Stage.Mash, 1.5), new(Stage.Boil, 1.0), new(Stage.Ferment, 96), new(Stage.Condition, 48)];

		snapContext = PlotContext.Build(steps, s => s.Stage, s => s.Hours)
			.Geom_Bar(fill: "#7c3aed")
			.YLab("hours")
			.Style();

		Batch[] batches =
		[
			new("Lager", 0, 1.048), new("Lager", 40, 1.030), new("Lager", 120, 1.012), new("Lager", 200, 1.009),
			new("Ale", 0, 1.056), new("Ale", 12, 1.040), new("Ale", 36, 1.018), new("Ale", 60, 1.013)
		];

		freeContext = PlotContext.Build(batches, b => b.Hours, b => b.Gravity)
			.Geom_Line(color: "#059669")
			.Geom_Point(color: "#059669", size: 5)
			.Facet_Wrap(b => b.Name, freeX: true)
			.Style();
	}

	private PlotContext<Sample, Instant, double> Build()
		=> PlotContext.Build(temperature, s => s.At, s => s.Value)
			.Scale_X_Instant(formatter: new Formats.InstantFormatter("HH:mm"))
			.Panel(p => p
				.Scale_Y_Continuous()
				.Geom_Line(color: "#dc2626", strokeWidth: 2,
					inspect: new("Temperature", Celsius) { MaxDuration = Duration.FromMinutes(10) })
				.Geom_Line(Process.Target, s => s.At, s => s.Value, color: "#dc2626", lineType: LineType.Dashed,
					inspect: new("Target", Celsius, InspectPolicy.Step))
				.Geom_Point(Process.Ambient, s => s.At, s => s.Value, color: "#6b7280", size: 4,
					inspect: new("Ambient", Celsius) { MaxDuration = Duration.FromMinutes(15) })
				.YLab("°C"), height: 0.4)
			.Panel(p => p
				.Scale_Y_Continuous()
				.Geom_Line(Process.Gravity, s => s.At, s => s.Value, color: "#2563eb", strokeWidth: 2)
				.Geom_Point(Process.Gravity, s => s.At, s => s.Value, color: "#2563eb", size: 5,
					onclick: (_, _) => InvokeAsync(() =>
					{
						markClicks++;
						StateHasChanged();
					}),
					inspect: new("Gravity", s => FormattableString.Invariant($"{s.Value:0.000}"), InspectPolicy.Previous) { MaxDuration = Duration.FromHours(3) })
				.YLab("SG"), height: 0.25)
			.Panel(p => p
				.Scale_Y_Continuous()
				.Geom_Area(pressure, s => s.At, s => s.Value, fill: "#059669", fillOpacity: 0.35,
					inspect: new("Pressure", s => FormattableString.Invariant($"{s.Value:0.00} bar")) { MaxDuration = Duration.FromMinutes(10) })
				.YLab("bar"), height: 0.2)
			.Panel(p => p
				.Scale_Y_Continuous(limits: (0, 1), hide: true)
				.Geom_Bar(Process.Cooling, s => s.At, s => s.Value, fill: "#0ea5e9", width: 1.0,
					inspect: new("Cooling", _ => "on", InspectPolicy.Containing) { Missing = "off" })
				.YLab("cooling"), height: 0.15)
			.Style();

	private static string Celsius(Sample sample) => FormattableString.Invariant($"{sample.Value:0.0} °C");

	private static string Time(Instant instant) => time.Format(instant);

	private static string Note(Reading<Instant> reading)
	{
		if (reading.Sample is not { } sample)
		{
			return string.Empty;
		}

		if (reading.Kind == ReadingKind.Step)
		{
			return "in effect since " + clock.Format(sample);
		}

		if (reading.Kind == ReadingKind.Containing)
		{
			return "interval around " + clock.Format(sample);
		}

		var offset = reading.Cursor - sample;
		var seconds = Math.Abs(offset.TotalSeconds);

		if (seconds < 1)
		{
			return "at the cursor";
		}

		var distance = seconds < 90
			? FormattableString.Invariant($"{seconds:0} s")
			: FormattableString.Invariant($"{seconds / 60:0} min");

		return $"sample {distance} {(offset > Duration.Zero ? "earlier" : "later")}";
	}

	private void OnInspectionChanged(Inspection<Instant>? value)
	{
		inspection = value;
		positionChanges++;
	}

	private void OnReadingsChanged(InspectionReadings<Instant> value)
	{
		readings = value;
		readoutChanges++;
	}

	private void OnViewChanged(ViewChange<Instant, double> change)
	{
		viewChanges++;
		window = change.X is { } x ? $"{clock.Format(x.min)}–{clock.Format(x.max)}" : "—";
		windowMinutes = change.X is { } span ? FormattableString.Invariant($"{(span.max - span.min).TotalMinutes:0}") : "—";
	}

	private Task ClearAsync() => plot.ClearInspectionAsync();

	private Task ResetAsync() => plot.ResetViewAsync();

	private Task AppendAsync()
	{
		for (var i = 0; i < 6; i++)
		{
			var at = temperature[^1].At + Duration.FromMinutes(5);

			temperature.Add(new(at, Process.TemperatureAt((at - Process.Start).TotalHours)));
		}

		for (var i = 0; i < 4; i++)
		{
			var at = pressure[^1].At + Duration.FromMinutes(7);

			pressure.Add(new(at, Process.PressureAt((at - Process.Start).TotalHours)));
		}

		return plot.RefreshAsync(RenderTarget.Render, CancellationToken.None);
	}

	private void Replace() => context = Build();
}

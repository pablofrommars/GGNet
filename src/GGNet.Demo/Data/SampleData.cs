namespace GGNet.Demo.Data;

public sealed record Reading(double Hours, double Gravity, string Batch);

public sealed record TimedReading(Instant Ts, double Gravity);

// Deterministic sample data so every demo page renders identically across runs.
public static class SampleData
{
	public static readonly IReadOnlyList<Reading> Readings =
	[
		new(0, 1.060, "Ale"),
		new(12, 1.058, "Ale"),
		new(24, 1.052, "Ale"),
		new(36, 1.044, "Ale"),
		new(48, 1.036, "Ale"),
		new(60, 1.029, "Ale"),
		new(72, 1.024, "Ale"),
		new(84, 1.020, "Ale"),
		new(96, 1.017, "Ale"),
		new(108, 1.015, "Ale"),
		new(120, 1.014, "Ale"),
		new(132, 1.013, "Ale"),
		new(144, 1.012, "Ale"),
		new(156, 1.012, "Ale"),
		new(168, 1.012, "Ale"),
		new(0, 1.055, "Lager"),
		new(12, 1.054, "Lager"),
		new(24, 1.052, "Lager"),
		new(36, 1.049, "Lager"),
		new(48, 1.045, "Lager"),
		new(60, 1.041, "Lager"),
		new(72, 1.037, "Lager"),
		new(84, 1.033, "Lager"),
		new(96, 1.029, "Lager"),
		new(108, 1.026, "Lager"),
		new(120, 1.023, "Lager"),
		new(132, 1.020, "Lager"),
		new(144, 1.018, "Lager"),
		new(156, 1.016, "Lager"),
		new(168, 1.015, "Lager")
	];

	// A seven-day fermentation at 12-hour cadence on a real time axis, for the
	// Instant-x pages (external controls, ShowLast).
	public static readonly Instant Start = Instant.FromUtc(2026, 7, 1, 0, 0);

	public static readonly IReadOnlyList<TimedReading> TimedReadings =
	[
		new(Start + Duration.FromHours(0), 1.060),
		new(Start + Duration.FromHours(12), 1.058),
		new(Start + Duration.FromHours(24), 1.052),
		new(Start + Duration.FromHours(36), 1.044),
		new(Start + Duration.FromHours(48), 1.036),
		new(Start + Duration.FromHours(60), 1.029),
		new(Start + Duration.FromHours(72), 1.024),
		new(Start + Duration.FromHours(84), 1.020),
		new(Start + Duration.FromHours(96), 1.017),
		new(Start + Duration.FromHours(108), 1.015),
		new(Start + Duration.FromHours(120), 1.014),
		new(Start + Duration.FromHours(132), 1.013),
		new(Start + Duration.FromHours(144), 1.012),
		new(Start + Duration.FromHours(156), 1.012),
		new(Start + Duration.FromHours(168), 1.012)
	];
}

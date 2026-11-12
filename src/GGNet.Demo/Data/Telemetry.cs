namespace GGNet.Demo.Data;

public sealed record TelemetryReading(Instant Ts, double Temperature, double Gravity, double Pressure, double Activity);

public static class Telemetry
{
	public static readonly Instant Start = Instant.FromUtc(2026, 7, 1, 0, 0);

	public static readonly IReadOnlyList<TelemetryReading> Readings = Generate();

	private static TelemetryReading[] Generate()
	{
		var readings = new TelemetryReading[97];

		for (var i = 0; i < readings.Length; i++)
		{
			var temperature = 19.0 + 1.5 * Math.Sin(i / 7.0) + 0.4 * Math.Sin(i / 2.3);

			readings[i] = new(
				Start + Duration.FromHours(i),
				temperature,
				1.060 - 0.045 * (1 - Math.Exp(-i / 30.0)),
				1.0 + 0.3 * Math.Sin(i / 11.0) + i / 200.0,
				temperature > 20.0 ? 1.0 : temperature < 18.0 ? -1.0 : 0.0);
		}

		return readings;
	}
}

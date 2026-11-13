namespace GGNet.Demo.Data;

public sealed record Sample(Instant At, double Value);

public static class Process
{
	public static readonly Instant Start = Instant.FromUtc(2026, 7, 1, 0, 0);

	public static readonly Duration Span = Duration.FromHours(24);

	public static List<Sample> Temperature() => [.. Every(Duration.FromMinutes(5), Duration.FromSeconds(8), TemperatureAt)];

	public static List<Sample> Pressure() => [.. Every(Duration.FromMinutes(7), Duration.FromSeconds(31), PressureAt)];

	public static double TemperatureAt(double hours) => 18.5 + 1.2 * Math.Sin(hours / 3.1) + 0.3 * Math.Sin(hours * 2.3);

	public static double PressureAt(double hours) => 0.4 + 0.5 * (1 - Math.Exp(-hours / 9.0)) + 0.03 * Math.Sin(hours * 1.7);

	public static readonly IReadOnlyList<Sample> Target = Steps((0, 18.0), (6, 19.5), (14, 17.5), (20, 18.0));

	public static readonly IReadOnlyList<Sample> Gravity = GenerateGravity();

	public static readonly IReadOnlyList<Sample> Ambient =
		[.. Every(Duration.FromMinutes(11), Duration.FromSeconds(47), hours => 21.0 + 0.8 * Math.Sin(hours / 2.0)).Where(sample => sample.At < Start + Duration.FromHours(9))];

	public static readonly IReadOnlyList<Sample> Cooling = GenerateCooling();

	private static IEnumerable<Sample> Every(Duration step, Duration phase, Func<double, double> value)
	{
		for (var at = Start + phase; at < Start + Span; at += step)
		{
			yield return new(at, value((at - Start).TotalHours));
		}
	}

	private static Sample[] Steps(params (int hour, double value)[] changes)
	{
		List<Sample> samples = [];

		for (var i = 0; i < changes.Length; i++)
		{
			var until = i + 1 < changes.Length ? Start + Duration.FromHours(changes[i + 1].hour) : Start + Span;

			samples.Add(new(Start + Duration.FromHours(changes[i].hour), changes[i].value));
			samples.Add(new(until - Duration.FromSeconds(1), changes[i].value));
		}

		return [.. samples];
	}

	private static Sample[] GenerateGravity()
	{
		List<Sample> samples = [];
		var at = Start + Duration.FromMinutes(3);

		for (var i = 0; at < Start + Span; i++)
		{
			samples.Add(new(at, 1.060 - 0.046 * (1 - Math.Exp(-(at - Start).TotalHours / 10.0))));
			at += Duration.FromMinutes(41 + 37 * (i % 3));
		}

		return [.. samples];
	}

	private static Sample[] GenerateCooling()
	{
		List<Sample> intervals = [];

		for (var i = 0; i < 48; i++)
		{
			var middle = Start + Duration.FromMinutes(15 + 30 * i);

			if (TemperatureAt((middle - Start).TotalHours) > 19.2)
			{
				intervals.Add(new(middle, 1.0));
			}
		}

		return [.. intervals];
	}
}

var readings = Enumerable.Range(0, 60)
	.Select(i => new Reading(
		$"Tank {(char)('A' + i % 6)}",
		Instant.FromUtc(2026, 7, 1, 0, 0).Plus(Duration.FromHours(i)),
		11.5 + (i % 6) * 0.7 + Math.Sin(i / 4.0)))
	.ToArray();

var tanks = readings.Select(r => r.Tank).Distinct().Order().ToArray();
double TankSlot(Reading reading) => Array.IndexOf(tanks, reading.Tank) + 1.0;

var summaries = Stat.Summary(readings, TankSlot, r => r.GravityPoints, r => r.Tank);

var plot = PlotContext.Build(summaries, s => s.X, s => s.Center)
	.Scale_X_Continuous(limits: (0.5, tanks.Length + 0.5), hide: true)
	.Scale_Color_Discrete(s => s.Group, ["#1f77b4", "#ff7f0e", "#2ca02c", "#d62728", "#9467bd", "#8c564b"])
	.Geom_ErrorBar(ymin: s => s.Lower, ymax: s => s.Upper)
	.Geom_Point(size: 7)
	.Geom_Text(y: s => s.Center + 0.35, text: s => $"{s.Group}: {s.Center:F1}")
	.Title("Average GravityPoints by Tank")
	.XLab("Tank")
	.YLab("Gravity points")
	.Style(legend: Position.Top);

Console.WriteLine((await plot.AsStringAsync()).Length);

record Reading(string Tank, Instant At, double GravityPoints);

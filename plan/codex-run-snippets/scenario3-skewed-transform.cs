var latencies = Enumerable.Range(0, 800)
	.Select(i => new ApiLatency(
		i % 160 == 0 ? 30_000 + i :
		i % 47 == 0 ? 1_500 + i * 7 :
		20 + i % 61 + Math.Pow(i % 17, 2) * 0.08))
	.Where(l => l.Milliseconds > 0)
	.ToArray();

var bins = Stat.Bin(latencies, l => Math.Log10(l.Milliseconds), bins: 32);
var realLatencyMarks = new[] { 50.0, 100.0, 1_000.0, 10_000.0, 30_000.0 };

var plot = PlotContext.Build(bins, b => b.Mid, b => b.Count)
	.Geom_Bar(width: 0.08, fill: "#4c78a8")
	.Geom_VLine(
		realLatencyMarks,
		ms => Math.Log10(ms),
		ms => ms >= 1_000 ? $"{ms / 1_000:g}s" : $"{ms:g} ms",
		color: "#111827",
		strokeWidth: 0.8)
	.Title("API Latency Distribution")
	.XLab("log10 latency in milliseconds; vertical marks show real latency values")
	.YLab("Requests")
	.Style();

Console.WriteLine((await plot.AsStringAsync()).Length);

record ApiLatency(double Milliseconds);

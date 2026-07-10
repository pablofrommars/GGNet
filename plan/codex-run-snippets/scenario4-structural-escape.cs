var productLines = new[]
{
	"Core", "Teams", "Enterprise", "Integrations", "Analytics", "Mobile", "Services"
};

var start = new LocalDate(2024, 1, 1);
var revenues = Enumerable.Range(0, 24)
	.SelectMany(monthIndex => productLines.Select((line, productIndex) =>
		new MonthlyRevenue(
			start.PlusMonths(monthIndex),
			line,
			90_000 + productIndex * 18_000 + monthIndex * (4_000 + productIndex * 350) + Math.Sin(monthIndex / 2.0 + productIndex) * 8_000)))
	.ToArray();

var shares = revenues
	.GroupBy(r => r.Month)
	.SelectMany(month =>
	{
		var total = month.Sum(r => r.Revenue);
		return month.Select(r => new RevenueShare(r.Month, r.ProductLine, r.Revenue / total));
	})
	.ToArray();

var plot = PlotContext.Build(shares, r => r.Month, r => r.Share)
	.Scale_Fill_Discrete(r => r.ProductLine, ["#4c78a8", "#f58518", "#54a24b", "#e45756", "#72b7b2", "#b279a2", "#ff9da6"])
	.Scale_Y_Continuous(limits: (0.0, 0.35), formatter: new DoubleFormatter("P0"))
	.Geom_Area(fillOpacity: 0.45)
	.Facet_Wrap(r => r.ProductLine, freeY: false, ncolumns: 2)
	.Title("Monthly Revenue Share by Product Line")
	.XLab("Month")
	.YLab("Share of monthly revenue")
	.Style(legend: Position.Top);

Console.WriteLine((await plot.AsStringAsync()).Length);

record MonthlyRevenue(LocalDate Month, string ProductLine, double Revenue);
record RevenueShare(LocalDate Month, string ProductLine, double Share);

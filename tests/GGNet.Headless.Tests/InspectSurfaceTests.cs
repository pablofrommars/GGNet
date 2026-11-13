using GGNet.Geoms;

namespace GGNet.Headless.Tests;

public class InspectSurfaceTests
{
	public sealed record Row(double X, double Y);

	private static readonly Row[] rows = [new(1, 2), new(2, 3), new(3, 1), new(4, 4)];

	private static readonly IEnumerable<Row> sequence = rows;

	private static readonly Inspect<Row> inspect = new("Series", row => row.Y.ToString("0.0", CultureInfo.InvariantCulture));

	private static PlotContext<Row, double, double> Plot() => PlotContext.Build(rows, row => row.X, row => row.Y);

	private static PlotContext<Row, double, double> Panel(Action<PanelFactory<Row, double, double>> layer)
		=> Plot().Panel(panel =>
		{
			layer(panel);

			return panel;
		});

	// One entry per public overload of the four inspected families: each has
	// to hand its own inspect argument to the geom it builds.
	public static TheoryData<string, Func<Inspect<Row>?, PlotContext<Row, double, double>>> Overloads => new()
	{
		{ "Line context", i => Plot().Geom_Line(inspect: i) },
		{ "Line context source", i => Plot().Geom_Line(rows, row => row.X, row => row.Y, inspect: i) },
		{ "Line panel", i => Panel(panel => panel.Geom_Line(inspect: i)) },
		{ "Line panel source", i => Panel(panel => panel.Geom_Line(rows, row => row.X, row => row.Y, inspect: i)) },
		{ "Point context", i => Plot().Geom_Point(inspect: i) },
		{ "Point context list", i => Plot().Geom_Point(rows, row => row.X, row => row.Y, inspect: i) },
		{ "Point context sequence", i => Plot().Geom_Point(sequence, row => row.X, row => row.Y, inspect: i) },
		{ "Point panel", i => Panel(panel => panel.Geom_Point(inspect: i)) },
		{ "Point panel list", i => Panel(panel => panel.Geom_Point(rows, row => row.X, row => row.Y, inspect: i)) },
		{ "Point panel sequence", i => Panel(panel => panel.Geom_Point(sequence, row => row.X, row => row.Y, inspect: i)) },
		{ "Area context", i => Plot().Geom_Area(inspect: i) },
		{ "Area context source", i => Plot().Geom_Area(rows, row => row.X, row => row.Y, inspect: i) },
		{ "Area panel", i => Panel(panel => panel.Geom_Area(inspect: i)) },
		{ "Area panel source", i => Panel(panel => panel.Geom_Area(rows, row => row.X, row => row.Y, inspect: i)) },
		{ "Bar context", i => Plot().Geom_Bar(inspect: i) },
		{ "Bar context source", i => Plot().Geom_Bar(rows, row => row.X, row => row.Y, inspect: i) },
		{ "Bar panel", i => Panel(panel => panel.Geom_Bar(inspect: i)) },
		{ "Bar panel source", i => Panel(panel => panel.Geom_Bar(rows, row => row.X, row => row.Y, inspect: i)) }
	};

	[Fact]
	public void EveryOverloadOfTheInspectedFamiliesIsListed()
	{
		// Arrange

		string[] families = ["Geom_Line", "Geom_Point", "Geom_Area", "Geom_Bar"];

		// Act

		var overloads = typeof(BuilderExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(method => families.Contains(method.Name))
			.ToArray();

		// Assert

		using var _ = new AssertionScope();

		overloads.Should().HaveCount(Overloads.Count);
		overloads.Should().OnlyContain(method => method.GetParameters().Last().Name == "inspect");
	}

	[Theory]
	[MemberData(nameof(Overloads))]
	public void TheOverloadHandsItsInspectionToTheGeom(string overload, Func<Inspect<Row>?, PlotContext<Row, double, double>> build)
	{
		// Arrange

		var context = build(inspect);
		context.Style();

		// Act

		context.Render();

		// Assert

		context.Panels.SelectMany(panel => panel.Geoms).OfType<Geom<Row, double, double>>()
			.Should().ContainSingle(overload).Which.Inspect.Should().BeSameAs(inspect);
	}

	[Theory]
	[MemberData(nameof(Overloads))]
	public async Task InspectionChangesNothingThatIsDrawn(string overload, Func<Inspect<Row>?, PlotContext<Row, double, double>> build)
	{
		// Arrange

		var plain = build(null).Style();
		var inspected = build(inspect).Style();

		// Act

		var expected = WithoutPlotId(await plain.AsStringAsync());
		var actual = WithoutPlotId(await inspected.AsStringAsync());

		// Assert

		actual.Should().Be(expected, overload);
	}

	// The per-instance plot id is the one part of the output that differs
	// between two renders of the same plot.
	private static string WithoutPlotId(string svg) => Regex.Replace(svg, @"gg(?!net-)[A-Za-z0-9_-]+", "ggID");

	[Fact]
	public void ContainingIsRefusedOffBars()
	{
		// Arrange

		var containing = inspect with { Policy = InspectPolicy.Containing };

		// Act

		Action line = () => Plot().Geom_Line(inspect: containing);
		Action point = () => Plot().Geom_Point(inspect: containing);
		Action area = () => Plot().Geom_Area(inspect: containing);
		Action bar = () => Plot().Geom_Bar(inspect: containing);

		// Assert

		using var _ = new AssertionScope();

		line.Should().Throw<GGNetUserException>().WithMessage("*Geom_Line*");
		point.Should().Throw<GGNetUserException>().WithMessage("*Geom_Point*");
		area.Should().Throw<GGNetUserException>().WithMessage("*Geom_Area*");
		bar.Should().NotThrow();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	public void ASeriesNeedsAName(string? name)
	{
		// Arrange

		// Act

		Action create = () => _ = new Inspect<Row>(name!, row => "x");

		// Assert

		create.Should().Throw<GGNetUserException>().WithMessage("*name*");
	}

	[Fact]
	public void ASeriesNeedsAValueFormatter()
	{
		// Arrange

		// Act

		Action create = () => _ = new Inspect<Row>("Series", null!);

		// Assert

		create.Should().Throw<GGNetUserException>().WithMessage("*value*");
	}
}

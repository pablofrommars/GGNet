using GGNet.Scales;

namespace GGNet.Headless.Tests;

public class ExplicitSourceInheritanceTests
{
	private interface IRow
	{
		double X { get; }
		double Y { get; }
		string Group { get; }
	}

	private sealed record Row(double X, double Y, string Group) : IRow;
	private sealed record OtherRow(double X, double Y, string Group) : IRow;

	private static readonly Row[] rows =
	[
		new(1, 2, "red"), new(2, 4, "red"), new(3, 3, "red"),
		new(1, 3, "blue"), new(2, 2, "blue"), new(3, 5, "blue")
	];

	private static readonly OtherRow[] otherRows = rows.Select(p => new OtherRow(p.X, p.Y, p.Group)).ToArray();

	public static IEnumerable<object[]> Overloads()
	{
		yield return ["Area", false, false];
		yield return ["Area", true, false];
		yield return ["Bar", false, false];
		yield return ["Bar", true, false];
		yield return ["Boxplot", false, false];
		yield return ["Boxplot", true, false];
		yield return ["ErrorBar", false, false];
		yield return ["ErrorBar", false, true];
		yield return ["ErrorBar", true, false];
		yield return ["ErrorBar", true, true];
		yield return ["Hex", false, false];
		yield return ["Hex", false, true];
		yield return ["Hex", true, false];
		yield return ["Hex", true, true];
		yield return ["Line", false, false];
		yield return ["Line", true, false];
		yield return ["Map", false, false];
		yield return ["Map", false, true];
		yield return ["Map", true, false];
		yield return ["Map", true, true];
		yield return ["Point", false, false];
		yield return ["Point", false, true];
		yield return ["Point", true, false];
		yield return ["Point", true, true];
		yield return ["Radar", false, false];
		yield return ["Radar", true, false];
		yield return ["Ribbon", false, false];
		yield return ["Ribbon", false, true];
		yield return ["Ribbon", true, false];
		yield return ["Ribbon", true, true];
		yield return ["RidgeLine", false, false];
		yield return ["RidgeLine", false, true];
		yield return ["RidgeLine", true, false];
		yield return ["RidgeLine", true, true];
		yield return ["Text", false, false];
		yield return ["Text", true, false];
		yield return ["Tile", false, false];
		yield return ["Tile", false, true];
		yield return ["Tile", true, false];
		yield return ["Violin", false, false];
		yield return ["Violin", false, true];
		yield return ["Violin", true, false];
		yield return ["Violin", true, true];
	}

	[Theory]
	[MemberData(nameof(Overloads))]
	public async Task TypedExplicitSourceOverloadsInheritUsedMappings(string family, bool panel, bool enumerable)
	{
		// Arrange

		var context = CreateContext();

		// Act

		AddLayer(context, rows, family, panel, enumerable);
		await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		Paints(context).Distinct().Should().BeEquivalentTo(["red", "blue"]);
		Mappings(context).Should().NotBeEmpty();
		foreach (var (name, mapping) in Mappings(context))
		{
			mapping.Should().BeSameAs(typeof(Data.Aesthetics<Row>).GetProperty(name)!.GetValue(context.Aesthetics));
		}
	}

	[Theory]
	[MemberData(nameof(Overloads))]
	public async Task OptOutAcceptsBothRowTypesAndKeepsConstantPaint(string family, bool panel, bool enumerable)
	{
		// Arrange

		var same = CreateContext();
		var different = CreateContext();

		// Act

		AddLayer(same, rows, family, panel, enumerable, inherit: false);
		AddLayer(different, otherRows, family, panel, enumerable, inherit: false);
		await same.Style().AsStringAsync();
		await different.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		Paints(same).Should().NotBeEmpty().And.OnlyContain(p => p == "green");
		Paints(different).Should().Equal(Paints(same));
		Mappings(same).Should().OnlyContain(p => p.mapping == null);
		Mappings(different).Should().OnlyContain(p => p.mapping == null);
	}

	[Theory]
	[MemberData(nameof(Overloads))]
	public void IncompatibleInheritanceRejectsAtTheDslCallWithoutAddingLayer(string family, bool panel, bool enumerable)
	{
		// Arrange

		var context = CreateContext();

		// Act

		var act = () => AddLayer(context, otherRows, family, panel, enumerable);

		// Assert

		using var _ = new AssertionScope();
		act.Should().Throw<GGNetUserException>().WithMessage("*Cannot inherit *")
			.Which.Message.Should().Contain($"'{typeof(Row)}'").And.Contain($"'{typeof(OtherRow)}'")
			.And.Contain("explicit").And.Contain("inherit: false");
		context.Default_Panel().Build((0, 0)).Geoms.Should().BeEmpty();
		context.CoordSystem.Should().Be(CoordSystem.Cartesian);
	}

	[Theory]
	[MemberData(nameof(Overloads))]
	public async Task ExplicitMappingsOverrideIncompatiblePlotMappings(string family, bool panel, bool enumerable)
	{
		// Arrange

		var context = CreateContext();

		// Act

		AddLayer(context, otherRows, family, panel, enumerable, explicitMappings: true);
		await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		Paints(context).Should().NotBeEmpty().And.OnlyContain(p => p == "purple");
		Mappings(context).Should().OnlyContain(p => p.mapping != null);
	}

	[Theory]
	[InlineData("Area")]
	[InlineData("Line")]
	[InlineData("Radar")]
	[InlineData("Ribbon")]
	[InlineData("RidgeLine")]
	[InlineData("Violin")]
	public async Task ExplicitSourceInheritanceControlsSeriesGrouping(string family)
	{
		// Arrange

		var inherited = CreateContext();
		var optedOut = CreateContext();
		AddLayer(inherited, rows, family, false, false);
		AddLayer(optedOut, rows, family, false, false, inherit: false);

		// Act

		await inherited.Style().AsStringAsync();
		await optedOut.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		inherited.Panels[0].Geoms[0].Layer.Should().HaveCount(2);
		optedOut.Panels[0].Geoms[0].Layer.Should().ContainSingle();
	}

	[Fact]
	public async Task PartialExplicitPointMappingRetainsInheritedSizeAndCapturesAtDslCall()
	{
		// Arrange

		var context = CreateContext();
		var originalSize = context.Aesthetics.Size;
		var explicitColor = new ConstantMapping<Row, string>("purple");
		context.Geom_Point(rows, p => p.X, p => p.Y, colorBy: explicitColor);
		context.Scale_Color_Identity(_ => "orange").Scale_Size_Identity(_ => 100);

		// Act

		await context.Style().AsStringAsync();
		var geom = (Geoms.Point.Point<Row, double, double>)context.Panels[0].Geoms[0];

		// Assert

		using var _ = new AssertionScope();
		geom.Aesthetics.Size.Should().BeSameAs(originalSize);
		geom.Aesthetics.Color.Should().BeSameAs(explicitColor);
		geom.Layer.Select(p => (Shapes.Circle)p.Value).Select(p => p.Aesthetic.Radius).Should().Equal(rows.Select(p => p.Y));
		Paints(context).Should().OnlyContain(p => p == "purple");
	}

	[Theory]
	[InlineData("Size")]
	[InlineData("LineType")]
	public void ExplicitColorDoesNotHideIncompatibleRemainingMapping(string aesthetic)
	{
		// Arrange

		var context = CreateContext();
		var color = new ConstantMapping<OtherRow, string>("purple");

		// Act

		Action act = aesthetic == "Size"
			? () => context.Geom_Point(otherRows, p => p.X, p => p.Y, colorBy: color)
			: () => context.Geom_Line(otherRows, p => p.X, p => p.Y, colorBy: color);

		// Assert

		act.Should().Throw<GGNetUserException>().WithMessage($"*{char.ToLowerInvariant(aesthetic[0])}{aesthetic[1..]}By*inherit: false*");
	}

	[Fact]
	public async Task UnusedIncompatibleAestheticsDoNotRejectLayer()
	{
		// Arrange

		var context = PlotContext.Build(rows, p => p.X, p => p.Y)
			.Scale_Fill_Identity(p => p.Group).Scale_LineType_Identity(_ => LineType.Dashed);
		context.Geom_Point(otherRows, p => p.X, p => p.Y, color: "green");

		// Act

		await context.Style().AsStringAsync();

		// Assert

		Paints(context).Should().HaveCount(rows.Length).And.OnlyContain(p => p == "green");
	}

	[Fact]
	public async Task ExplicitSourcesStillRequireSelectors()
	{
		// Arrange

		var context = CreateContext().Geom_Point(rows, inherit: false).Style();

		// Act

		var act = async () => await context.AsStringAsync();

		// Assert

		await act.Should().ThrowAsync<GGNetUserException>().WithMessage("X selector is required");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task PolygonSugarForwardsOptOutAndRejectsIncompatibleFill(bool panel)
	{
		// Arrange

		var context = CreateContext();
		var polygons = Polygons(rows[0]);

		// Act

		Action act = panel ? () => context.Default_Panel().Geom_Map(polygons) : () => context.Geom_Map(polygons);
		var error = Record.Exception(act);
		if (panel)
		{
			context.Default_Panel().Geom_Map(polygons, fill: "green", inherit: false);
		}
		else
		{
			context.Geom_Map(polygons, fill: "green", inherit: false);
		}
		await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		error.Should().BeOfType<GGNetUserException>().Which.Message.Should().Contain("fillBy").And.Contain("inherit: false");
		Paints(context).Should().Equal("green");
	}

	[Fact]
	public async Task IteratorSourceIsMaterializedOnceAndInheritsMappings()
	{
		// Arrange

		var context = CreateContext();
		var items = rows.ToList();
		var enumerations = 0;
		IEnumerable<Row> Enumerate()
		{
			enumerations++;
			foreach (var item in items)
			{
				yield return item;
			}
		}

		// Act

		context.Geom_Point(Enumerate(), p => p.X, p => p.Y);
		var enumerationsAtConstruction = enumerations;
		items.Clear();
		await context.Style().AsStringAsync();
		await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		enumerationsAtConstruction.Should().Be(1);
		enumerations.Should().Be(1);
		Paints(context).Should().Equal(rows.Select(p => p.Group));
		Mappings(context).Single(p => p.name == "Color").mapping.Should().BeSameAs(context.Aesthetics.Color);
		Mappings(context).Single(p => p.name == "Size").mapping.Should().BeSameAs(context.Aesthetics.Size);
	}

	[Fact]
	public async Task MappingImplementingBothRowInterfacesCanBeInherited()
	{
		// Arrange

		var context = PlotContext.Build(rows, p => p.X, p => p.Y);
		var mapping = new DualMapping();
		context.Aesthetics.Color = mapping;
		context.Geom_Point(otherRows, p => p.X, p => p.Y);

		// Act

		await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		Mappings(context).Single(p => p.name == "Color").mapping.Should().BeSameAs(mapping);
		Paints(context).Should().HaveCount(otherRows.Length).And.OnlyContain(p => p == "purple");
	}

	[Fact]
	public void SharedRowInterfaceDoesNotMakeAnInvariantMappingCompatible()
	{
		// Arrange

		IReadOnlyList<IRow> plotRows = rows;
		var context = PlotContext.Build(plotRows, p => p.X, p => p.Y).Scale_Color_Identity(p => p.Group);

		// Act

		var act = () => context.Geom_Point(rows, p => p.X, p => p.Y);

		// Assert

		act.Should().Throw<GGNetUserException>().WithMessage("*colorBy*IRow*Row*inherit: false*");
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task AddingMappingsLaterDoesNotRetroactivelyChangeLayers(bool explicitSource, bool inherit)
	{
		// Arrange

		var context = PlotContext.Build(rows, p => p.X, p => p.Y);
		if (explicitSource)
		{
			context.Geom_Point(rows, p => p.X, p => p.Y, color: "green", inherit: inherit);
		}
		else
		{
			context.Geom_Point(color: "green", inherit: inherit);
		}
		context.Scale_Color_Identity(p => p.Group);

		// Act

		await context.Style().AsStringAsync();

		// Assert

		Paints(context).Should().HaveCount(rows.Length).And.OnlyContain(p => p == "green");
	}

	[Fact]
	public async Task PartialExplicitLineMappingRetainsInheritedDash()
	{
		// Arrange

		var context = CreateContext();
		var color = new ConstantMapping<Row, string>("purple");
		context.Geom_Line(rows, p => p.X, p => p.Y, colorBy: color);

		// Act

		await context.Style().AsStringAsync();
		var geom = (Geoms.Line.Line<Row, double, double>)context.Panels[0].Geoms[0];

		// Assert

		using var _ = new AssertionScope();
		geom.Aesthetics.Color.Should().BeSameAs(color);
		geom.Aesthetics.LineType.Should().BeSameAs(context.Aesthetics.LineType);
		geom.Layer.Select(p => (Shapes.Path)p.Value).Select(p => p.Aesthetic.LineType)
			.Should().BeEquivalentTo([LineType.Solid, LineType.Dashed]);
		Paints(context).Should().OnlyContain(p => p == "purple");
	}

	private sealed class DualMapping : IAestheticMapping<Row, string>, IAestheticMapping<OtherRow, string>
	{
		public bool Guide => false;
		public string? Name => null;
		public Guide Type => GGNet.Guide.None;
		public IEnumerable<(string value, string label)> Labels => [];
		public void Train(Row item) { }
		public void Train(OtherRow item) { }
		public string Map(Row item) => "orange";
		public string Map(OtherRow item) => "purple";
	}

	private static PlotContext<Row, double, double> CreateContext()
		=> PlotContext.Build(rows, p => p.X, p => p.Y)
			.Scale_Color_Identity(p => p.Group).Scale_Fill_Identity(p => p.Group)
			.Scale_Size_Identity(p => p.Y).Scale_LineType_Identity(p => p.Group == "red" ? LineType.Solid : LineType.Dashed);

	private sealed class ConstantMapping<T, TValue>(TValue value) : IAestheticMapping<T, TValue>
	{
		public bool Guide => false;
		public string? Name => null;
		public Guide Type => GGNet.Guide.None;
		public IEnumerable<(TValue value, string label)> Labels => [];
		public void Train(T item) { }
		public TValue Map(T item) => value;
	}

	private static IEnumerable<(string name, object? mapping)> Mappings(PlotContext<Row, double, double> context)
	{
		var geom = context.Panels[0].Geoms[0];
		var aesthetics = geom.GetType().GetProperty("Aesthetics")!.GetValue(geom)!;
		return aesthetics.GetType().GetProperties().Select(p => (p.Name, p.GetValue(aesthetics)));
	}

	private static IEnumerable<string?> Paints(PlotContext<Row, double, double> context)
		=> context.Panels[0].Geoms[0].Layer.Select(p => p.Value switch
		{
			Shapes.Area area => area.Aesthetic.Fill,
			Shapes.Circle circle => circle.Aesthetic.Fill,
			Shapes.Rectangle rectangle => rectangle.Aesthetic.Fill,
			Shapes.Polygon polygon => polygon.Aesthetic.Fill,
			Shapes.MultiPolygon polygon => polygon.Aesthetic.Fill,
			Shapes.Text text => text.Aesthetic.Color,
			Shapes.Path path => path.Aesthetic.Stroke,
			_ => null
		}).Where(p => p is not null);

	private static Geospacial.Polygon[] Polygons(IRow item)
		=> [new() { Longitude = [item.X, item.X + 0.5, item.X], Latitude = [item.Y, item.Y, item.Y + 0.5] }];

	private static void AddLayer<T>(PlotContext<Row, double, double> context, IReadOnlyList<T> source,
		string family, bool panel, bool enumerable, bool inherit = true, bool explicitMappings = false)
		where T : IRow
	{
		var paint = explicitMappings ? new ConstantMapping<T, string>("purple") : null;
		var size = explicitMappings ? new ConstantMapping<T, double>(7) : null;
		var dash = explicitMappings ? new ConstantMapping<T, LineType>(LineType.Dotted) : null;
		switch (family, panel, enumerable)
		{
			case ("Area", false, false):
				context.Geom_Area(source, p => p.X, p => p.Y, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Area", true, false):
				context.Default_Panel().Geom_Area(source, p => p.X, p => p.Y, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Bar", false, false):
				context.Geom_Bar(source, p => p.X, p => p.Y, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Bar", true, false):
				context.Default_Panel().Geom_Bar(source, p => p.X, p => p.Y, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Boxplot", false, false):
				context.Geom_Boxplot(source, p => p.X, _ => 1.0, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Boxplot", true, false):
				context.Default_Panel().Geom_Boxplot(source, p => p.X, _ => 1.0, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("ErrorBar", false, false):
				context.Geom_ErrorBar(source, p => p.X, p => p.Y, p => p.Y - 1, p => p.Y + 1, colorBy: paint, color: "green", inherit: inherit);
				break;
			case ("ErrorBar", false, true):
				context.Geom_ErrorBar((IEnumerable<T>)source, p => p.X, p => p.Y, p => p.Y - 1, p => p.Y + 1, colorBy: paint, color: "green", inherit: inherit);
				break;
			case ("ErrorBar", true, false):
				context.Default_Panel().Geom_ErrorBar(source, p => p.X, p => p.Y, p => p.Y - 1, p => p.Y + 1, colorBy: paint, color: "green", inherit: inherit);
				break;
			case ("ErrorBar", true, true):
				context.Default_Panel().Geom_ErrorBar((IEnumerable<T>)source, p => p.X, p => p.Y, p => p.Y - 1, p => p.Y + 1, colorBy: paint, color: "green", inherit: inherit);
				break;
			case ("Hex", false, false):
				context.Geom_Hex(source, p => p.X, p => p.Y, _ => 0.2, _ => 0.2, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Hex", false, true):
				context.Geom_Hex((IEnumerable<T>)source, p => p.X, p => p.Y, _ => 0.2, _ => 0.2, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Hex", true, false):
				context.Default_Panel().Geom_Hex(source, p => p.X, p => p.Y, _ => 0.2, _ => 0.2, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Hex", true, true):
				context.Default_Panel().Geom_Hex((IEnumerable<T>)source, p => p.X, p => p.Y, _ => 0.2, _ => 0.2, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Line", false, false):
				context.Geom_Line(source, p => p.X, p => p.Y, colorBy: paint, lineTypeBy: dash, color: "green", inherit: inherit);
				break;
			case ("Line", true, false):
				context.Default_Panel().Geom_Line(source, p => p.X, p => p.Y, colorBy: paint, lineTypeBy: dash, color: "green", inherit: inherit);
				break;
			case ("Map", false, false):
				context.Geom_Map(source, p => Polygons(p), fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Map", false, true):
				context.Geom_Map((IEnumerable<T>)source, p => Polygons(p), fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Map", true, false):
				context.Default_Panel().Geom_Map(source, p => Polygons(p), fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Map", true, true):
				context.Default_Panel().Geom_Map((IEnumerable<T>)source, p => Polygons(p), fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Point", false, false):
				context.Geom_Point(source, p => p.X, p => p.Y, colorBy: paint, sizeBy: size, color: "green", inherit: inherit);
				break;
			case ("Point", false, true):
				context.Geom_Point((IEnumerable<T>)source, p => p.X, p => p.Y, colorBy: paint, sizeBy: size, color: "green", inherit: inherit);
				break;
			case ("Point", true, false):
				context.Default_Panel().Geom_Point(source, p => p.X, p => p.Y, colorBy: paint, sizeBy: size, color: "green", inherit: inherit);
				break;
			case ("Point", true, true):
				context.Default_Panel().Geom_Point((IEnumerable<T>)source, p => p.X, p => p.Y, colorBy: paint, sizeBy: size, color: "green", inherit: inherit);
				break;
			case ("Radar", false, false):
				context.Geom_Radar(source, p => p.X, p => p.Y, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Radar", true, false):
				context.Default_Panel().Geom_Radar(source, p => p.X, p => p.Y, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Ribbon", false, false):
				context.Geom_Ribbon(source, p => p.X, p => p.Y - 1, p => p.Y + 1, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Ribbon", false, true):
				context.Geom_Ribbon((IEnumerable<T>)source, p => p.X, p => p.Y - 1, p => p.Y + 1, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Ribbon", true, false):
				context.Default_Panel().Geom_Ribbon(source, p => p.X, p => p.Y - 1, p => p.Y + 1, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Ribbon", true, true):
				context.Default_Panel().Geom_Ribbon((IEnumerable<T>)source, p => p.X, p => p.Y - 1, p => p.Y + 1, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("RidgeLine", false, false):
				context.Geom_RidgeLine(source, p => p.X, _ => 1.0, p => p.Y, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("RidgeLine", false, true):
				context.Geom_RidgeLine((IEnumerable<T>)source, p => p.X, _ => 1.0, p => p.Y, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("RidgeLine", true, false):
				context.Default_Panel().Geom_RidgeLine(source, p => p.X, _ => 1.0, p => p.Y, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("RidgeLine", true, true):
				context.Default_Panel().Geom_RidgeLine((IEnumerable<T>)source, p => p.X, _ => 1.0, p => p.Y, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Text", false, false):
				context.Geom_Text(source, p => p.X, p => p.Y, text: p => p.Group, colorBy: paint, color: "green", inherit: inherit);
				break;
			case ("Text", true, false):
				context.Default_Panel().Geom_Text(source, p => p.X, p => p.Y, text: p => p.Group, colorBy: paint, color: "green", inherit: inherit);
				break;
			case ("Tile", false, false):
				context.Geom_Tile(source, p => p.X, p => p.Y, _ => 0.2, _ => 0.2, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Tile", false, true):
				context.Geom_Tile((IEnumerable<T>)source, p => p.X, p => p.Y, _ => 0.2, _ => 0.2, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Tile", true, false):
				context.Default_Panel().Geom_Tile(source, p => p.X, p => p.Y, _ => 0.2, _ => 0.2, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Violin", false, false):
				context.Geom_Violin(source, _ => 1.0, p => p.Y, p => p.Y / 10, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Violin", false, true):
				context.Geom_Violin((IEnumerable<T>)source, _ => 1.0, p => p.Y, p => p.Y / 10, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Violin", true, false):
				context.Default_Panel().Geom_Violin(source, _ => 1.0, p => p.Y, p => p.Y / 10, fillBy: paint, fill: "green", inherit: inherit);
				break;
			case ("Violin", true, true):
				context.Default_Panel().Geom_Violin((IEnumerable<T>)source, _ => 1.0, p => p.Y, p => p.Y / 10, fillBy: paint, fill: "green", inherit: inherit);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(family));
		}
	}
}

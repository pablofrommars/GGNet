using System.Collections;
using System.Collections.Immutable;
using GGNet.Shapes;

namespace GGNet.Headless.Tests;

public class SourceIdentityTests
{
	private sealed record Row(double X, double Y, int Group = 1);

	public static IEnumerable<object[]> Paths()
	{
		yield return ["Point", "Context"];
		yield return ["Point", "Panel"];
		yield return ["Point", "InheritedContext"];
		yield return ["Point", "InheritedPanel"];
		yield return ["Point", "EnumerableContext"];
		yield return ["Point", "EnumerablePanel"];
		yield return ["Tile", "Context"];
		yield return ["Tile", "Panel"];
		yield return ["Tile", "InheritedContext"];
		yield return ["Tile", "InheritedPanel"];
		yield return ["Tile", "EnumerableContext"];
		yield return ["Segment", "Context"];
		yield return ["Segment", "Panel"];
		yield return ["Segment", "InheritedContext"];
		yield return ["Segment", "InheritedPanel"];
		yield return ["Segment", "EnumerableContext"];
		yield return ["ABLine", "Context"];
		yield return ["ABLine", "Panel"];
		yield return ["ABLine", "InheritedContext"];
		yield return ["ABLine", "InheritedPanel"];
		yield return ["ABLine", "EnumerableContext"];
		yield return ["ABLine", "EnumerablePanel"];
		yield return ["ErrorBar", "Context"];
		yield return ["ErrorBar", "Panel"];
		yield return ["ErrorBar", "InheritedContext"];
		yield return ["ErrorBar", "InheritedPanel"];
		yield return ["ErrorBar", "EnumerableContext"];
		yield return ["ErrorBar", "EnumerablePanel"];
		yield return ["Hex", "Context"];
		yield return ["Hex", "Panel"];
		yield return ["Hex", "InheritedContext"];
		yield return ["Hex", "InheritedPanel"];
		yield return ["Hex", "EnumerableContext"];
		yield return ["Hex", "EnumerablePanel"];
		yield return ["Map", "Context"];
		yield return ["Map", "Panel"];
		yield return ["Map", "InheritedContext"];
		yield return ["Map", "InheritedPanel"];
		yield return ["Map", "EnumerableContext"];
		yield return ["Map", "EnumerablePanel"];
		yield return ["Ribbon", "Context"];
		yield return ["Ribbon", "Panel"];
		yield return ["Ribbon", "InheritedContext"];
		yield return ["Ribbon", "InheritedPanel"];
		yield return ["Ribbon", "EnumerableContext"];
		yield return ["Ribbon", "EnumerablePanel"];
		yield return ["RidgeLine", "Context"];
		yield return ["RidgeLine", "Panel"];
		yield return ["RidgeLine", "InheritedContext"];
		yield return ["RidgeLine", "InheritedPanel"];
		yield return ["RidgeLine", "EnumerableContext"];
		yield return ["RidgeLine", "EnumerablePanel"];
		yield return ["Violin", "Context"];
		yield return ["Violin", "Panel"];
		yield return ["Violin", "InheritedContext"];
		yield return ["Violin", "InheritedPanel"];
		yield return ["Violin", "EnumerableContext"];
		yield return ["Violin", "EnumerablePanel"];
	}

	[Theory]
	[MemberData(nameof(Paths))]
	public async Task MutableListsStayLive(string family, string path)
	{
		// Arrange

		var rows = new List<Row> { new(1, 2) };
		var context = PlotContext.Build(rows, r => r.X, r => r.Y);
		Add(context, family, path, rows);
		rows.Add(new(2, 3));

		// Act

		await context.Style().AsStringAsync();
		var first = Counts(context, family);
		rows.Add(new(3, 4));
		await context.AsStringAsync();
		var second = Counts(context, family);

		// Assert

		using var _ = new AssertionScope();
		first.Should().Equal(2);
		second.Should().Equal(3);
	}

	[Theory]
	[MemberData(nameof(Paths))]
	public async Task SameSourcePartitionsFacets(string family, string path)
	{
		// Arrange

		var rows = new List<Row> { new(1, 2, 1), new(2, 3, 1), new(3, 4, 2) };
		var context = PlotContext.Build(rows, r => r.X, r => r.Y).Facet_Wrap(r => r.Group);
		Add(context, family, path, rows);

		// Act

		await context.Style().AsStringAsync();

		// Assert

		Counts(context, family).Should().BeEquivalentTo([2, 1]);
	}

	[Theory]
	[MemberData(nameof(ExplicitPaths))]
	public async Task DistinctListsRepeatAcrossFacets(string family, string path)
	{
		// Arrange

		var rows = new List<Row> { new(1, 2, 1), new(2, 3, 1), new(3, 4, 2) };
		var context = PlotContext.Build(rows, r => r.X, r => r.Y).Facet_Wrap(r => r.Group);
		Add(context, family, path, rows.ToList());

		// Act

		await context.Style().AsStringAsync();

		// Assert

		Counts(context, family).Should().Equal(3, 3);
	}

	public static IEnumerable<object[]> ExplicitPaths() => Paths().Where(p => !((string)p[1]).StartsWith("Inherited", StringComparison.Ordinal));

	[Theory]
	[MemberData(nameof(Paths))]
	public async Task SharedStatMarkerSurvivesAndRecomputesOncePerLaterPass(string family, string path)
	{
		// Arrange

		var rows = new List<Row> { new(1, 2) };
		var source = new CountingStat(rows);
		var context = PlotContext.Build(source, r => r.X, r => r.Y);
		Add(context, family, path, source);
		await context.Style().AsStringAsync();
		var baseline = source.Recomputations;

		// Act

		rows.Add(new(2, 3));
		await context.AsStringAsync();
		var second = Counts(context, family);
		rows.Add(new(3, 4));
		await context.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		source.Recomputations.Should().Be(baseline + 2);
		context.Panels.SelectMany(p => p.Geoms).Should().OnlyContain(g => ReferenceEquals(g.StatSource, source));
		second.Should().Equal(2);
		Counts(context, family).Should().Equal(3);
	}

	[Theory]
	[MemberData(nameof(ExplicitPaths))]
	public async Task IndependentLayerStatsRefreshOnLaterPasses(string family, string path)
	{
		// Arrange

		var rows = new List<Row> { new(1, 2) };
		var source = new CountingStat(rows);
		var context = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		Add(context, family, path, source);
		await context.Style().AsStringAsync();
		var baseline = source.Recomputations;

		// Act

		rows.Add(new(2, 3));
		await context.AsStringAsync();
		var second = Counts(context, family);
		rows.Add(new(3, 4));
		await context.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		source.Recomputations.Should().Be(baseline + 2);
		second.Should().Equal(2);
		Counts(context, family).Should().Equal(3);
	}

	public static IEnumerable<object[]> EnumerablePaths() => Paths().Where(p => ((string)p[1]).StartsWith("Enumerable", StringComparison.Ordinal));

	[Theory]
	[MemberData(nameof(EnumerablePaths))]
	public async Task NonListEnumerablesMaterializeOnceAtConstruction(string family, string path)
	{
		// Arrange

		var rows = new List<Row> { new(1, 2) };
		var iterations = 0;
		IEnumerable<Row> Iterate()
		{
			iterations++;
			foreach (var row in rows)
			{
				yield return row;
			}
		}
		var context = PlotContext.Build(rows, r => r.X, r => r.Y);

		// Act

		AddEnumerable(context, family, path == "EnumerablePanel", Iterate());
		var atConstruction = iterations;
		rows.Add(new(2, 3));
		await context.Style().AsStringAsync();
		await context.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		atConstruction.Should().Be(1);
		iterations.Should().Be(1);
		Counts(context, family).Should().Equal(1);
	}

	[Theory]
	[MemberData(nameof(EnumerablePaths))]
	public void RuntimeListsAreNotEnumeratedDuringConversion(string family, string path)
	{
		// Arrange

		var source = new CountingStat([new(1, 2)]);
		var context = PlotContext.Build(source, r => r.X, r => r.Y);

		// Act

		AddEnumerable(context, family, path == "EnumerablePanel", source);

		// Assert

		source.Enumerations.Should().Be(0);
	}

	[Fact]
	public void SourceConstructorStillCopies()
	{
		// Arrange

		var rows = new List<Row> { new(1, 2) };

		// Act

		var source = new Source<Row>(rows);
		rows.Add(new(2, 3));

		// Assert

		source.Should().ContainSingle();
	}

	[Fact]
	public void CanonicalMemberSignaturesUseReadOnlyListsWithoutSourceShims()
	{
		// Arrange

		var families = Paths().Select(p => "Geom_" + (string)p[0]).Distinct().ToArray();

		// Act

		var parameters = typeof(BuilderExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(m => families.Contains(m.Name))
			.SelectMany(m => m.GetParameters()).Where(p => p.Name == "source").ToArray();

		// Assert

		using var _ = new AssertionScope();
		parameters.Count(p => p.ParameterType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)).Should().Be(20);
		parameters.Count(p => p.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)).Should().Be(18);
		parameters.Should().NotContain(p => p.ParameterType.GetGenericTypeDefinition() == typeof(Source<>));
	}

	private static int[] Counts(PlotContext<Row, double, double> context, string family) => context.Panels
		.SelectMany(p => p.Geoms).Select(g => family switch
		{
			"Ribbon" or "RidgeLine" => g.Layer.Sum(s => s is Area area ? area.Points.Count : 0),
			"Violin" => g.Layer.Sum(s => s is Polygon polygon ? polygon.Path.Longitude.Length / 2 : 0),
			"ErrorBar" => g.Layer.Count(s => s is Circle),
			_ => g.Layer.Count
		}).ToArray();

	private static Geospacial.Polygon[] Polygons(Row row) =>
	[
		new() { Longitude = [row.X, row.X + 0.5, row.X], Latitude = [row.Y, row.Y, row.Y + 0.5] }
	];

	private static void Add(PlotContext<Row, double, double> context, string family, string path, IReadOnlyList<Row> source)
	{
		if (path.StartsWith("Enumerable", StringComparison.Ordinal))
		{
			AddEnumerable(context, family, path == "EnumerablePanel", source);
			return;
		}

		switch ((family, path))
		{
			case ("Point", "Context"):
				context.Geom_Point(source, x: r => r.X, y: r => r.Y);
				break;
			case ("Point", "Panel"):
				context.Default_Panel().Geom_Point(source, x: r => r.X, y: r => r.Y);
				break;
			case ("Point", "InheritedContext"):
				context.Geom_Point(x: r => r.X, y: r => r.Y);
				break;
			case ("Point", "InheritedPanel"):
				context.Default_Panel().Geom_Point(x: r => r.X, y: r => r.Y);
				break;
			case ("Tile", "Context"):
				context.Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5);
				break;
			case ("Tile", "Panel"):
				context.Default_Panel().Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5);
				break;
			case ("Tile", "InheritedContext"):
				context.Geom_Tile(x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5);
				break;
			case ("Tile", "InheritedPanel"):
				context.Default_Panel().Geom_Tile(x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5);
				break;
			case ("Segment", "Context"):
				context.Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5);
				break;
			case ("Segment", "Panel"):
				context.Default_Panel().Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5);
				break;
			case ("Segment", "InheritedContext"):
				context.Geom_Segment(x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5);
				break;
			case ("Segment", "InheritedPanel"):
				context.Default_Panel().Geom_Segment(x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5);
				break;
			case ("ABLine", "Context"):
				context.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference");
				break;
			case ("ABLine", "Panel"):
				context.Default_Panel().Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference");
				break;
			case ("ABLine", "InheritedContext"):
				context.Geom_ABLine(a: r => r.X, b: r => r.Y, label: _ => "reference");
				break;
			case ("ABLine", "InheritedPanel"):
				context.Default_Panel().Geom_ABLine(a: r => r.X, b: r => r.Y, label: _ => "reference");
				break;
			case ("ErrorBar", "Context"):
				context.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
				break;
			case ("ErrorBar", "Panel"):
				context.Default_Panel().Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
				break;
			case ("ErrorBar", "InheritedContext"):
				context.Geom_ErrorBar(x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
				break;
			case ("ErrorBar", "InheritedPanel"):
				context.Default_Panel().Geom_ErrorBar(x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
				break;
			case ("Hex", "Context"):
				context.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2);
				break;
			case ("Hex", "Panel"):
				context.Default_Panel().Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2);
				break;
			case ("Hex", "InheritedContext"):
				context.Geom_Hex(x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2);
				break;
			case ("Hex", "InheritedPanel"):
				context.Default_Panel().Geom_Hex(x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2);
				break;
			case ("Map", "Context"):
				context.Geom_Map(source, polygons: Polygons);
				break;
			case ("Map", "Panel"):
				context.Default_Panel().Geom_Map(source, polygons: Polygons);
				break;
			case ("Map", "InheritedContext"):
				context.Geom_Map(polygons: Polygons);
				break;
			case ("Map", "InheritedPanel"):
				context.Default_Panel().Geom_Map(polygons: Polygons);
				break;
			case ("Ribbon", "Context"):
				context.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
				break;
			case ("Ribbon", "Panel"):
				context.Default_Panel().Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
				break;
			case ("Ribbon", "InheritedContext"):
				context.Geom_Ribbon(x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
				break;
			case ("Ribbon", "InheritedPanel"):
				context.Default_Panel().Geom_Ribbon(x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
				break;
			case ("RidgeLine", "Context"):
				context.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5);
				break;
			case ("RidgeLine", "Panel"):
				context.Default_Panel().Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5);
				break;
			case ("RidgeLine", "InheritedContext"):
				context.Geom_RidgeLine(x: r => r.X, y: r => r.Y, height: _ => 0.5);
				break;
			case ("RidgeLine", "InheritedPanel"):
				context.Default_Panel().Geom_RidgeLine(x: r => r.X, y: r => r.Y, height: _ => 0.5);
				break;
			case ("Violin", "Context"):
				context.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5);
				break;
			case ("Violin", "Panel"):
				context.Default_Panel().Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5);
				break;
			case ("Violin", "InheritedContext"):
				context.Geom_Violin(x: r => r.X, y: r => r.Y, width: _ => 0.5);
				break;
			case ("Violin", "InheritedPanel"):
				context.Default_Panel().Geom_Violin(x: r => r.X, y: r => r.Y, width: _ => 0.5);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(path));
		}
	}

	private static void AddEnumerable(PlotContext<Row, double, double> context, string family, bool panel, IEnumerable<Row> source)
	{
		switch ((family, panel))
		{
			case ("Point", false):
				context.Geom_Point(source, x: r => r.X, y: r => r.Y);
				break;
			case ("Point", true):
				context.Default_Panel().Geom_Point(source, x: r => r.X, y: r => r.Y);
				break;
			case ("Tile", false):
				context.Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5);
				break;
			case ("Segment", false):
				context.Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5);
				break;
			case ("ABLine", false):
				context.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference");
				break;
			case ("ABLine", true):
				context.Default_Panel().Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference");
				break;
			case ("ErrorBar", false):
				context.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
				break;
			case ("ErrorBar", true):
				context.Default_Panel().Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
				break;
			case ("Hex", false):
				context.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2);
				break;
			case ("Hex", true):
				context.Default_Panel().Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2);
				break;
			case ("Map", false):
				context.Geom_Map(source, polygons: Polygons);
				break;
			case ("Map", true):
				context.Default_Panel().Geom_Map(source, polygons: Polygons);
				break;
			case ("Ribbon", false):
				context.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
				break;
			case ("Ribbon", true):
				context.Default_Panel().Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
				break;
			case ("RidgeLine", false):
				context.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5);
				break;
			case ("RidgeLine", true):
				context.Default_Panel().Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5);
				break;
			case ("Violin", false):
				context.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5);
				break;
			case ("Violin", true):
				context.Default_Panel().Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(family));
		}
	}

	private sealed class CountingStat(List<Row> input) : IReadOnlyList<Row>, IStatSource
	{
		private List<Row> rows = [.. input];

		public int Recomputations { get; private set; }

		public int Enumerations { get; private set; }

		public int Count => rows.Count;

		public Row this[int index] => rows[index];

		public void Recompute()
		{
			Recomputations++;
			rows = [.. input];
		}

		public IEnumerator<Row> GetEnumerator()
		{
			Enumerations++;
			return rows.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	[Fact]
	public async Task ArrayBindsAcrossSupportedReceivers()
	{
		// Arrange

		var context = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		var panelContext = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		Row[] source = [new(1, 2)];

		// Act

		context.Geom_Point(source, x: r => r.X, y: r => r.Y);
		panelContext.Panel(p => p.Geom_Point(source, x: r => r.X, y: r => r.Y));
		context.Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5);
		panelContext.Panel(p => p.Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5));
		context.Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5));
		context.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference");
		panelContext.Panel(p => p.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference"));
		context.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2);
		panelContext.Panel(p => p.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2));
		context.Geom_Map(source, polygons: Polygons);
		panelContext.Panel(p => p.Geom_Map(source, polygons: Polygons));
		context.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5);
		panelContext.Panel(p => p.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5));
		context.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5);
		panelContext.Panel(p => p.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5));
		await context.Style().AsStringAsync();
		await panelContext.Style().AsStringAsync();

		// Assert

		context.Panels.Concat(panelContext.Panels).SelectMany(p => p.Geoms).Should().HaveCount(20);
	}

	[Fact]
	public async Task ListBindsAcrossSupportedReceivers()
	{
		// Arrange

		var context = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		var panelContext = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		var source = new List<Row> { new(1, 2) };

		// Act

		context.Geom_Point(source, x: r => r.X, y: r => r.Y);
		panelContext.Panel(p => p.Geom_Point(source, x: r => r.X, y: r => r.Y));
		context.Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5);
		panelContext.Panel(p => p.Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5));
		context.Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5));
		context.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference");
		panelContext.Panel(p => p.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference"));
		context.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2);
		panelContext.Panel(p => p.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2));
		context.Geom_Map(source, polygons: Polygons);
		panelContext.Panel(p => p.Geom_Map(source, polygons: Polygons));
		context.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5);
		panelContext.Panel(p => p.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5));
		context.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5);
		panelContext.Panel(p => p.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5));
		await context.Style().AsStringAsync();
		await panelContext.Style().AsStringAsync();

		// Assert

		context.Panels.Concat(panelContext.Panels).SelectMany(p => p.Geoms).Should().HaveCount(20);
	}

	[Fact]
	public async Task SourceBindsAcrossSupportedReceivers()
	{
		// Arrange

		var context = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		var panelContext = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		var source = new Source<Row>([new(1, 2)]);

		// Act

		context.Geom_Point(source, x: r => r.X, y: r => r.Y);
		panelContext.Panel(p => p.Geom_Point(source, x: r => r.X, y: r => r.Y));
		context.Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5);
		panelContext.Panel(p => p.Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5));
		context.Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5));
		context.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference");
		panelContext.Panel(p => p.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference"));
		context.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2);
		panelContext.Panel(p => p.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2));
		context.Geom_Map(source, polygons: Polygons);
		panelContext.Panel(p => p.Geom_Map(source, polygons: Polygons));
		context.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5);
		panelContext.Panel(p => p.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5));
		context.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5);
		panelContext.Panel(p => p.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5));
		await context.Style().AsStringAsync();
		await panelContext.Style().AsStringAsync();

		// Assert

		context.Panels.Concat(panelContext.Panels).SelectMany(p => p.Geoms).Should().HaveCount(20);
	}

	[Fact]
	public async Task StatSourceBindsAcrossSupportedReceivers()
	{
		// Arrange

		var context = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		var panelContext = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		var source = new StatSource<Row>(() => [new(1, 2)]);

		// Act

		context.Geom_Point(source, x: r => r.X, y: r => r.Y);
		panelContext.Panel(p => p.Geom_Point(source, x: r => r.X, y: r => r.Y));
		context.Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5);
		panelContext.Panel(p => p.Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5));
		context.Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5));
		context.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference");
		panelContext.Panel(p => p.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference"));
		context.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2);
		panelContext.Panel(p => p.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2));
		context.Geom_Map(source, polygons: Polygons);
		panelContext.Panel(p => p.Geom_Map(source, polygons: Polygons));
		context.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5);
		panelContext.Panel(p => p.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5));
		context.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5);
		panelContext.Panel(p => p.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5));
		await context.Style().AsStringAsync();
		await panelContext.Style().AsStringAsync();

		// Assert

		context.Panels.Concat(panelContext.Panels).SelectMany(p => p.Geoms).Should().HaveCount(20);
	}

	[Fact]
	public async Task RuntimeListBindsAcrossSupportedReceivers()
	{
		// Arrange

		var context = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		var panelContext = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		IEnumerable<Row> source = new List<Row> { new(1, 2) };

		// Act

		context.Geom_Point(source, x: r => r.X, y: r => r.Y);
		panelContext.Panel(p => p.Geom_Point(source, x: r => r.X, y: r => r.Y));
		context.Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5);
		context.Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5);
		context.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference");
		panelContext.Panel(p => p.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference"));
		context.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2);
		panelContext.Panel(p => p.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2));
		context.Geom_Map(source, polygons: Polygons);
		panelContext.Panel(p => p.Geom_Map(source, polygons: Polygons));
		context.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5);
		panelContext.Panel(p => p.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5));
		context.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5);
		panelContext.Panel(p => p.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5));
		await context.Style().AsStringAsync();
		await panelContext.Style().AsStringAsync();

		// Assert

		context.Panels.Concat(panelContext.Panels).SelectMany(p => p.Geoms).Should().HaveCount(18);
	}

	[Fact]
	public async Task IteratorBindsAcrossSupportedReceivers()
	{
		// Arrange

		var context = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		var panelContext = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		IEnumerable<Row> source = Enumerable.Range(1, 1).Select(i => new Row(i, 2));

		// Act

		context.Geom_Point(source, x: r => r.X, y: r => r.Y);
		panelContext.Panel(p => p.Geom_Point(source, x: r => r.X, y: r => r.Y));
		context.Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5);
		context.Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5);
		context.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference");
		panelContext.Panel(p => p.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference"));
		context.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2);
		panelContext.Panel(p => p.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2));
		context.Geom_Map(source, polygons: Polygons);
		panelContext.Panel(p => p.Geom_Map(source, polygons: Polygons));
		context.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5);
		panelContext.Panel(p => p.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5));
		context.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5);
		panelContext.Panel(p => p.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5));
		await context.Style().AsStringAsync();
		await panelContext.Style().AsStringAsync();

		// Assert

		context.Panels.Concat(panelContext.Panels).SelectMany(p => p.Geoms).Should().HaveCount(18);
	}

	[Fact]
	public async Task ImmutableArrayBindsAcrossSupportedReceivers()
	{
		// Arrange

		var context = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		var panelContext = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		var source = ImmutableArray.Create(new Row(1, 2));

		// Act

		context.Geom_Point(source, x: r => r.X, y: r => r.Y);
		panelContext.Panel(p => p.Geom_Point(source, x: r => r.X, y: r => r.Y));
		context.Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5);
		panelContext.Panel(p => p.Geom_Tile(source, x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5));
		context.Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_Segment(source, x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5));
		context.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference");
		panelContext.Panel(p => p.Geom_ABLine(source, a: r => r.X, b: r => r.Y, label: _ => "reference"));
		context.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_ErrorBar(source, x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2);
		panelContext.Panel(p => p.Geom_Hex(source, x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2));
		context.Geom_Map(source, polygons: Polygons);
		panelContext.Panel(p => p.Geom_Map(source, polygons: Polygons));
		context.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_Ribbon(source, x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5);
		panelContext.Panel(p => p.Geom_RidgeLine(source, x: r => r.X, y: r => r.Y, height: _ => 0.5));
		context.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5);
		panelContext.Panel(p => p.Geom_Violin(source, x: r => r.X, y: r => r.Y, width: _ => 0.5));
		await context.Style().AsStringAsync();
		await panelContext.Style().AsStringAsync();

		// Assert

		context.Panels.Concat(panelContext.Panels).SelectMany(p => p.Geoms).Should().HaveCount(20);
	}

	[Fact]
	public async Task CollectionExpressionBindsAcrossSupportedReceivers()
	{
		// Arrange

		var context = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);
		var panelContext = PlotContext.Build(new[] { new Row(0, 0) }, r => r.X, r => r.Y);

		// Act

		context.Geom_Point([new Row(1, 2)], x: r => r.X, y: r => r.Y);
		panelContext.Panel(p => p.Geom_Point([new Row(1, 2)], x: r => r.X, y: r => r.Y));
		context.Geom_Tile([new Row(1, 2)], x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5);
		panelContext.Panel(p => p.Geom_Tile([new Row(1, 2)], x: r => r.X, y: r => r.Y, width: _ => 0.5, height: _ => 0.5));
		context.Geom_Segment([new Row(1, 2)], x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_Segment([new Row(1, 2)], x: r => r.X, xend: r => r.X + 0.5, y: r => r.Y, yend: r => r.Y + 0.5));
		context.Geom_ABLine([new Row(1, 2)], a: r => r.X, b: r => r.Y, label: _ => "reference");
		panelContext.Panel(p => p.Geom_ABLine([new Row(1, 2)], a: r => r.X, b: r => r.Y, label: _ => "reference"));
		context.Geom_ErrorBar([new Row(1, 2)], x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_ErrorBar([new Row(1, 2)], x: r => r.X, y: r => r.Y, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_Hex([new Row(1, 2)], x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2);
		panelContext.Panel(p => p.Geom_Hex([new Row(1, 2)], x: r => r.X, y: r => r.Y, dx: _ => 0.2, dy: _ => 0.2));
		context.Geom_Map([new Row(1, 2)], polygons: Polygons);
		panelContext.Panel(p => p.Geom_Map([new Row(1, 2)], polygons: Polygons));
		context.Geom_Ribbon([new Row(1, 2)], x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5);
		panelContext.Panel(p => p.Geom_Ribbon([new Row(1, 2)], x: r => r.X, ymin: r => r.Y - 0.5, ymax: r => r.Y + 0.5));
		context.Geom_RidgeLine([new Row(1, 2)], x: r => r.X, y: r => r.Y, height: _ => 0.5);
		panelContext.Panel(p => p.Geom_RidgeLine([new Row(1, 2)], x: r => r.X, y: r => r.Y, height: _ => 0.5));
		context.Geom_Violin([new Row(1, 2)], x: r => r.X, y: r => r.Y, width: _ => 0.5);
		panelContext.Panel(p => p.Geom_Violin([new Row(1, 2)], x: r => r.X, y: r => r.Y, width: _ => 0.5));
		await context.Style().AsStringAsync();
		await panelContext.Style().AsStringAsync();

		// Assert

		context.Panels.Concat(panelContext.Panels).SelectMany(p => p.Geoms).Should().HaveCount(20);
	}
}

using System.Collections;
using GGNet.Shapes;

namespace GGNet.Headless.Tests;

public class FacetTraversalTests
{
	[Theory]
	[InlineData(false, 1, 1)]
	[InlineData(false, 4, 1)]
	[InlineData(false, 16, 1)]
	[InlineData(false, 1, 3)]
	[InlineData(false, 4, 3)]
	[InlineData(false, 16, 3)]
	[InlineData(true, 1, 1)]
	[InlineData(true, 4, 1)]
	[InlineData(true, 16, 1)]
	[InlineData(true, 1, 3)]
	[InlineData(true, 4, 3)]
	[InlineData(true, 16, 3)]
	public void SharedSourceTraversalDoesNotMultiplyByFacetCount(bool grid, int groups, int layers)
	{
		// Arrange

		const int count = 512;
		var source = new CountingList<Row>(Enumerable.Range(0, count).Select(i => new Row(i, i, i % groups, i % 2)).ToList());
		var selectors = 0;
		var context = PlotContext.Build(source, row => row.X, row => row.Y);
		for (var i = 0; i < layers; i++)
		{
			context.Geom_Point(source, row => row.X, row => row.Y);
		}
		if (grid)
		{
			context.Facet_Grid(row => { selectors++; return row.Group; }, row => { selectors++; return row.Column; });
		}
		else
		{
			context.Facet_Wrap(row => { selectors++; return row.Group; });
		}
		context.Init(grid: false);

		// Act

		context.Render();

		// Assert

		using var _ = new AssertionScope();
		source.IndexReads.Should().Be(count * (2 + 2 * layers));
		source.EnumeratedRows.Should().Be(0);
		selectors.Should().Be(count * (grid ? 4 : 2));
		context.Panels.SelectMany(panel => panel.Geoms).Sum(geom => geom.Layer.Count).Should().Be(count * layers);
		context.Panels.Should().HaveCount(grid ? groups * 2 : groups);
	}

	[Theory]
	[InlineData(false, false, false)]
	[InlineData(false, true, false)]
	[InlineData(false, false, true)]
	[InlineData(false, true, true)]
	[InlineData(true, false, false)]
	[InlineData(true, true, false)]
	[InlineData(true, false, true)]
	[InlineData(true, true, true)]
	public void BucketsMatchFilteringForShapesOrderStripsAndScales(bool grid, bool freeX, bool freeY)
	{
		// Arrange

		List<Row> rows = [new(40, 3, 2, 1), new(20, 30, 1, 0), new(10, 2, 2, 1), new(30, 40, 1, 0)];
		var shared = new CountingList<Row>(rows, equal: true);
		var filteredRoot = new CountingList<Row>(rows, equal: true);
		var filteredLayer = new CountingList<Row>(rows.ToList(), equal: true);
		var indexed = Create(shared, shared, grid, freeX, freeY);
		var filtered = Create(filteredRoot, filteredLayer, grid, freeX, freeY);

		// Act

		indexed.Render();
		filtered.Render();

		// Assert

		using var _ = new AssertionScope();
		indexed.N.Should().Be(filtered.N);
		indexed.Panels.Should().HaveCount(grid ? 4 : 2);
		indexed.Panels.Select(panel => panel.Geoms[0].Layer.Count).Should().Equal(grid ? [2, 0, 0, 2] : [2, 2]);
		for (var i = 0; i < indexed.Panels.Count; i++)
		{
			var actual = indexed.Panels[i];
			var expected = filtered.Panels[i];
			actual.Coord.Should().Be(expected.Coord);
			actual.Strip.Should().Be(expected.Strip);
			actual.Axis.Should().Be(expected.Axis);
			actual.XLab.Should().Be(expected.XLab);
			actual.YLab.Should().Be(expected.YLab);
			actual.Geoms[0].Layer.Should().Equal(expected.Geoms[0].Layer);
			actual.Geoms[1].Layer.Should().Equal(expected.Geoms[1].Layer);
			actual.X.Range.Should().Be(expected.X.Range);
			actual.Y.Range.Should().Be(expected.Y.Range);
			actual.X.Labels.Should().Equal(expected.X.Labels);
			actual.Y.Labels.Should().Equal(expected.Y.Labels);
		}
		indexed.Positions.X.Scales.Should().HaveCount(freeX ? indexed.Panels.Count : 1);
		indexed.Positions.Y.Scales.Should().HaveCount(freeY ? indexed.Panels.Count : 1);
		Circles(indexed).Select(circle => circle.X).Should().Equal(20, 30, 20, 30, 40, 10, 40, 10);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void EqualDistinctSourcesFilterTheirOwnPayloads(bool grid)
	{
		// Arrange

		var root = new CountingList<Row>([new(1, 2, 1), new(3, 4, 2)], equal: true);
		var layer = new CountingList<Row>([new(30, 40, 2), new(10, 20, 1), new(50, 60, 1)], equal: true);
		var context = Create(root, layer, grid, false, false);

		// Act

		context.Render();

		// Assert

		using var _ = new AssertionScope();
		Circles(context).Select(circle => (circle.X, circle.Y)).Should().Equal((10, 20), (50, 60), (10, 20), (50, 60), (30, 40), (30, 40));
		layer.IndexReads.Should().Be(2 * 2 * 2 * layer.Count);
		root.EqualityCalls.Should().Be(context.Panels.Count * 2);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void CopiedSourcesRepeatAcrossFacets(bool grid)
	{
		// Arrange

		List<Row> rows = [new(1, 2, 1), new(3, 4, 2)];
		var root = new CountingList<Row>(rows);
		var copy = new Source<Row>(rows);
		var context = Create(root, copy, grid, false, false);

		// Act

		context.Render();

		// Assert

		context.Panels.SelectMany(panel => panel.Geoms).Select(geom => geom.Layer.Count).Should().Equal(2, 2, 2, 2);
	}

	[Fact]
	public void ReferenceIdentityDoesNotOverrideCustomEqualityRejection()
	{
		// Arrange

		var source = new CountingList<Row>([new(1, 2, 1), new(3, 4, 2)], equal: false);
		var context = Create(source, source, false, false, false);

		// Act

		context.Render();

		// Assert

		using var _ = new AssertionScope();
		context.Panels.SelectMany(panel => panel.Geoms).Select(geom => geom.Layer.Count).Should().Equal(2, 2, 2, 2);
		source.EqualityCalls.Should().Be(4);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void CovariantSharedSourceWithDifferentFacetTypeRemainsUnfiltered(bool grid)
	{
		// Arrange

		IReadOnlyList<DerivedRow> source = new List<DerivedRow> { new(1, 2, 1), new(3, 4, 2) };
		IReadOnlyList<Row> covariant = source;
		var context = PlotContext.Build(source, row => row.X, row => row.Y)
			.Geom_Point(covariant, row => row.X, row => row.Y);
		if (grid)
		{
			context.Facet_Grid(row => row.Group, row => row.Column);
		}
		else
		{
			context.Facet_Wrap(row => row.Group);
		}
		context.Init(grid: false);

		// Act

		context.Render();

		// Assert

		using var _ = new AssertionScope();
		ReferenceEquals(source, covariant).Should().BeTrue();
		context.Panels.SelectMany(panel => panel.Geoms).Select(geom => geom.Layer.Count).Should().Equal(2, 2);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void MembershipRebuildsAfterReorderingChangingKeysEmptyingAndRefilling(bool grid)
	{
		// Arrange

		List<Row> rows = [new(1, 2, 1), new(3, 4, 2)];
		var context = Create(rows, rows, grid, true, true);
		context.Render();

		// Act

		rows.Reverse();
		rows[0] = new(30, 40, 3);
		rows.Add(new(5, 6, 1));
		context.Render();
		var changed = Circles(context).Select(circle => circle.X).ToArray();
		var changedStrips = context.Panels.Select(panel => panel.Strip).ToArray();
		rows.Clear();
		context.Render();
		var emptyPanels = context.Panels.Count;
		rows.Add(new(9, 10, 4));
		context.Render();

		// Assert

		using var _ = new AssertionScope();
		changed.Should().Equal(1, 5, 1, 5, 30, 30);
		changedStrips.Should().Equal(grid ? [("0", "1"), ("", "3")] : [("1", null), ("3", null)]);
		emptyPanels.Should().Be(0);
		Circles(context).Select(circle => circle.X).Should().Equal(9, 9);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void StatisticalRootRefreshPrecedesMembershipOnEveryPass(bool grid)
	{
		// Arrange

		List<Row> rows = [new(1, 2, 1)];
		var recomputations = 0;
		var source = new StatSource<Row>(() => { recomputations++; return rows.ToList(); });
		var context = Create(source, source, grid, false, false);
		rows.Add(new(3, 4, 2));

		// Act

		context.Render();
		var first = Circles(context).Select(circle => circle.X).ToArray();
		rows.Clear();
		context.Render();
		var emptyPanels = context.Panels.Count;
		rows.Add(new(5, 6, 3));
		context.Render();

		// Assert

		using var _ = new AssertionScope();
		first.Should().Equal(1, 1, 3, 3);
		emptyPanels.Should().Be(0);
		Circles(context).Select(circle => circle.X).Should().Equal(5, 5);
		recomputations.Should().Be(4);
	}

	[Theory]
	[InlineData(1, 1, 1)]
	[InlineData(1, 2, 2)]
	[InlineData(2, 1, 2)]
	public void UndersizedWrapDimensionsRejectBeforeAllocatingPanels(int rows, int columns, int capacity)
	{
		// Arrange

		var source = new CountingList<Row>(Enumerable.Range(0, 4).Select(i => new Row(i, i, i)).ToList());
		var selectors = 0;
		var context = PlotContext.Build(source, row => row.X, row => row.Y).Geom_Point()
			.Facet_Wrap(row => { selectors++; return row.Group; }, nrows: rows, ncolumns: columns);
		context.Init(grid: false);

		// Act

		var render = () => context.Render();

		// Assert

		using var _ = new AssertionScope();
		render.Should().Throw<GGNetUserException>().WithMessage(FormattableString.Invariant($"Facet_Wrap needs 4 panels, but nrows={rows} and ncolumns={columns} provide only {capacity} slots. Increase either dimension or omit one to derive it automatically."));
		context.Panels.Should().BeEmpty();
		source.IndexReads.Should().Be(4);
		selectors.Should().Be(4);
	}

	private static PlotContext<Row, double, double> Create(IReadOnlyList<Row> root, IReadOnlyList<Row> layer, bool grid, bool freeX, bool freeY)
	{
		var context = PlotContext.Build(root, row => row.X, row => row.Y)
			.Geom_Point(layer, row => row.X, row => row.Y)
			.Geom_Point(layer, row => row.X, row => row.Y)
			.XLab("horizontal").YLab("vertical");
		if (grid)
		{
			context.Facet_Grid(row => row.Group, row => row.Column, freeX: freeX, freeY: freeY);
		}
		else
		{
			context.Facet_Wrap(row => row.Group, freeX: freeX, freeY: freeY);
		}
		context.Init();
		return context;
	}

	private static IEnumerable<Circle> Circles(PlotContext<Row, double, double> context) => context.Panels
		.SelectMany(panel => panel.Geoms).SelectMany(geom => geom.Layer).Select(shape => shape is Circle circle ? circle : throw new InvalidOperationException("Expected a circle"));

	private record Row(double X, double Y, int Group, int Column = 0);

	private sealed record DerivedRow(double X, double Y, int Group) : Row(X, Y, Group);

	private sealed class CountingList<T>(List<T> items, bool? equal = null) : IReadOnlyList<T>
	{
		public int IndexReads { get; private set; }

		public int EnumeratedRows { get; private set; }

		public int EqualityCalls { get; private set; }

		public int Count => items.Count;

		public T this[int index]
		{
			get
			{
				IndexReads++;
				return items[index];
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			foreach (var item in items)
			{
				EnumeratedRows++;
				yield return item;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public override bool Equals(object? obj)
		{
			EqualityCalls++;
			return equal ?? ReferenceEquals(this, obj);
		}

		public override int GetHashCode() => 0;
	}
}

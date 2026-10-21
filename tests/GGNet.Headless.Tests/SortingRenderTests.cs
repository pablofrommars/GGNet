using GGNet.Geoms.Boxplot;
using GGNet.Scales;

namespace GGNet.Headless.Tests;

public class SortingRenderTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void LineOrdersUniquePointsAndRebuildsAfterSourceMutation(int order)
	{
		// Arrange

		Row[] rows = [new(1, 10), new(2, 20), new(3, 30), new(4, 40)];
		List<Row> source = order switch
		{
			0 => [.. rows],
			1 => [.. rows.Reverse()],
			_ => [rows[2], rows[0], rows[3], rows[1]]
		};
		var context = PlotContext.Build(source, row => row.X, row => row.Y).Geom_Line();
		context.Init(grid: false);

		// Act

		context.Render();
		var first = Items(Shapes<Shapes.Path>(context).Single().Points);
		source.Clear();
		source.AddRange([new(8, 80), new(6, 60), new(8, 800)]);
		context.Render();
		var second = Items(Shapes<Shapes.Path>(context).Single().Points);

		// Assert

		using var _ = new AssertionScope();
		first.Should().Equal((1, 10), (2, 20), (3, 30), (4, 40));
		second.Should().Equal((6, 60), (8, 80));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task PiecewiseDuplicateXRetainsTheFirstGapOrDataButKeepsInteractionMarks(bool gapFirst)
	{
		// Arrange

		var data = new Row(2, 20);
		var gap = new Row(2, double.NaN);
		Row[] source = [new(3, 30), gapFirst ? gap : data, gapFirst ? data : gap, new(1, 10)];
		var clicked = new List<Row>();
		var context = PlotContext.Build(source, row => row.X, row => row.Y)
			.Geom_Line(piecewise: true, onclick: (row, _) => { clicked.Add(row); return Task.CompletedTask; });
		context.Init(grid: false);

		// Act

		context.Render();
		var points = Items(Shapes<Shapes.Path>(context).Single().Points);
		var circles = Shapes<Shapes.Circle>(context);
		foreach (var circle in circles)
		{
			await circle.OnClick!(new());
		}

		// Assert

		using var _ = new AssertionScope();
		points.Select(point => point.x).Should().Equal(1, 2, 3);
		points[1].y.Should().Be(gapFirst ? double.NaN : 20);
		circles.Select(circle => circle.Y).Should().Equal(30, 20, 10);
		clicked[1].Should().BeSameAs(data);
	}

	[Theory]
	[InlineData(PositionAdjustment.Identity)]
	[InlineData(PositionAdjustment.Stack)]
	public async Task AreaDuplicateXRetainsTheFirstInteractionItem(PositionAdjustment adjustment)
	{
		// Arrange

		var retained = new Row(2, 20);
		Row[] source = [new(3, 30), retained, new(1, 10), new(2, 200)];
		var clicked = new List<Row>();
		var context = PlotContext.Build(source, row => row.X, row => row.Y)
			.Geom_Area(position: adjustment, onclick: (row, _) => { clicked.Add(row); return Task.CompletedTask; });
		context.Init(grid: false);

		// Act

		context.Render();
		var points = Items(Shapes<Shapes.Area>(context).Single().Points);
		foreach (var circle in Shapes<Shapes.Circle>(context))
		{
			await circle.OnClick!(new());
		}

		// Assert

		using var _ = new AssertionScope();
		points.Should().Equal((1, 0, 10), (2, 0, 20), (3, 0, 30));
		clicked.Select(row => row.X).Should().Equal(1, 2, 3);
		clicked[1].Should().BeSameAs(retained);
	}

	[Fact]
	public void StackedAreaInterpolatesInterleavedGridsAcrossThreeSeries()
	{
		// Arrange

		Row[] source =
		[
			new(4, 6, "A"), new(0, 2, "A"),
			new(3, 3, "B"), new(1, 1, "B"),
			new(4, 4, "C"), new(2, 2, "C"), new(0, 0, "C")
		];
		var context = PlotContext.Build(source, row => row.X, row => row.Y)
			.Scale_Fill_Discrete(row => row.Group, ["red", "blue", "green"], guide: false)
			.Geom_Area(position: PositionAdjustment.Stack);
		context.Init(grid: false);

		// Act

		context.Render();
		var areas = Shapes<Shapes.Area>(context);

		// Assert

		using var _ = new AssertionScope();
		areas.Should().HaveCount(3);
		Items(areas[0].Points).Should().Equal((0, 0, 2), (1, 0, 3), (2, 0, 4), (3, 0, 5), (4, 0, 6));
		Items(areas[1].Points).Should().Equal((1, 3, 4), (2, 4, 6), (3, 5, 8));
		Items(areas[2].Points).Should().Equal((0, 2, 2), (1, 4, 5), (2, 6, 8), (3, 8, 11), (4, 6, 10));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ReverseRibbonAndRidgeLineRetainFirstDuplicateBounds(bool ridge)
	{
		// Arrange

		Row[] source = [new(3, 1), new(2, 1), new(2, 100), new(1, 1)];
		var context = PlotContext.Build(source, row => row.X, _ => 5.0);
		if (ridge)
		{
			context.Geom_RidgeLine(height: row => row.Y);
		}
		else
		{
			context.Geom_Ribbon(ymin: _ => 5.0, ymax: row => 5 + row.Y);
		}
		context.Init(grid: false);

		// Act

		context.Render();
		var points = Items(Shapes<Shapes.Area>(context).Single().Points);

		// Assert

		points.Should().Equal((1, 5, 6), (2, 5, 6), (3, 5, 6));
	}

	[Fact]
	public void ReverseViolinTraversesBothSidesAndRetainsTheFirstDuplicateWidth()
	{
		// Arrange

		Row[] source = [new(3, 3), new(2, 2), new(100, 2), new(1, 1)];
		var context = PlotContext.Build(source, _ => 10.0, row => row.Y)
			.Geom_Violin(width: row => row.X);
		context.Init(grid: false);

		// Act

		context.Render();
		var polygon = Shapes<Shapes.Polygon>(context).Single();

		// Assert

		using var _ = new AssertionScope();
		polygon.Path.Latitude.Should().Equal(1, 2, 3, 3, 2, 1);
		polygon.Path.Longitude.Should().Equal(10 - 0.45, 10 - 0.9, 10 - 1.35, 10 + 1.35, 10 + 0.9, 10 + 0.45);
	}

	[Fact]
	public void BarAndErrorBarKeepSortedGroupsAndWithinGroupArrivalOrder()
	{
		// Arrange

		Row[] source = [new(3, 30), new(1, 10), new(3, 31), new(2, 20), new(1, 11)];
		var bars = PlotContext.Build(source, row => row.X, row => row.Y).Geom_Bar();
		var errors = PlotContext.Build(source, row => row.X, row => row.Y)
			.Geom_ErrorBar(ymin: row => row.Y - 1, ymax: row => row.Y + 1);
		bars.Init(grid: false);
		errors.Init(grid: false);

		// Act

		bars.Render();
		errors.Render();
		var rectangles = Shapes<Shapes.Rectangle>(bars);
		var circles = Shapes<Shapes.Circle>(errors);

		// Assert

		using var _ = new AssertionScope();
		rectangles.Select(rectangle => rectangle.X).Should().Equal(0.55, 0.55, 1.55, 2.55, 2.55);
		rectangles.Select(rectangle => (rectangle.Y, rectangle.Height)).Should().Equal((0, 11), (11, 10), (0, 20), (0, 31), (31, 30));
		circles.Select(circle => (circle.X, circle.Y)).Should().Equal((1, 10), (1, 11), (2, 20), (3, 30), (3, 31));
	}

	[Fact]
	public void BoxplotSortsGroupsAndRetainsReverseDuplicateSamples()
	{
		// Arrange

		Row[] source = [new(10, 3), new(4, 1), new(1, 3), new(2, 1), new(1, 3), new(1, 3)];
		var context = PlotContext.Build(source, row => row.X, row => row.Y).Geom_Boxplot();
		context.Init(grid: false);

		// Act

		context.Render();
		var rectangles = Shapes<Shapes.Rectangle>(context);

		// Assert

		using var _ = new AssertionScope();
		rectangles.Select(rectangle => rectangle.Y).Should().Equal(1 - 0.8, 3 - 0.8);
		rectangles.Select(rectangle => (rectangle.X, rectangle.Width)).Should().Equal((2.5, 1), (1, 2.25));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void GroupingPreservesTheNaNEqualityAndComparerMismatch(bool nan)
	{
		// Arrange

		var key = nan ? double.NaN : 1.0;
		Row[] source = [new(key, 4), new(key, 8)];
		// Disable axis contribution to observe grouping independently of invalid-range rejection.
		var bars = PlotContext.Build(source, row => row.X, row => row.Y).Geom_Bar(scale: (false, false));
		var errors = PlotContext.Build(source, row => row.X, row => row.Y).Geom_ErrorBar(scale: (false, false));
		var boxes = PlotContext.Build(source, row => row.Y, row => row.X).Geom_Boxplot(scale: (false, false));
		bars.Init(grid: false);
		errors.Init(grid: false);
		boxes.Init(grid: false);

		// Act

		bars.Render();
		errors.Render();
		boxes.Render();

		// Assert

		using var _ = new AssertionScope();
		Shapes<Shapes.Rectangle>(bars).Select(rectangle => rectangle.Height).Should().Equal(nan ? [4] : [8, 4]);
		Shapes<Shapes.Circle>(errors).Select(circle => circle.Y).Should().Equal(nan ? [4] : [4, 8]);
		var box = Shapes<Shapes.Rectangle>(boxes).Single();
		box.X.Should().Be(nan ? 4 : 5);
		box.Width.Should().Be(nan ? 0 : 2);
	}

	[Theory]
	[InlineData(0, 7, 7, 7)]
	[InlineData(1, 1, 1, 3.25)]
	[InlineData(2, 1.75, 2.5, 3.25)]
	[InlineData(3, double.NaN, 1.5, 2.25)]
	public void SamplePercentilesPreserveConstantReverseDuplicateAndNaNValues(int scenario, double lower, double median, double upper)
	{
		// Arrange

		double[] values = scenario switch
		{
			0 => [7, 7, 7, 7],
			1 => [10, 1, 1, 1],
			2 => [4, 3, 2, 1],
			_ => [3, double.NaN, 2, 1]
		};
		var samples = new SampleBuffer<double>();
		foreach (var value in values)
		{
			samples.Add(value);
		}
		double[] probabilities = [0.25, 0.5, 0.75];

		// Act

		var actual = probabilities.Select(p => Boxplot<double, double, double>.Percentile(samples, p));

		// Assert

		actual.Should().Equal(lower, median, upper);
	}

	[Fact]
	public void DiscreteRanksAreReadyBeforeCommitAndClearRemovesOldKeys()
	{
		// Arrange

		var scale = new DiscretePosition<int>(limits: (20, 30), expand: (0, 0, 0, 0));
		int[] originalKeys = [10, 20, 30, 40];
		int[] replacementKeys = [7, 9, 20];
		foreach (var value in new[] { 40, 30, 20, 10, 20 })
		{
			scale.Train(value);
		}

		// Act

		var before = originalKeys.Select(scale.Map).ToArray();
		var unmap = scale.Unmap(2);
		scale.Commit(grid: true);
		var range = scale.Range;
		var labels = scale.Labels.ToArray();
		scale.Clear();
		scale.Train(9);
		scale.Train(7);
		var after = replacementKeys.Select(scale.Map).ToArray();

		// Assert

		using var _ = new AssertionScope();
		before.Should().Equal(0, 1, 2, 3);
		unmap.Should().Be(30);
		range.Should().Be((1, 2));
		labels.Should().Equal((0, "10"), (1, "20"), (2, "30"), (3, "40"));
		after.Should().Equal(0, 1, double.NaN);
		scale.Unmap(0).Should().Be(7);
		scale.Unmap(2).Should().BeNull();
	}

	[Fact]
	public void DateTimeSamplesMissingMinutesAcrossDaysBeforeCommitAndResetsAfterClear()
	{
		// Arrange

		var first = new LocalDateTime(2026, 10, 20, 9, 0);
		var second = first.PlusDays(1);
		var scale = new DateTimePosition(limits: (first.PlusMinutes(1), second.PlusMinutes(1)), expand: (0, 0, 0, 0));
		foreach (var value in new[] { second.PlusMinutes(2), first.PlusMinutes(2), second, first })
		{
			scale.Train(value);
		}

		// Act

		var before = new[] { first, first.PlusMinutes(1), first.PlusMinutes(2), second, second.PlusMinutes(1), second.PlusMinutes(2) }.Select(scale.Map).ToArray();
		var unmap = scale.Unmap(4);
		scale.Commit(grid: true);
		var range = scale.Range;
		scale.Clear();
		var next = second.PlusDays(1);
		scale.Train(next.PlusMinutes(3));
		scale.Train(next);
		var after = new[] { next, next.PlusMinutes(1), next.PlusMinutes(3), first }.Select(scale.Map).ToArray();

		// Assert

		using var _ = new AssertionScope();
		before.Should().Equal(0, 1, 2, 3, 4, 5);
		unmap.Should().Be(second.PlusMinutes(1));
		range.Should().Be((1, 4));
		after.Should().Equal(0, 1, 3, double.NaN);
		scale.Unmap(2).Should().Be(next.PlusMinutes(2));
		scale.Unmap(4).Should().BeNull();
	}

	private static T[] Items<T>(SortedBuffer<T> buffer) => [.. Enumerable.Range(0, buffer.Count).Select(index => buffer[index])];

	private static T[] Shapes<T>(PlotContext<Row, double, double> context) => [.. context.Panels[0].Geoms[0].Layer.Select(shape => shape.Value).OfType<T>()];

	private sealed record Row(double X, double Y, string Group = "A");
}

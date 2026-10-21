using System.Collections;

namespace GGNet.Headless.Tests;

public class StatRefreshTests
{
	[Fact]
	public async Task ChainedSourcesRefreshUpstreamBeforeEveryExport()
	{
		// Arrange

		var values = new List<double> { 1, 2 };
		var upstream = Stat.Bin(values, value => value, bins: 2);
		var downstream = Stat.Summary(upstream, _ => 0, bin => bin.Count);
		var plot = PlotContext.Build(downstream, item => item.X, item => item.Center).Geom_Point().Style();
		await plot.AsStringAsync();

		// Act

		values.Add(2);
		await plot.AsStringAsync();
		var afterInsert = downstream[0].Center;
		values.RemoveAt(0);
		await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		afterInsert.Should().Be(1.5);
		upstream.Sum(bin => bin.Count).Should().Be(2);
		downstream[0].Center.Should().Be(1);
	}

	[Fact]
	public async Task FirstExportRefreshesAnExplicitLineSource()
	{
		// Arrange

		var values = new List<double> { 1, 2 };
		var summary = Stat.Summary(values, _ => 0, value => value);
		var context = PlotContext.Build(new double[] { 0 }, value => value, value => value)
			.Geom_Line(summary, item => item.X, item => item.Center);
		values.Add(9);

		// Act

		await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		summary[0].Center.Should().Be(4);
		context.Panels.Single().Geoms.Single().Layer.Should().NotBeEmpty();
	}

	[Fact]
	public void ConstructionIsEagerAndDirectRecomputeIsShallow()
	{
		// Arrange

		var values = new List<double> { 1, 2 };
		var upstream = new CountingSource(values);
		var summary = Stat.Summary(upstream, _ => 0, value => value);
		var initial = summary[0].Center;
		values.Add(9);

		// Act

		summary.Recompute();

		// Assert

		using var _ = new AssertionScope();
		initial.Should().Be(1.5);
		summary[0].Center.Should().Be(1.5);
		upstream.Calls.Should().Be(0);
	}

	[Fact]
	public void DirectRecomputeDoesNotRefreshBuiltInDependencies()
	{
		// Arrange

		var values = new List<double> { 1, 2 };
		var bins = Stat.Bin(values, value => value, bins: 2);
		var summary = Stat.Summary(bins, _ => 0, bin => bin.Count);
		values.Add(2);

		// Act

		summary.Recompute();

		// Assert

		using var _ = new AssertionScope();
		bins.Sum(bin => bin.Count).Should().Be(2);
		summary[0].Center.Should().Be(1);
	}

	[Theory]
	[InlineData("Bin")]
	[InlineData("GroupedBin")]
	[InlineData("Density")]
	[InlineData("GroupedDensity")]
	[InlineData("Count")]
	[InlineData("Summary")]
	[InlineData("GroupedSummary")]
	public async Task EveryFactoryRefreshesItsCustomUpstreamFirst(string factory)
	{
		// Arrange

		var values = new List<double> { 1, 2 };
		var upstream = new CountingSource(values);
		var selected = new List<double>();
		double Select(double value)
		{
			selected.Add(value);
			return value;
		}
		var plot = FactoryPlot(factory, upstream, Select);
		selected.Clear();
		values.Add(4);

		// Act

		await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		upstream.Calls.Should().Be(1);
		selected.Should().Contain(4);
	}

	[Fact]
	public async Task SharedRootDependenciesAndVisibleLayersRefreshOnlyOnce()
	{
		// Arrange

		var values = new List<double> { 1, 2 };
		var upstream = new CountingSource(values);
		var selected = 0;
		var bins = Stat.Bin(upstream, value => { selected++; return value; }, bins: 2);
		var summary = Stat.Summary(bins, _ => 0, bin => bin.Count);
		var other = Stat.Summary(bins, _ => 1, bin => bin.Count);
		var context = PlotContext.Build(summary, item => item.X, item => item.Center)
			.Geom_Point()
			.Geom_Line(summary, item => item.X, item => item.Center)
			.Geom_Line(other, item => item.X, item => item.Center)
			.Geom_Line(bins, bin => bin.Mid, bin => (double)bin.Count)
			.Geom_Line(upstream, value => value, value => value);
		selected = 0;
		values.Add(2);

		// Act

		await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		upstream.Calls.Should().Be(1);
		selected.Should().Be(values.Count);
		summary[0].Center.Should().Be(1.5);
		other[0].Center.Should().Be(1.5);
	}

	[Fact]
	public async Task ValueEqualDistinctCustomSourcesEachRefresh()
	{
		// Arrange

		var root = new CountingSource([1, 2]);
		var other = new CountingSource([3, 4]);
		var plot = PlotContext.Build(root, value => value, value => value)
			.Geom_Point()
			.Geom_Line(other, value => value, value => value)
			.Geom_Line(root, value => value, value => value).Style();

		// Act

		await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		root.Equals(other).Should().BeTrue();
		root.Calls.Should().Be(1);
		other.Calls.Should().Be(1);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task FacetsUseFreshRootDependenciesOnFirstExport(bool grid, bool free)
	{
		// Arrange

		var values = new List<double> { 1 };
		var upstream = new CountingSource(values);
		var summary = Stat.Summary(upstream, value => value, value => value);
		var context = PlotContext.Build(summary, item => item.X, item => item.Center)
			.Geom_ErrorBar(ymin: item => item.Lower, ymax: item => item.Upper);
		if (grid)
		{
			context.Facet_Grid(item => item.X, _ => 0, freeX: free, freeY: free);
		}
		else
		{
			context.Facet_Wrap(item => item.X, freeX: free, freeY: free);
		}
		values.Add(2);

		// Act

		await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		upstream.Calls.Should().Be(1);
		context.Panels.Should().HaveCount(2);
		context.Panels.Select(panel => panel.Geoms.Single().Layer.Count).Should().Equal(2, 2);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task EmptyFacetsRecoverWhenUpstreamBecomesPopulated(bool grid)
	{
		// Arrange

		var values = new List<double>();
		var upstream = new CountingSource(values);
		var summary = Stat.Summary(upstream, value => value, value => value);
		var context = PlotContext.Build(summary, item => item.X, item => item.Center).Geom_Point();
		if (grid)
		{
			context.Facet_Grid(item => item.X, _ => 0);
		}
		else
		{
			context.Facet_Wrap(item => item.X);
		}
		await context.Style().AsStringAsync();
		var emptyPanels = context.Panels.Count;

		// Act

		values.Add(2);
		await context.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		emptyPanels.Should().Be(0);
		upstream.Calls.Should().Be(2);
		context.Panels.Should().ContainSingle();
		context.Panels.Single().Geoms.Single().Layer.Should().ContainSingle();
	}

	[Theory]
	[InlineData("Default")]
	[InlineData("Explicit")]
	[InlineData("Wrap")]
	[InlineData("Grid")]
	public async Task OnlyActualPanelFactoriesRefreshTheirSources(string mode)
	{
		// Arrange

		var defaultSource = new CountingSource([1, 2]);
		var explicitSource = new CountingSource([3, 4]);
		var context = PlotContext.Build(new double[] { 0, 1 }, value => value, value => value)
			.Geom_Line(defaultSource, value => value, value => value);
		if (mode != "Default")
		{
			context.Panel(panel => panel.Geom_Line(explicitSource, value => value, value => value));
		}
		if (mode == "Wrap")
		{
			context.Facet_Wrap(value => value);
		}
		else if (mode == "Grid")
		{
			context.Facet_Grid(value => value, _ => 0);
		}
		defaultSource.Fail = mode == "Explicit";
		explicitSource.Fail = mode != "Explicit";

		// Act

		await context.Style().AsStringAsync();
		await context.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		defaultSource.Calls.Should().Be(mode == "Explicit" ? 0 : 2);
		explicitSource.Calls.Should().Be(mode == "Explicit" ? 2 : 0);
	}

	[Fact]
	public async Task EmptyFacetsDoNotRefreshInactiveLayerSources()
	{
		// Arrange

		var values = new List<double> { 1 };
		var layer = new CountingSource([2, 3]);
		var context = PlotContext.Build(values, value => value, value => value)
			.Geom_Line(layer, value => value, value => value).Facet_Wrap(value => value);
		await context.Style().AsStringAsync();
		values.Clear();
		layer.Fail = true;

		// Act

		await context.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		context.Panels.Should().BeEmpty();
		layer.Calls.Should().Be(1);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ExistingLayerRefreshesAfterRootFacetsDisappearAndReturn(bool grid)
	{
		// Arrange

		var rootValues = new List<double> { 1 };
		var layerValues = new List<double> { 2, 3 };
		var layer = new CountingSource(layerValues);
		var context = PlotContext.Build(rootValues, value => value, value => value)
			.Geom_Point(layer, value => value, value => value);
		if (grid)
		{
			context.Facet_Grid(value => value, _ => 0);
		}
		else
		{
			context.Facet_Wrap(value => value);
		}
		await context.Style().AsStringAsync();
		var initialCalls = layer.Calls;

		// Act

		rootValues.Clear();
		layerValues.Clear();
		layerValues.AddRange([10, 20]);
		layer.Fail = true;
		await context.AsStringAsync();
		var emptyPanels = context.Panels.Count;
		var inactiveCalls = layer.Calls;
		var inactiveItems = layer.ToArray();
		rootValues.AddRange([4, 5]);
		layerValues.Add(30);
		layer.Fail = false;
		await context.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		initialCalls.Should().Be(1);
		emptyPanels.Should().Be(0);
		inactiveCalls.Should().Be(1);
		inactiveItems.Should().Equal(2, 3);
		layer.Calls.Should().Be(2);
		layer.Should().Equal(10, 20, 30);
		context.Panels.Should().HaveCount(2);
		foreach (var panel in context.Panels)
		{
			panel.Geoms.Single().StatSource.Should().BeSameAs(layer);
			panel.Geoms.Single().Layer.Select(shape => shape.Value).OfType<Shapes.Circle>()
				.Select(circle => (circle.X, circle.Y)).Should().Equal((10, 10), (20, 20), (30, 30));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SelectorFailurePreservesCachedItemsAndNextExportRetries(bool root)
	{
		// Arrange

		var values = new List<double> { 1, 2 };
		var upstream = new CountingSource(values);
		var failure = new InvalidOperationException("selector failed");
		var fail = false;
		var summary = Stat.Summary(upstream, _ => 0, value => fail ? throw failure : value);
		IPlotContext plot = root
			? PlotContext.Build(summary, item => item.X, item => item.Center).Geom_Point().Style()
			: PlotContext.Build(new double[] { 0 }, value => value, value => value)
				.Geom_Line(summary, item => item.X, item => item.Center).Style();
		await plot.AsStringAsync();
		values.Add(9);
		fail = true;

		// Act

		var export = async () => await plot.AsStringAsync();
		var thrown = await export.Should().ThrowAsync<InvalidOperationException>();
		var cached = summary[0].Center;
		fail = false;
		await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		thrown.Which.Should().BeSameAs(failure);
		cached.Should().Be(1.5);
		summary[0].Center.Should().Be(4);
		upstream.Calls.Should().Be(3);
	}

	[Theory]
	[InlineData(false, false, false)]
	[InlineData(false, false, true)]
	[InlineData(false, true, false)]
	[InlineData(false, true, true)]
	[InlineData(true, false, false)]
	[InlineData(true, false, true)]
	[InlineData(true, true, false)]
	[InlineData(true, true, true)]
	public async Task RetainedAdaptersRefreshBeforeFirstExport(bool errorBar, bool panel, bool enumerable)
	{
		// Arrange

		var values = new List<double> { 1, 2 };
		var summary = Stat.Summary(values, _ => 0, value => value);
		var context = PlotContext.Build(new double[] { 0 }, value => value, value => value);
		if (panel)
		{
			context.Panel(factory => errorBar
				? enumerable
					? factory.Geom_ErrorBar((IEnumerable<Summary>)summary, item => item.X, item => item.Center, ymin: item => item.Lower, ymax: item => item.Upper)
					: factory.Geom_ErrorBar(summary, item => item.X, item => item.Center, ymin: item => item.Lower, ymax: item => item.Upper)
				: enumerable
					? factory.Geom_Point((IEnumerable<Summary>)summary, item => item.X, item => item.Center)
					: factory.Geom_Point(summary, item => item.X, item => item.Center));
		}
		else if (errorBar)
		{
			if (enumerable)
			{
				context.Geom_ErrorBar((IEnumerable<Summary>)summary, item => item.X, item => item.Center, ymin: item => item.Lower, ymax: item => item.Upper);
			}
			else
			{
				context.Geom_ErrorBar(summary, item => item.X, item => item.Center, ymin: item => item.Lower, ymax: item => item.Upper);
			}
		}
		else if (enumerable)
		{
			context.Geom_Point((IEnumerable<Summary>)summary, item => item.X, item => item.Center);
		}
		else
		{
			context.Geom_Point(summary, item => item.X, item => item.Center);
		}
		values.Add(9);

		// Act

		await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		summary[0].Center.Should().Be(4);
		context.Panels.Single().Geoms.Single().StatSource.Should().BeSameAs(summary);
	}

	[Fact]
	public async Task LongBuiltInChainsRefreshWithoutRecursiveTraversal()
	{
		// Arrange

		var values = new List<double> { 1 };
		var upstream = new CountingSource(values);
		var summary = Stat.Summary(upstream, _ => 0, value => value);
		for (var i = 0; i < 10000; i++)
		{
			summary = Stat.Summary(summary, item => item.X, item => item.Center);
		}
		var plot = PlotContext.Build(summary, item => item.X, item => item.Center).Geom_Point().Style();
		values.Add(3);

		// Act

		await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		upstream.Calls.Should().Be(1);
		summary[0].Center.Should().Be(2);
	}

	[Theory]
	[InlineData(false, false, false)]
	[InlineData(false, false, true)]
	[InlineData(false, true, false)]
	[InlineData(false, true, true)]
	[InlineData(true, false, false)]
	[InlineData(true, false, true)]
	[InlineData(true, true, false)]
	[InlineData(true, true, true)]
	public async Task FacetMatchingUsesRefreshedCustomSourceEquality(bool grid, bool laterExport, bool matchAfterRefresh)
	{
		// Arrange

		var rootValues = new List<double> { 1, 2 };
		var layerValues = new List<double> { 1, 2 };
		var root = new CountingSource(rootValues, compareRows: true);
		var layer = new CountingSource(layerValues, compareRows: true);
		var context = PlotContext.Build(root, value => value, value => value)
			.Geom_Point(layer, value => value, value => value);
		if (grid)
		{
			context.Facet_Grid(value => value, _ => 0);
		}
		else
		{
			context.Facet_Wrap(value => value);
		}
		if (laterExport)
		{
			await context.Style().AsStringAsync();
		}
		layerValues.Add(3);
		if (matchAfterRefresh)
		{
			rootValues.Add(3);
		}
		var comparisons = root.EqualityCalls;

		// Act

		await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		context.Panels.Should().HaveCount(matchAfterRefresh ? 3 : 2);
		context.Panels.Select(panel => panel.Geoms.Single().Layer.Count)
			.Should().OnlyContain(count => count == (matchAfterRefresh ? 1 : 3));
		root.Calls.Should().Be(laterExport ? 2 : 1);
		layer.Calls.Should().Be(laterExport ? 2 : 1);
		root.EqualityCalls.Should().Be(comparisons + context.Panels.Count);
	}

	[Fact]
	public async Task EmptyActiveLayerPopulatesOnItsFirstExport()
	{
		// Arrange

		var values = new List<double>();
		var summary = Stat.Summary(values, _ => 0, value => value);
		var context = PlotContext.Build(new double[] { 1, 2 }, value => value, value => value)
			.Geom_Point(summary, item => item.X, item => item.Center).Facet_Wrap(value => value);
		values.Add(3);

		// Act

		await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		summary.Should().ContainSingle();
		summary[0].Center.Should().Be(3);
		context.Panels.Select(panel => panel.Geoms.Single().Layer.Count).Should().Equal(1, 1);
	}

	private static IPlotContext FactoryPlot(string factory, CountingSource source, Func<double, double> selector)
		=> factory switch
		{
			"Bin" => PlotContext.Build(Stat.Bin(source, selector, bins: 2), item => item.Mid, item => (double)item.Count).Geom_Point().Style(),
			"GroupedBin" => PlotContext.Build(Stat.Bin(source, selector, _ => 0, bins: 2), item => item.Mid, item => (double)item.Count).Geom_Point().Style(),
			"Density" => PlotContext.Build(Stat.Density(source, selector, bandwidth: 1, n: 3), item => item.At, item => item.Density).Geom_Point().Style(),
			"GroupedDensity" => PlotContext.Build(Stat.Density(source, selector, _ => 0, bandwidth: 1, n: 3), item => item.At, item => item.Density).Geom_Point().Style(),
			"Count" => PlotContext.Build(Stat.Count(source, selector), item => item.Key, item => (double)item.N).Geom_Point().Style(),
			"Summary" => PlotContext.Build(Stat.Summary(source, _ => 0, selector), item => item.X, item => item.Center).Geom_Point().Style(),
			"GroupedSummary" => PlotContext.Build(Stat.Summary(source, _ => 0, selector, _ => 0), item => item.X, item => item.Center).Geom_Point().Style(),
			_ => throw new ArgumentOutOfRangeException(nameof(factory))
		};

	private sealed class CountingSource(IReadOnlyList<double> values, bool compareRows = false) : IReadOnlyList<double>, IStatSource
	{
		private double[] items = [.. values];

		public int Calls { get; private set; }

		public bool Fail { get; set; }

		public int EqualityCalls { get; private set; }

		public int Count => items.Length;

		public double this[int index] => items[index];

		public void Recompute()
		{
			Calls++;
			if (Fail)
			{
				throw new InvalidOperationException("Inactive source was refreshed");
			}
			items = [.. values];
		}

		public override bool Equals(object? obj)
		{
			EqualityCalls++;
			return obj is CountingSource other && (!compareRows || items.SequenceEqual(other.items));
		}

		public override int GetHashCode() => 0;

		public IEnumerator<double> GetEnumerator() => ((IEnumerable<double>)items).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}

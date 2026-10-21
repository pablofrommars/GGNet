using GGNet.Rendering;
using Microsoft.Extensions.Logging;

namespace GGNet.Headless.Tests;

public class FacetCapacityTests
{
	[Theory]
	[InlineData(null, null)]
	[InlineData(1, null)]
	[InlineData(null, 1)]
	[InlineData(2, 2)]
	[InlineData(3, 3)]
	public async Task SufficientAndDerivedDimensionsKeepEveryMark(int? rows, int? columns)
	{
		// Arrange

		var source = Enumerable.Range(0, 4).Select(i => new Row(i, i)).ToList();
		var context = PlotContext.Build(source, row => row.X, row => row.X).Geom_Point()
			.Facet_Wrap(row => row.Group, nrows: rows, ncolumns: columns);

		// Act

		var svg = await context.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		context.Panels.Should().HaveCount(4);
		XDocument.Parse(svg).Descendants().Count(element => element.Name.LocalName == "circle").Should().Be(4);
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData(1, null)]
	[InlineData(null, 1)]
	[InlineData(1, 1)]
	public async Task EmptySourcesRemainValid(int? rows, int? columns)
	{
		// Arrange

		var context = PlotContext.Build(Array.Empty<Row>(), row => row.X, row => row.X).Geom_Point()
			.Facet_Wrap(row => row.Group, nrows: rows, ncolumns: columns);

		// Act

		var svg = await context.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		context.Panels.Should().BeEmpty();
		XDocument.Parse(svg).Descendants().Count(element => element.Name.LocalName == "circle").Should().Be(0);
		svg.Should().NotContain("NaN").And.NotContain("Infinity");
	}

	[Fact]
	public void CapacityCountsDistinctKeysRatherThanRows()
	{
		// Arrange

		var source = Enumerable.Range(0, 8).Select(i => new Row(i, i % 2)).ToList();
		var context = PlotContext.Build(source, row => row.X, row => row.X).Geom_Point()
			.Facet_Wrap(row => row.Group, nrows: 1, ncolumns: 2);

		// Act

		context.Render();

		// Assert

		context.Panels.Select(panel => panel.Geoms[0].Layer.Count).Should().Equal(4, 4);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void GrowthBeyondCapacityRejectsAndLaterValidDataRecovers(bool statistical)
	{
		// Arrange

		List<Row> rows = [new(1, 0), new(2, 1)];
		var recomputations = 0;
		IReadOnlyList<Row> source = statistical
			? new StatSource<Row>(() => { recomputations++; return rows.ToList(); })
			: rows;
		var context = PlotContext.Build(source, row => row.X, row => row.X).Geom_Point()
			.Facet_Wrap(row => row.Group, nrows: 1, ncolumns: 2);
		context.Render();
		var initialMarks = context.Panels.Sum(panel => panel.Geoms[0].Layer.Count);

		// Act

		rows.AddRange([new(3, 2), new(4, 3)]);
		var failure = Record.Exception(context.Render);
		var failedPanels = context.Panels.Count;
		rows.Clear();
		rows.Add(new(5, 4));
		context.Render();

		// Assert

		using var _ = new AssertionScope();
		initialMarks.Should().Be(2);
		failure.Should().BeOfType<GGNetUserException>().Which.Message.Should().Contain("needs 4 panels").And.Contain("only 2 slots");
		failedPanels.Should().Be(0);
		context.Panels.Should().ContainSingle();
		context.Panels[0].Strip.x.Should().Be("4");
		context.Panels[0].Geoms[0].Layer.Should().ContainSingle();
		recomputations.Should().Be(statistical ? 4 : 0);
	}

	[Fact]
	public async Task HeadlessExportSurfacesTheActionableCapacityError()
	{
		// Arrange

		Row[] source = [new(1, 0), new(2, 1)];
		var context = PlotContext.Build(source, row => row.X, row => row.X).Geom_Point()
			.Facet_Wrap(row => row.Group, nrows: 1, ncolumns: 1);

		// Act

		var export = () => context.AsStringAsync();

		// Assert

		await export.Should().ThrowAsync<GGNetUserException>().WithMessage("Facet_Wrap needs 2 panels, but nrows=1 and ncolumns=1 provide only 1 slots. Increase either dimension or omit one to derive it automatically.");
	}

	[Fact]
	public void LargeDimensionProductsDoNotOverflowCapacityChecks()
	{
		// Arrange

		Row[] source = [new(1, 0), new(2, 1)];
		var context = PlotContext.Build(source, row => row.X, row => row.X).Geom_Point()
			.Facet_Wrap(row => row.Group, nrows: 50_000, ncolumns: 50_000);

		// Act

		context.Render();

		// Assert

		using var _ = new AssertionScope();
		context.N.Should().Be((50_000, 50_000));
		context.Panels.Select(panel => panel.Geoms[0].Layer.Count).Should().Equal(1, 1);
	}

	[Fact]
	public async Task InteractiveCapacityFailureUsesExistingLoggingAndCanRecover()
	{
		// Arrange

		List<Row> source = [new(1, 0), new(2, 1)];
		var context = PlotContext.Build(source, row => row.X, row => row.X).Geom_Point()
			.Facet_Wrap(row => row.Group, nrows: 1, ncolumns: 1);
		var logged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var rendered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var plot = new Mock<IPlotRendering>();
		plot.Setup(value => value.Render(RenderTarget.Render)).Callback(context.Render);
		plot.Setup(value => value.StateHasChangedAsync()).Returns(() => { rendered.SetResult(); return Task.CompletedTask; });
		var logger = new Mock<ILogger>();
		logger.Setup(value => value.IsEnabled(LogLevel.Error)).Returns(true);
		logger.Setup(value => value.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
			It.Is<Exception>(exception => exception is GGNetUserException), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
			.Callback(() => logged.SetResult());
		await using var handler = RenderModeHandler.Factory(RenderMode.Interactive, plot.Object, logger.Object);

		// Act

		await handler.RefreshAsync(RenderTarget.Render, CancellationToken.None);
		await logged.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		var rejectedFrame = handler.ShouldRender();
		var failedPanels = context.Panels.Count;
		source.RemoveAt(1);
		await handler.RefreshAsync(RenderTarget.Render, CancellationToken.None);
		await rendered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		// Assert

		using var _ = new AssertionScope();
		rejectedFrame.Should().BeFalse();
		failedPanels.Should().Be(0);
		handler.ShouldRender().Should().BeTrue();
		context.Panels.Should().ContainSingle();
		context.Panels[0].Geoms[0].Layer.Should().ContainSingle();
		plot.Verify(value => value.StateHasChangedAsync(), Times.Once);
	}

	private sealed record Row(double X, int Group);
}

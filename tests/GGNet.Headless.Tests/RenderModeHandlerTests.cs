// Per-file usings (not global): Moq.Match collides with the globally-used
// System.Text.RegularExpressions.Match, and only this file needs either.
using GGNet.Rendering;

using Moq;

namespace GGNet.Headless.Tests;

// Phase 1: the render-mode handlers in isolation. The Interactive handler runs
// a background channel loop with semaphore backpressure; tests drive it
// deterministically by using that same backpressure as the synchronization
// signal (StateHasChangedAsync releases a test semaphore) — never Task.Delay.
// IPlotRendering is doubled with Moq; the SingleReader loop invokes it on one
// thread only, so recording is race-free.
public class RenderModeHandlerTests
{
	[Fact]
	public async Task FactoryMapsModes()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();

		// Act

		await using var interactive = RenderModeHandler.Factory(RenderMode.Interactive, plot.Object);
		await using var interactiveAuto = RenderModeHandler.Factory(RenderMode.InteractiveAuto, plot.Object);
		await using var staticHandler = RenderModeHandler.Factory(RenderMode.Static, plot.Object);

		// Assert

		using var _ = new AssertionScope();

		interactive.Should().BeOfType<InteractiveRenderModeHandler>();
		interactiveAuto.Should().BeOfType<InteractiveAutoRenderModeHandler>();
		staticHandler.Should().BeOfType<StaticRenderModeHandler>();
	}

	[Fact]
	public async Task RenderFailureIsLoggedAndTheLoopSurvives()
	{
		// Arrange

		var gate = new SemaphoreSlim(0);

		var plot = new Mock<IPlotRendering>();
		plot.Setup(p => p.Render(It.IsAny<RenderTarget>())).Throws(new InvalidOperationException("boom"));

		var logger = new Mock<Microsoft.Extensions.Logging.ILogger>();
		logger.Setup(l => l.IsEnabled(It.IsAny<Microsoft.Extensions.Logging.LogLevel>())).Returns(true);
		logger.Setup(l => l.Log(
				Microsoft.Extensions.Logging.LogLevel.Error,
				It.IsAny<Microsoft.Extensions.Logging.EventId>(),
				It.IsAny<It.IsAnyType>(),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
			.Callback(() => gate.Release());

		await using var sut = RenderModeHandler.Factory(RenderMode.Interactive, plot.Object, logger.Object);

		// Act

		await sut.RefreshAsync(RenderTarget.Render, CancellationToken.None);
		var logged = await gate.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		// A second refresh proves the loop survived the failed frame.
		await sut.RefreshAsync(RenderTarget.Render, CancellationToken.None);
		var loggedAgain = await gate.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		// Assert

		using var _ = new AssertionScope();

		logged.Should().BeTrue();
		loggedAgain.Should().BeTrue();
	}

	[Fact]
	public void FactoryRejectsUnknownMode()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();

		// Act

		Action act = () => RenderModeHandler.Factory((RenderMode)999, plot.Object);

		// Assert

		act.Should().Throw<GGNetInternalException>();
	}

	[Fact]
	public async Task StaticDoesNotRender()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();

		await using var handler = new StaticRenderModeHandler(plot.Object);

		// Act / Assert

		using var _ = new AssertionScope();

		handler.ShouldRender().Should().BeFalse();
		handler.Child().ShouldRender().Should().BeFalse();
	}

	[Fact]
	public async Task InteractiveAutoRendersInline()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();

		await using var handler = new InteractiveAutoRenderModeHandler(plot.Object);

		// Act

		await handler.RefreshAsync(RenderTarget.Render, TestContext.Current.CancellationToken);

		// Assert

		using var _ = new AssertionScope();

		plot.Verify(p => p.Render(RenderTarget.Render), Times.Once);
		handler.ShouldRender().Should().BeTrue();
		handler.Child().ShouldRender().Should().BeTrue();
	}

	[Fact]
	public void InteractiveChildRefreshIsSingleBit()
	{
		// Arrange

		var child = new InteractiveRenderModeHandler.ChildRenderHandler();

		// Act / Assert

		using var _ = new AssertionScope();

		child.ShouldRender().Should().BeFalse();

		child.Refresh();
		child.ShouldRender().Should().BeTrue();
		child.ShouldRender().Should().BeFalse();

		child.Refresh();
		child.Refresh();
		child.ShouldRender().Should().BeTrue();
		child.ShouldRender().Should().BeFalse();
	}

	[Fact]
	public async Task ShouldRenderIsOneShotAfterRender()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();
		var rendered = new SemaphoreSlim(0);

		plot.Setup(p => p.StateHasChangedAsync())
			.Returns(() =>
			{
				rendered.Release();
				return Task.CompletedTask;
			});

		await using var handler = new InteractiveRenderModeHandler(plot.Object);

		// Act

		handler.ShouldRender().Should().BeFalse();

		await handler.RefreshAsync(RenderTarget.Render, TestContext.Current.CancellationToken);
		await rendered.WaitAsync(TestContext.Current.CancellationToken);

		// Assert

		using var _ = new AssertionScope();

		handler.ShouldRender().Should().BeTrue();
		handler.ShouldRender().Should().BeFalse();
	}

	[Fact]
	public async Task CoalescesQueuedRefreshesAndSubsumesLoading()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();
		var renders = new List<RenderTarget>();
		var rendered = new SemaphoreSlim(0);

		plot.Setup(p => p.Render(It.IsAny<RenderTarget>()))
			.Callback<RenderTarget>(renders.Add);
		plot.Setup(p => p.StateHasChangedAsync())
			.Returns(() =>
			{
				rendered.Release();
				return Task.CompletedTask;
			});

		await using var handler = new InteractiveRenderModeHandler(plot.Object);

		// Act

		// Batch 1: a single Loading. The loop renders it, then blocks on backpressure.
		await handler.RefreshAsync(RenderTarget.Loading, TestContext.Current.CancellationToken);
		await rendered.WaitAsync(TestContext.Current.CancellationToken);

		// While the loop is blocked, queue a burst: two Render and one Loading.
		await handler.RefreshAsync(RenderTarget.Render, TestContext.Current.CancellationToken);
		await handler.RefreshAsync(RenderTarget.Render, TestContext.Current.CancellationToken);
		await handler.RefreshAsync(RenderTarget.Loading, TestContext.Current.CancellationToken);

		// Release backpressure: the loop drains the whole burst in one pass.
		handler.OnAfterRender(firstRender: false);
		await rendered.WaitAsync(TestContext.Current.CancellationToken);

		// Assert

		using var _ = new AssertionScope();

		// Burst coalesced to a single render; Render subsumed the queued Loading.
		renders.Should().Equal(RenderTarget.Loading, RenderTarget.Render);
		plot.Verify(p => p.Render(It.IsAny<RenderTarget>()), Times.Exactly(2));
	}

	[Fact]
	public async Task DisposeIsIdempotent()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();
		var handler = new InteractiveRenderModeHandler(plot.Object);

		// Act

		Func<Task> act = async () =>
		{
			await handler.DisposeAsync();
			await handler.DisposeAsync();
		};

		// Assert

		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task SerializesRendersUnderRepeatedRefresh()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();
		var rendered = new SemaphoreSlim(0);

		plot.Setup(p => p.StateHasChangedAsync())
			.Returns(() =>
			{
				rendered.Release();
				return Task.CompletedTask;
			});

		await using var handler = new InteractiveRenderModeHandler(plot.Object);

		const int n = 25;

		// Act

		// Each refresh is fully rendered (awaited) before the next is queued: the
		// single-writer + backpressure contract, exercised repeatedly.
		for (var i = 0; i < n; i++)
		{
			await handler.RefreshAsync(RenderTarget.Render, TestContext.Current.CancellationToken);
			await rendered.WaitAsync(TestContext.Current.CancellationToken);
			handler.OnAfterRender(firstRender: false);
		}

		// Assert

		plot.Verify(p => p.Render(RenderTarget.Render), Times.Exactly(n));
	}
}

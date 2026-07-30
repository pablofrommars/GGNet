// Per-file usings (not global): Moq.Match collides with the globally-used
// System.Text.RegularExpressions.Match.
using GGNet.Rendering;

using Moq;

namespace GGNet.Headless.Tests;

// The spec/realization split's phase-0 precondition (§8c #4): the one seam the
// single-threaded headless goldens cannot see. Interactive commits mutate
// view-window state from caller threads while the handler's background loop
// renders — these tests hammer exactly that and assert the system-level
// contract: no crash, the loop survives, and state converges to the last
// writer once the storm ends.
public class ConcurrencyStressTests
{
	[Fact]
	public async Task ConcurrentRefreshStormCoalescesAndTheLoopSurvives()
	{
		// Arrange

		var renders = 0;
		var renderGate = new SemaphoreSlim(0);

		var plot = new Mock<IPlotRendering>();

		await using var sut = (InteractiveRenderModeHandler)RenderModeHandler.Factory(RenderMode.Interactive, plot.Object);

		plot.Setup(p => p.Render(It.IsAny<RenderTarget>()))
			.Callback(() =>
			{
				Interlocked.Increment(ref renders);
				renderGate.Release();
			});

		// Complete the backpressure handshake so the loop keeps draining.
		plot.Setup(p => p.StateHasChangedAsync())
			.Returns(() =>
			{
				sut.OnAfterRender(firstRender: false);

				return Task.CompletedTask;
			});

		// Act

		// Eight writers × 200 refreshes each, mixing targets — far more than
		// production (the circuit serializes handlers), on purpose.
		var writers = Enumerable.Range(0, 8).Select(w => Task.Run(async () =>
		{
			for (var i = 0; i < 200; i++)
			{
				await sut.RefreshAsync(i % 3 == 0 ? RenderTarget.Loading : RenderTarget.Render, CancellationToken.None);
			}
		}));

		await Task.WhenAll(writers);

		// Drain: the loop is quiescent once no render arrives within a beat.
		while (await renderGate.WaitAsync(TimeSpan.FromMilliseconds(250)))
		{
		}

		var drained = Volatile.Read(ref renders);

		// One more refresh proves the loop survived the storm.
		await sut.RefreshAsync(RenderTarget.Render, CancellationToken.None);
		var alive = await renderGate.WaitAsync(TimeSpan.FromSeconds(5));

		// Assert

		using var _ = new AssertionScope();

		alive.Should().BeTrue();
		drained.Should().BeGreaterThan(0)
			.And.BeLessThan(1600, "coalescing must collapse queued refreshes, not render one frame per write");
	}

	private sealed record P(double X, double Y);

	private static readonly P[] data =
	[
		new(1.0, 2.0),
		new(2.0, 3.5),
		new(3.0, 2.8),
		new(4.0, 4.2)
	];

	[Fact]
	public async Task ViewWindowWritesRacingRendersConvergeToTheLastWriter()
	{
		// Arrange

		var context = PlotContext.Build(data, i => i.X, i => i.Y)
			.Geom_Point()
			.Style();

		context.Init();
		context.Render();

		// Act

		// Writers mutate the view window while a renderer runs full passes —
		// the production shape (gesture thread vs background loop), amplified.
		var writers = Enumerable.Range(0, 4).Select(w => Task.Run(() =>
		{
			for (var i = 0; i < 250; i++)
			{
				context.SetXView(i % 7, (i % 7) + 1.0 + w);
			}
		}));

		var renderer = Task.Run(() =>
		{
			for (var i = 0; i < 200; i++)
			{
				context.Render();
			}
		});

		var storm = async () => await Task.WhenAll([.. writers, renderer]);

		// Assert

		await storm.Should().NotThrowAsync();

		// Convergence: one authoritative write and pass after the storm wins,
		// exactly — torn frames during the race may render garbage transiently,
		// but the next frame must correct.
		context.SetXView(2.0, 3.0);
		context.Render();

		context.Positions.X.Scales[0].Range.Should().Be((2.0, 3.0));
	}

	[Fact]
	public async Task DisposeDuringARefreshStormCompletesCleanly()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();

		var sut = RenderModeHandler.Factory(RenderMode.Interactive, plot.Object);

		var storm = Task.Run(async () =>
		{
			for (var i = 0; i < 500; i++)
			{
				await sut.RefreshAsync(RenderTarget.Render, CancellationToken.None);
			}
		});

		// Act

		// Dispose races the writers: it must cancel the loop, await it, and
		// leave late writes harmless (they land in a channel nobody reads).
		var dispose = async () => await sut.DisposeAsync();

		// Assert

		using var _ = new AssertionScope();

		await dispose.Should().NotThrowAsync();
		await ((Func<Task>)(() => storm)).Should().NotThrowAsync();
	}
}

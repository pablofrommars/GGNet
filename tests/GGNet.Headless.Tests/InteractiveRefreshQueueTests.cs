using System.Threading.Channels;

using GGNet.Rendering;

namespace GGNet.Headless.Tests;

public class InteractiveRefreshQueueTests
{
	[Fact]
	public async Task StalledAcknowledgementRetainsOneWakeAndTwoTargetBits()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();
		var rendered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		plot.Setup(p => p.StateHasChangedAsync())
			.Returns(() =>
			{
				rendered.TrySetResult();
				return Task.CompletedTask;
			});
		await using var handler = new InteractiveRenderModeHandler(plot.Object);
		await handler.RefreshAsync(RenderTarget.Loading, CancellationToken.None);
		await rendered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
		var channel = WakeChannel(handler);

		// Act

		for (var i = 0; i < 50000; i++)
		{
			await handler.RefreshAsync(i % 2 == 0 ? RenderTarget.Render : RenderTarget.Loading, CancellationToken.None);
		}

		var retainedBeforeDisposal = channel.Reader.Count;
		var pendingBeforeDisposal = PendingTargets(handler);
		var acceptsSecondWake = channel.Writer.TryWrite(0);
		await handler.DisposeAsync();

		for (var i = 0; i < 50000; i++)
		{
			await handler.RefreshAsync(i % 2 == 0 ? RenderTarget.Render : RenderTarget.Loading, CancellationToken.None);
		}

		// Assert

		using var _ = new AssertionScope();

		retainedBeforeDisposal.Should().Be(1);
		pendingBeforeDisposal.Should().Be(3);
		acceptsSecondWake.Should().BeFalse("the wake channel itself has capacity one");
		channel.Reader.Count.Should().Be(1);
		PendingTargets(handler).Should().Be(3);
		channel.Reader.TryRead(out var wake).Should().BeTrue();
		wake.Should().Be(0);
		channel.Reader.TryRead(out wake).Should().BeFalse();
		await channel.Reader.Completion.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
		channel.Writer.TryWrite(0).Should().BeFalse("disposal completes the writer nonthrowingly");
		plot.Verify(p => p.Render(It.IsAny<RenderTarget>()), Times.Once);
	}

	[Theory]
	[InlineData(RenderTarget.Render, false)]
	[InlineData(RenderTarget.Loading, false)]
	[InlineData(RenderTarget.Render, true)]
	[InlineData(RenderTarget.Loading, true)]
	public async Task PreCanceledRefreshDoesNotPublishATargetOrWake(RenderTarget target, bool disposeFirst)
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();
		var rendered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		plot.Setup(p => p.StateHasChangedAsync())
			.Returns(() =>
			{
				rendered.TrySetResult();
				return Task.CompletedTask;
			});
		await using var handler = new InteractiveRenderModeHandler(plot.Object);
		await handler.RefreshAsync(RenderTarget.Loading, CancellationToken.None);
		await rendered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
		if (disposeFirst)
		{
			await handler.DisposeAsync();
		}

		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		// Act

		var refresh = handler.RefreshAsync(target, cancellation.Token);
		Func<Task> act = () => refresh;

		// Assert

		using var _ = new AssertionScope();

		refresh.IsCanceled.Should().BeTrue();
		(await act.Should().ThrowAsync<OperationCanceledException>())
			.Which.CancellationToken.Should().Be(cancellation.Token);
		PendingTargets(handler).Should().Be(0);
		WakeChannel(handler).Reader.Count.Should().Be(0);
		plot.Verify(p => p.Render(It.IsAny<RenderTarget>()), Times.Once);
	}

	[Fact]
	public async Task CanceledRenderDoesNotPromotePendingLoading()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();
		using var rendered = new SemaphoreSlim(0);
		plot.Setup(p => p.StateHasChangedAsync())
			.Returns(() =>
			{
				rendered.Release();
				return Task.CompletedTask;
			});
		await using var handler = new InteractiveRenderModeHandler(plot.Object);
		await handler.RefreshAsync(RenderTarget.Loading, CancellationToken.None);
		(await rendered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)).Should().BeTrue();
		await handler.RefreshAsync(RenderTarget.Loading, CancellationToken.None);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		// Act

		Func<Task> canceled = () => handler.RefreshAsync(RenderTarget.Render, cancellation.Token);
		await canceled.Should().ThrowAsync<OperationCanceledException>();
		handler.OnAfterRender(firstRender: false);
		var renderedAgain = await rendered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
		await handler.DisposeAsync();

		// Assert

		using var _ = new AssertionScope();

		renderedAgain.Should().BeTrue();
		plot.Verify(p => p.Render(RenderTarget.Loading), Times.Exactly(2));
		plot.Verify(p => p.Render(RenderTarget.Render), Times.Never);
	}

	[Fact]
	public async Task ProducerAfterPendingExchangeLeavesAWakeForTheNextFrame()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();
		using var rendered = new SemaphoreSlim(0);
		var targets = new List<RenderTarget>();
		await using var handler = new InteractiveRenderModeHandler(plot.Object);
		plot.Setup(p => p.Render(It.IsAny<RenderTarget>()))
			.Callback<RenderTarget>(target =>
			{
				targets.Add(target);
				if (targets.Count == 1)
				{
					// Render is entered only after the consumer has taken the pending bits.
					handler.RefreshAsync(RenderTarget.Render, CancellationToken.None)
						.IsCompletedSuccessfully.Should().BeTrue();
				}
			});
		plot.Setup(p => p.StateHasChangedAsync())
			.Returns(() =>
			{
				rendered.Release();
				return Task.CompletedTask;
			});

		// Act

		await handler.RefreshAsync(RenderTarget.Loading, CancellationToken.None);
		(await rendered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)).Should().BeTrue();
		var queuedWake = WakeChannel(handler).Reader.Count;
		var queuedTargets = PendingTargets(handler);
		handler.OnAfterRender(firstRender: false);
		var renderedAgain = await rendered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
		await handler.DisposeAsync();

		// Assert

		using var _ = new AssertionScope();

		queuedWake.Should().Be(1);
		queuedTargets.Should().Be(1);
		renderedAgain.Should().BeTrue();
		targets.Should().Equal(RenderTarget.Loading, RenderTarget.Render);
	}

	[Fact]
	public async Task ConcurrentProducersRetainBothTargetsAndOneWake()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();
		using var rendered = new SemaphoreSlim(0);
		plot.Setup(p => p.StateHasChangedAsync())
			.Returns(() =>
			{
				rendered.Release();
				return Task.CompletedTask;
			});
		await using var handler = new InteractiveRenderModeHandler(plot.Object);
		await handler.RefreshAsync(RenderTarget.Loading, CancellationToken.None);
		(await rendered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)).Should().BeTrue();
		var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var producers = Enumerable.Range(0, 8).Select(writer => Task.Run(async () =>
		{
			await start.Task;
			for (var i = 0; i < 2000; i++)
			{
				await handler.RefreshAsync(writer % 2 == 0 ? RenderTarget.Render : RenderTarget.Loading, CancellationToken.None);
			}
		}, TestContext.Current.CancellationToken)).ToArray();

		// Act

		start.SetResult();
		await Task.WhenAll(producers).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
		var retained = WakeChannel(handler).Reader.Count;
		var pending = PendingTargets(handler);
		handler.OnAfterRender(firstRender: false);
		var renderedAgain = await rendered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
		await handler.DisposeAsync();

		// Assert

		using var _ = new AssertionScope();

		retained.Should().Be(1);
		pending.Should().Be(3);
		renderedAgain.Should().BeTrue();
		plot.Verify(p => p.Render(RenderTarget.Loading), Times.Once);
		plot.Verify(p => p.Render(RenderTarget.Render), Times.Once);
	}

	[Fact]
	public async Task ProducersRacingRepeatedDisposalCompleteWithoutFurtherFrames()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();
		var rendered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		plot.Setup(p => p.StateHasChangedAsync())
			.Returns(() =>
			{
				rendered.TrySetResult();
				return Task.CompletedTask;
			});
		await using var handler = new InteractiveRenderModeHandler(plot.Object);
		await handler.RefreshAsync(RenderTarget.Loading, CancellationToken.None);
		await rendered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
		var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var producers = Enumerable.Range(0, 8).Select(writer => Task.Run(async () =>
		{
			await start.Task;
			for (var i = 0; i < 2000; i++)
			{
				await handler.RefreshAsync(i % 2 == 0 ? RenderTarget.Render : RenderTarget.Loading, CancellationToken.None);
			}
		}, TestContext.Current.CancellationToken)).ToArray();
		var disposals = Enumerable.Range(0, 2).Select(index => Task.Run(async () =>
		{
			await start.Task;
			await handler.DisposeAsync();
		}, TestContext.Current.CancellationToken)).ToArray();

		// Act

		start.SetResult();
		await Task.WhenAll([.. producers, .. disposals])
			.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
		for (var i = 0; i < 2000; i++)
		{
			await handler.RefreshAsync(RenderTarget.Render, CancellationToken.None);
			handler.OnAfterRender(firstRender: false);
		}

		await handler.DisposeAsync();

		// Assert

		using var _ = new AssertionScope();

		WakeChannel(handler).Reader.Count.Should().BeInRange(0, 1);
		PendingTargets(handler).Should().BeInRange(0, 3);
		plot.Verify(p => p.Render(It.IsAny<RenderTarget>()), Times.Once);
		plot.Verify(p => p.StateHasChangedAsync(), Times.Once);
	}

	[Fact]
	public async Task StateChangeFailurePreservesARefreshPublishedDuringTheFrame()
	{
		// Arrange

		var plot = new Mock<IPlotRendering>();
		var rendered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var targets = new List<RenderTarget>();
		await using var handler = new InteractiveRenderModeHandler(plot.Object);
		plot.Setup(p => p.Render(It.IsAny<RenderTarget>()))
			.Callback<RenderTarget>(target =>
			{
				targets.Add(target);
				if (targets.Count == 1)
				{
					handler.RefreshAsync(RenderTarget.Render, CancellationToken.None)
						.IsCompletedSuccessfully.Should().BeTrue();
				}
			});
		plot.SetupSequence(p => p.StateHasChangedAsync())
			.ThrowsAsync(new InvalidOperationException("Frame failed."))
			.Returns(() =>
			{
				rendered.TrySetResult();
				return Task.CompletedTask;
			});

		// Act

		await handler.RefreshAsync(RenderTarget.Loading, CancellationToken.None);
		await rendered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
		await handler.DisposeAsync();

		// Assert

		using var _ = new AssertionScope();

		targets.Should().Equal(RenderTarget.Loading, RenderTarget.Render);
		plot.Verify(p => p.StateHasChangedAsync(), Times.Exactly(2));
	}

	private static Channel<byte> WakeChannel(InteractiveRenderModeHandler handler)
		=> (Channel<byte>)typeof(InteractiveRenderModeHandler)
			.GetField("channel", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(handler)!;

	private static int PendingTargets(InteractiveRenderModeHandler handler)
		=> (int)typeof(InteractiveRenderModeHandler)
			.GetField("pending", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(handler)!;
}

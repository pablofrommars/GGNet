using Microsoft.Extensions.Logging;

namespace GGNet.Rendering;

internal sealed partial class InteractiveRenderModeHandler : RenderModeHandler
{
	private readonly CancellationTokenSource cancellationTokenSource = new();

	private readonly SemaphoreSlim semaphore = new(0, 1);

	private readonly ILogger? logger;

	private volatile int render;

	// SingleWriter is deliberately false: RefreshAsync is public surface —
	// gestures arrive on the circuit thread, host commands from any thread
	// (ConcurrencyStressTests pins the multi-writer contract).
	readonly Channel<RenderTarget> channel = Channel.CreateUnbounded<RenderTarget>(new()
	{
		SingleReader = true,
		SingleWriter = false
	});

	private readonly Task background;

	public InteractiveRenderModeHandler(IPlotRendering plot, ILogger? logger = null)
	  : base(plot)
	{
		this.logger = logger;

		background = RunBackground();
	}

	public override async Task RefreshAsync(RenderTarget target, CancellationToken token)
	{
		await channel.Writer.WriteAsync(target, token);
	}

	public override bool ShouldRender() => Interlocked.Exchange(ref render, 0) == 1;

	public override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			return;
		}

		semaphore.TryRelease();
	}

	public sealed class ChildRenderHandler : IChildRenderModeHandler
	{
		private volatile int render;

		// Single-bit pending: set-then-consume cannot lose an update.
		public void Refresh() => Interlocked.Exchange(ref render, 1);

		public bool ShouldRender() => Interlocked.Exchange(ref render, 0) == 1;
	}

	public override IChildRenderModeHandler Child() => new ChildRenderHandler();

	private async Task RunBackground()
	{
		try
		{
			while (await channel.Reader.WaitToReadAsync(cancellationTokenSource.Token).ConfigureAwait(false))
			{
				try
				{
					var pendingRender = false;
					var pendingLoading = false;

					while (channel.Reader.TryRead(out var target))
					{
						if (target == RenderTarget.Render)
						{
							pendingRender = true;
						}
						else
						{
							pendingLoading = true;
						}
					}

					if (!pendingRender && !pendingLoading)
					{
						continue;
					}

					// A queued render subsumes any queued loading state.
					plot.Render(pendingRender ? RenderTarget.Render : RenderTarget.Loading);

					Interlocked.Exchange(ref render, 1);

					await plot.StateHasChangedAsync();

					await semaphore.WaitAsync(cancellationTokenSource.Token);
				}
				catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
				{
				}
				catch (Exception exception)
				{
					// The loop must survive a failed frame, but never silently:
					// every interactive commit (wheel, pan, host commands)
					// renders through here.
					if (logger is not null)
					{
						Log.RenderFrameFailed(logger, exception);
					}
				}
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception)
		{
		}
	}

	private static partial class Log
	{
		[LoggerMessage(Level = LogLevel.Error, Message = "Render frame failed.")]
		public static partial void RenderFrameFailed(ILogger logger, Exception exception);
	}

	private volatile int disposing;

	public override async ValueTask DisposeAsync()
	{
		if (Interlocked.CompareExchange(ref disposing, 1, 0) == 0)
		{
			cancellationTokenSource.Cancel();

			if (background is not null)
			{
				await background.ConfigureAwait(false);
			}

			cancellationTokenSource.Dispose();

			semaphore.Dispose();
		}

		await base.DisposeAsync().ConfigureAwait(false);
	}
}

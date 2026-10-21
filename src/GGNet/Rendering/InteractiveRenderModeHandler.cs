using Microsoft.Extensions.Logging;

namespace GGNet.Rendering;

internal sealed partial class InteractiveRenderModeHandler : RenderModeHandler
{
	private readonly CancellationTokenSource cancellationTokenSource = new();

	private readonly SemaphoreSlim semaphore = new(0, 1);

	private readonly ILogger? frameLogger;

	private volatile int render;

	private const int pendingRender = 1;

	private const int pendingLoading = 2;

	private int pending;

	private readonly Channel<byte> channel = Channel.CreateBounded<byte>(new BoundedChannelOptions(1)
	{
		SingleReader = true,
		SingleWriter = false,
		FullMode = BoundedChannelFullMode.Wait
	});

	private readonly Task background;

	public InteractiveRenderModeHandler(IPlotRendering plot, ILogger? logger = null)
	  : base(plot)
	{
		frameLogger = logger;

		background = RunBackground();
	}

	public override Task RefreshAsync(RenderTarget target, CancellationToken token)
	{
		if (token.IsCancellationRequested)
		{
			return Task.FromCanceled(token);
		}

		if (disposing == 0)
		{
			// INVARIANT: Publishing after the wake can leave pending work with no signal.
			Interlocked.Or(ref pending, target == RenderTarget.Render ? pendingRender : pendingLoading);
			channel.Writer.TryWrite(0);
		}

		return Task.CompletedTask;
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
					// INVARIANT: Taking pending bits before the wake can consume a later producer's only signal.
					if (!channel.Reader.TryRead(out _))
					{
						continue;
					}

					var targets = Interlocked.Exchange(ref pending, 0);

					if (targets == 0)
					{
						continue;
					}

					plot.Render((targets & pendingRender) != 0 ? RenderTarget.Render : RenderTarget.Loading);

					Interlocked.Exchange(ref render, 1);

					await plot.StateHasChangedAsync();

					await semaphore.WaitAsync(cancellationTokenSource.Token);
				}
				catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
				{
				}
				catch (Exception exception)
				{
					if (frameLogger is not null)
					{
						Log.RenderFrameFailed(frameLogger, exception);
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
			channel.Writer.TryComplete();
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

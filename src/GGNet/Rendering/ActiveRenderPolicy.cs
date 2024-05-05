namespace GGNet.Rendering;

public sealed class ActiveRenderPolicy : RenderPolicyBase
{
	private readonly CancellationTokenSource cancellationTokenSource = new();

	private readonly SemaphoreSlim semaphore = new(0, 1);

	private readonly ChannelWriter<RenderTarget> writer;

	private volatile int render = 0;

	private readonly Task task;

	public ActiveRenderPolicy(IPlotRendering plot)
		: base(plot)
	{
		var channel = Channel.CreateUnbounded<RenderTarget>(new()
		{
			SingleReader = true,
			SingleWriter = true
		});

		writer = channel.Writer;

		task = Task.Factory.StartNew(async () =>
		{
			try
			{


				var reader = channel.Reader;

				var token = cancellationTokenSource.Token;

				while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
				{
					var mask = RenderTarget.None;

					while (reader.TryRead(out var target))
					{
						mask |= target;
					}

					if (mask == RenderTarget.None)
					{
						continue;
					}

					plot.Render(mask);

					Interlocked.Exchange(ref render, 1);

					await plot.StateHasChangedAsync().ConfigureAwait(false);

					try
					{
						await semaphore.WaitAsync(token).ConfigureAwait(false);
					}
					finally
					{
					}
				}
			}
			catch (Exception)
			{
			}
		}, TaskCreationOptions.LongRunning);
	}

	public override async Task RefreshAsync(RenderTarget target)
	{
		await writer.WriteAsync(target, cancellationTokenSource.Token);
	}

	public override bool ShouldRender() => Interlocked.Exchange(ref render, 0) == 1;

	public override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			return;
		}

		semaphore.Release();
	}

	public sealed class ChildRenderPolicy : IChildRenderPolicy
	{
		private volatile int render = 0;

		public void Refresh(RenderTarget target)
			=> Interlocked.Exchange(ref render, (int)target);

		public bool ShouldRender(RenderTarget target = RenderTarget.All)
			=> ((RenderTarget)Interlocked.Exchange(ref render, 0) & target) != RenderTarget.None;
	}

	public override IChildRenderPolicy Child() => new ChildRenderPolicy();

	private int disposing = 0;

	public override async ValueTask DisposeAsync()
	{
		if (Interlocked.CompareExchange(ref disposing, 1, 0) == 1)
		{
			return;
		}

		cancellationTokenSource?.Cancel();
		cancellationTokenSource?.Dispose();

		if (task is not null)
		{
			await task.ConfigureAwait(false);
		}
	}
}

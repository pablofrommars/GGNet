namespace GGNet.Components;

public sealed class AutoRenderPolicy : RenderPolicyBase
{
	private readonly CancellationTokenSource cancellationTokenSource = new();

	private readonly SemaphoreSlim semaphore = new(0, 1);

	private ChannelWriter<int> writer = default!;

	private volatile int render = 0;

	private readonly Task task;

	public AutoRenderPolicy(IPlot plot)
		: base(plot)
	{
		task = Task.Factory.StartNew(async () =>
		{
			try
			{
				var channel = Channel.CreateUnbounded<int>(new()
				{
					SingleReader = true,
					SingleWriter = true
				});

				var reader = channel.Reader;
				writer = channel.Writer;

				var token = cancellationTokenSource.Token;

				while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
				{
					while (reader.TryRead(out _))
					{
					}

					plot.Render();

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

	public override async Task RefreshAsync()
	{
		await writer.WriteAsync(1, cancellationTokenSource.Token);
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

	public sealed class RenderChildPolicy : RenderChildPolicyBase
	{
		private volatile int render = 0;

		public override void Refresh() => Interlocked.Exchange(ref render, 1);

		public override bool ShouldRender() => Interlocked.Exchange(ref render, 0) == 1;
	}

	public override RenderChildPolicyBase Child() => new RenderChildPolicy();

	public override async ValueTask DisposeAsync()
	{
		cancellationTokenSource?.Cancel();
		cancellationTokenSource?.Dispose();

		if (task is not null)
		{
			await task.ConfigureAwait(false);
		}
	}
}
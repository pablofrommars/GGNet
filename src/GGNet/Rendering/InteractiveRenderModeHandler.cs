namespace GGNet.Rendering;

public sealed class InteractiveRenderModeHandler : RenderModeHandler
{
  private readonly CancellationTokenSource cancellationTokenSource = new();

  private readonly SemaphoreSlim semaphore = new(0, 1);

  private volatile int render = 0;

  readonly Channel<RenderTarget> channel = Channel.CreateUnbounded<RenderTarget>(new()
  {
    SingleReader = true,
    SingleWriter = true
  });

  private readonly Task background;

  public InteractiveRenderModeHandler(IPlotRendering plot)
    : base(plot)
  {
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
    private volatile int render = 0;

    public void Refresh(RenderTarget target)
      => Interlocked.Exchange(ref render, (int)target);

    public bool ShouldRender(RenderTarget target = RenderTarget.All)
      => ((RenderTarget)Interlocked.Exchange(ref render, 0) & target) != RenderTarget.None;
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
          var mask = RenderTarget.None;

          while (channel.Reader.TryRead(out var target))
          {
            mask |= target;
          }

          if (mask == RenderTarget.None)
          {
            continue;
          }

          plot.Render(mask);

          Interlocked.Exchange(ref render, 1);

          await plot.StateHasChangedAsync();

          await semaphore.WaitAsync(cancellationTokenSource.Token);
        }
        catch (Exception)
        {
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

  private volatile int disposing = 0;

  public override async ValueTask DisposeAsync()
  {
    if (Interlocked.CompareExchange(ref disposing, 1, 0) == 1)
    {
      return;
    }

    cancellationTokenSource.Cancel();

    if (background is not null)
    {
      await background.ConfigureAwait(false);
    }

    cancellationTokenSource.Dispose();

    semaphore.Dispose();
  }
}
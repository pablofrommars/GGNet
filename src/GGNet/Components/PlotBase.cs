namespace GGNet.Components;

using Rendering;

public abstract class PlotBase<T, TX, TY> : ComponentBase, IPlot, IPlotRendering, IAsyncDisposable
  where TX : struct
  where TY : struct
{
  [Parameter]
  public required PlotContext<T, TX, TY> Context { get; init; }

  [Parameter]
  public required RenderMode RenderMode { get; init; }

  public string Id => Context.Id;

  public Style Style => Context.Style!;

  protected override void OnInitialized()
  {
    RenderModeHandler = Rendering.RenderModeHandler.Factory(RenderMode, this);
  }

  public IRenderModeHandler? RenderModeHandler { get; set; }

  public abstract void Render(RenderTarget target);

  public Task StateHasChangedAsync() => InvokeAsync(StateHasChanged);

  protected override bool ShouldRender() => RenderModeHandler?.ShouldRender() ?? true;

  protected override void OnAfterRender(bool firstRender) => RenderModeHandler?.OnAfterRender(firstRender);

  public Task RefreshAsync(RenderTarget target, CancellationToken token)
  {
    if (RenderModeHandler is null)
    {
      return Task.CompletedTask;
    }

    return RenderModeHandler.RefreshAsync(target, token);
  }

  private int disposing = 0;

  public ValueTask DisposeAsync()
  {
    if (Interlocked.CompareExchange(ref disposing, 1, 0) == 1)
    {
      return ValueTask.CompletedTask;
    }

    if (RenderModeHandler is null)
    {
      return ValueTask.CompletedTask;
    }

    return RenderModeHandler.DisposeAsync();
  }
}

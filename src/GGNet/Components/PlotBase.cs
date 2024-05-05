namespace GGNet.Components;

using Rendering;

public abstract class PlotBase<T, TX, TY> : ComponentBase, IPlot, IPlotRendering, IAsyncDisposable
  where TX : struct
  where TY : struct
{
  [Parameter]
  public required PlotContext<T, TX, TY> Context { get; init; }

  [Parameter]
  public required RenderPolicy RenderPolicy { get; init; }

  public string Id => Context.Id;

  public Style Style => Context.Style!;

  protected override void OnInitialized()
  {
    Policy = RenderPolicyBase.Factory(RenderPolicy, this);
  }

  public IRenderPolicy? Policy { get; set; }

  public abstract void Render(RenderTarget target);

  public Task StateHasChangedAsync() => InvokeAsync(StateHasChanged);

  protected override bool ShouldRender() => Policy?.ShouldRender() ?? true;

  protected override void OnAfterRender(bool firstRender) => Policy?.OnAfterRender(firstRender);

  public Task RefreshAsync(RenderTarget target)
  {
    if (Policy is null)
    {
      return Task.CompletedTask;
    }

    return Policy.RefreshAsync(target);
  }

  private int disposing = 0;

  public ValueTask DisposeAsync()
  {
    if (Interlocked.CompareExchange(ref disposing, 1, 0) == 1)
    {
      return ValueTask.CompletedTask;
    }

    if (Policy is null)
    {
      return ValueTask.CompletedTask;
    }

    return Policy.DisposeAsync();
  }
}

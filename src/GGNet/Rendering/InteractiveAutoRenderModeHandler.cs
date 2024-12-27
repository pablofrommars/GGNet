namespace GGNet.Rendering;

public sealed class InteractiveAutoRenderModeHandler(IPlotRendering plot) : RenderModeHandler(plot)
{
  public override Task RefreshAsync(RenderTarget target, CancellationToken token)
  {
    plot.Render(RenderTarget.All);

    return Task.CompletedTask;
  }

  public override bool ShouldRender() => true;

  public sealed class ChildRenderHandler : IChildRenderModeHandler
  {
    public void Refresh(RenderTarget target = RenderTarget.All)
    {
    }

    public bool ShouldRender(RenderTarget target) => true;
  }

  public override IChildRenderModeHandler Child() => new ChildRenderHandler();
}

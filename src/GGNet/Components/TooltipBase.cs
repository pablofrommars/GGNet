namespace GGNet.Components;

public abstract class TooltipBase : ComponentBase, ITooltip
{
  [Parameter]
  public required ICoord Coord { get; set; }

  [Parameter]
  public Zone Zone { get; set; }

  protected readonly RenderFragment _renderForeignObject;

  internal TooltipContext? context;

  public TooltipBase()
  {
    _renderForeignObject = RenderForeignObject;
  }

  protected abstract void RenderForeignObject(RenderTreeBuilder __builder);

  public void Show(double x, double y, double offset, RenderFragment content, string? color = null, double? opacity = null)
  {
    context = new(
      X: Coord.ToX(x),
      Y: Coord.ToY(y),
      Offset: offset,
      Content: content,
      Color: color ?? "#ffffff",
      Opacity: opacity ?? 1.0
    );

    StateHasChanged();
  }

  public void Hide()
  {
    context = null;

    StateHasChanged();
  }
}

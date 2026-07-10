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

	public void Show(double x, double y, double offset, RenderFragment content, string? color = null, double? alpha = null)
	{
		var (px, py) = Coord.Project(x, y);

		context = new(
		  X: px,
		  Y: py,
		  Offset: offset,
		  Content: content,
		  Color: color ?? "#ffffff",
		  Opacity: alpha ?? 1.0
		);

		_ = InvokeAsync(StateHasChanged);
	}

	public void Hide()
	{
		context = null;

		_ = InvokeAsync(StateHasChanged);
	}
}

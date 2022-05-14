using GGNet.Common;

namespace GGNet.Components.Tooltips;

using Theme = Theme.Theme;

public abstract class Tooltip : ComponentBase, ITooltip
{
	[Parameter]
	public string Id { get; set; } = default!;

	[Parameter]
	public ICoord Coord { get; set; } = default!;

	[Parameter]
	public Zone Area { get; set; }

	[Parameter]
	public Theme Theme { get; set; } = default!;

	protected bool visibility = false;

	protected string? color;
	protected double alpha;
	protected string? themeColor;
	protected double themeAlpha;

	protected string? foreignObject;

	protected abstract string Render(double x, double y, double offset, string content);

	public void Show(double x, double y, double offset, string content, string? color = null, double? alpha = null)
	{
		visibility = true;

		this.color = color ?? "#FFFFFF";
		this.alpha = alpha ?? 1.0;
		
		themeColor = Theme.Tooltip.Background ?? color ?? "#FFFFFF";
		themeAlpha = Theme.Tooltip.Alpha ?? alpha ?? 1.0;

		foreignObject = Render(Coord.CoordX(x), Coord.CoordY(y), offset, content);

		StateHasChanged();
	}

	public void Hide()
	{
		visibility = false;
		StateHasChanged();
	}
}
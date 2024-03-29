﻿using GGNet.Common;

namespace GGNet.Components.Tooltips;

using Theme = Theme.Theme;

public abstract class Tooltip : ComponentBase, ITooltip
{
	[Parameter]
	public required string Id { get; set; }

	[Parameter]
	public required ICoord Coord { get; set; }

	[Parameter]
	public Zone Area { get; set; }

	[Parameter]
	public required Theme Theme { get; set; }

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
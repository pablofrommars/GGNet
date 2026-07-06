namespace GGNet;

using Geoms.Line;
using Scales;
using static LineType;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds a line layer: one path per color/line-type combination, points connected in x order.
	/// </summary>
	/// <param name="source">Items to plot; one vertex per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Position along y, in y-axis data units. Defaults to the plot's y selector.</param>
	/// <param name="colorBy">Data-driven color: participates in the color scale and the legend. Build with <c>Scale_Color_Discrete</c>.</param>
	/// <param name="lineTypeBy">Data-driven dash pattern: participates in the line-type scale and the legend. Build with <c>Scale_LineType_Discrete</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hovered vertex when no explicit hover handlers are set.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke color for the whole layer; with <paramref name="colorBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	/// <param name="piecewise">Break the path at NaN y values (pen lift) instead of skipping them.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Line<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, string>? colorBy = null,
	  IAestheticMapping<T2, LineType>? lineTypeBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  (bool x, bool y)? scale = null, bool inherit = true, bool piecewise = false)
	  where TX1 : struct
	  where TY1 : struct
	{
		panel.AddTyped(() =>
		{
			var geom = new Line<T2, TX1, TY1>(source, x, y, colorBy, lineTypeBy, tooltip, scale, piecewise)
			{
				Aesthetic = new()
				{
					Stroke = color,
					StrokeOpacity = opacity,
					StrokeWidth = strokeWidth,
					LineType = lineType
				},
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout
			};

			return geom;
		});

		return panel;
	}

	/// <summary>
	/// Adds a line layer: one path per color/line-type combination, points connected in x order.
	/// </summary>
	/// <param name="source">Items to plot; one vertex per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Position along y, in y-axis data units. Defaults to the plot's y selector.</param>
	/// <param name="colorBy">Data-driven color: participates in the color scale and the legend. Build with <c>Scale_Color_Discrete</c>.</param>
	/// <param name="lineTypeBy">Data-driven dash pattern: participates in the line-type scale and the legend. Build with <c>Scale_LineType_Discrete</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hovered vertex when no explicit hover handlers are set.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke color for the whole layer; with <paramref name="colorBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	/// <param name="piecewise">Break the path at NaN y values (pen lift) instead of skipping them.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Line<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, string>? colorBy = null,
	  IAestheticMapping<T2, LineType>? lineTypeBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  (bool x, bool y)? scale = null, bool inherit = true, bool piecewise = false)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Line(source, x, y, colorBy, lineTypeBy, onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType, scale, inherit, piecewise);

		return context;
	}

	/// <summary>
	/// Adds a line layer: one path per color/line-type combination, points connected in x order.
	/// </summary>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Position along y, in y-axis data units. Defaults to the plot's y selector.</param>
	/// <param name="colorBy">Data-driven color: participates in the color scale and the legend. Build with <c>Scale_Color_Discrete</c>.</param>
	/// <param name="lineTypeBy">Data-driven dash pattern: participates in the line-type scale and the legend. Build with <c>Scale_LineType_Discrete</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hovered vertex when no explicit hover handlers are set.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke color for the whole layer; with <paramref name="colorBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	/// <param name="piecewise">Break the path at NaN y values (pen lift) instead of skipping them.</param>
	public static PanelFactory<T, TX, TY> Geom_Line<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  IAestheticMapping<T, string>? colorBy = null,
	  IAestheticMapping<T, LineType>? lineTypeBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  (bool x, bool y)? scale = null, bool inherit = true, bool piecewise = false)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Line(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, colorBy ?? (inherit ? panel.Context.Aesthetics.Color : null), lineTypeBy ?? (inherit ? panel.Context.Aesthetics.LineType : null), onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType, scale, inherit, piecewise);
	}

	/// <summary>
	/// Adds a line layer: one path per color/line-type combination, points connected in x order.
	/// </summary>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Position along y, in y-axis data units. Defaults to the plot's y selector.</param>
	/// <param name="colorBy">Data-driven color: participates in the color scale and the legend. Build with <c>Scale_Color_Discrete</c>.</param>
	/// <param name="lineTypeBy">Data-driven dash pattern: participates in the line-type scale and the legend. Build with <c>Scale_LineType_Discrete</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hovered vertex when no explicit hover handlers are set.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke color for the whole layer; with <paramref name="colorBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	/// <param name="piecewise">Break the path at NaN y values (pen lift) instead of skipping them.</param>
	public static PlotContext<T, TX, TY> Geom_Line<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  IAestheticMapping<T, string>? colorBy = null,
	  IAestheticMapping<T, LineType>? lineTypeBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  (bool x, bool y)? scale = null, bool inherit = true, bool piecewise = false)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Line(x, y, colorBy, lineTypeBy, onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType, scale, inherit, piecewise);

		return context;
	}
}

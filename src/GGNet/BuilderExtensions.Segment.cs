namespace GGNet;

using Geoms.Segment;
using static LineType;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds a segment layer: one line from (x, y) to (xend, yend) per item.
	/// </summary>
	/// <param name="source">Items to plot; one segment per item.</param>
	/// <param name="x">Start x, in x-axis data units.</param>
	/// <param name="xend">End x, in x-axis data units.</param>
	/// <param name="y">Start y, in y-axis data units.</param>
	/// <param name="yend">End y, in y-axis data units.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the segment midpoint when no explicit hover handlers are set.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke color for the layer.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Segment<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1> x,
	  Func<T2, TX1> xend,
	  Func<T2, TY1> y,
	  Func<T2, TY1> yend,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)
	  where TX1 : struct
	  where TY1 : struct
	{
		panel.AddTyped(() =>
		{
			var geom = new Segment<T2, TX1, TY1>(source, x, xend, y, yend, tooltip)
			{
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout,
				Aesthetic = new()
				{
					Stroke = color,
					StrokeOpacity = opacity,
					StrokeWidth = strokeWidth,
					LineType = lineType
				}
			};

			return geom;
		});

		return panel;
	}

	/// <summary>
	/// Adds a segment layer: one line from (x, y) to (xend, yend) per item.
	/// </summary>
	/// <param name="source">Items to plot; one segment per item.</param>
	/// <param name="x">Start x, in x-axis data units.</param>
	/// <param name="xend">End x, in x-axis data units.</param>
	/// <param name="y">Start y, in y-axis data units.</param>
	/// <param name="yend">End y, in y-axis data units.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the segment midpoint when no explicit hover handlers are set.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke color for the layer.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Segment<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  Source<T2> source,
	  Func<T2, TX1> x,
	  Func<T2, TX1> xend,
	  Func<T2, TY1> y,
	  Func<T2, TY1> yend,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Segment(source, x, xend, y, yend, onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType);

		return context;
	}

	/// <summary>
	/// Adds a segment layer: one line from (x, y) to (xend, yend) per item.
	/// </summary>
	/// <param name="source">Items to plot; one segment per item.</param>
	/// <param name="x">Start x, in x-axis data units.</param>
	/// <param name="xend">End x, in x-axis data units.</param>
	/// <param name="y">Start y, in y-axis data units.</param>
	/// <param name="yend">End y, in y-axis data units.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the segment midpoint when no explicit hover handlers are set.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke color for the layer.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Segment<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IEnumerable<T2> source,
	  Func<T2, TX1> x,
	  Func<T2, TX1> xend,
	  Func<T2, TY1> y,
	  Func<T2, TY1> yend,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)
	  where TX1 : struct
	  where TY1 : struct
	  => context.Geom_Segment(new Source<T2>(source), x, xend, y, yend, onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType);

	/// <summary>
	/// Adds a segment layer: one line from (x, y) to (xend, yend) per item.
	/// </summary>
	/// <param name="x">Start x, in x-axis data units.</param>
	/// <param name="xend">End x, in x-axis data units.</param>
	/// <param name="y">Start y, in y-axis data units.</param>
	/// <param name="yend">End y, in y-axis data units.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the segment midpoint when no explicit hover handlers are set.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke color for the layer.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	public static PanelFactory<T, TX, TY> Geom_Segment<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX> x,
	  Func<T, TX> xend,
	  Func<T, TY> y,
	  Func<T, TY> yend,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Segment(panel, panel.Context.RequireSource(), x, xend, y, yend, onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType);
	}

	/// <summary>
	/// Adds a segment layer: one line from (x, y) to (xend, yend) per item.
	/// </summary>
	/// <param name="x">Start x, in x-axis data units.</param>
	/// <param name="xend">End x, in x-axis data units.</param>
	/// <param name="y">Start y, in y-axis data units.</param>
	/// <param name="yend">End y, in y-axis data units.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the segment midpoint when no explicit hover handlers are set.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke color for the layer.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	public static PlotContext<T, TX, TY> Geom_Segment<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX> x,
	  Func<T, TX> xend,
	  Func<T, TY> y,
	  Func<T, TY> yend,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Segment(x, xend, y, yend, onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType);

		return context;
	}
}

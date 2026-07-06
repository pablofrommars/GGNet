namespace GGNet;

using Geoms.Point;
using Scales;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds a point layer: one circle per item.
	/// </summary>
	/// <param name="source">Items to plot; one point per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Position along y, in y-axis data units. Defaults to the plot's y selector.</param>
	/// <param name="sizeBy">Data-driven size: participates in the size scale and the legend. Build with <c>Scale_Size_Continuous</c>.</param>
	/// <param name="colorBy">Data-driven color: participates in the color scale and the legend. Build with <c>Scale_Color_Discrete</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the point when no explicit hover handlers are set.</param>
	/// <param name="animation">Adds the <c>animate-point</c> css class to each circle.</param>
	/// <param name="size">Circle radius in pixels.</param>
	/// <param name="color">Constant fill and stroke color for the whole layer; with <paramref name="colorBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Stroke and fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Point<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, double>? sizeBy = null,
	  IAestheticMapping<T2, string>? colorBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  bool animation = false,
	  double size = 5, string color = "#23d0fc", double opacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		panel.AddTyped(() =>
		{
			var geom = new Point<T2, TX1, TY1>(source, x, y, sizeBy, colorBy, tooltip, animation, scale)
			{
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout,
				Aesthetic = new()
				{
					Radius = size,
					Fill = color,
					FillOpacity = opacity,
				}
			};

			return geom;
		});

		return panel;
	}

	/// <summary>
	/// Adds a point layer: one circle per item.
	/// </summary>
	/// <param name="source">Items to plot; one point per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Position along y, in y-axis data units. Defaults to the plot's y selector.</param>
	/// <param name="sizeBy">Data-driven size: participates in the size scale and the legend. Build with <c>Scale_Size_Continuous</c>.</param>
	/// <param name="colorBy">Data-driven color: participates in the color scale and the legend. Build with <c>Scale_Color_Discrete</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the point when no explicit hover handlers are set.</param>
	/// <param name="animation">Adds the <c>animate-point</c> css class to each circle.</param>
	/// <param name="size">Circle radius in pixels.</param>
	/// <param name="color">Constant fill and stroke color for the whole layer; with <paramref name="colorBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Stroke and fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Point<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, double>? sizeBy = null,
	  IAestheticMapping<T2, string>? colorBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  bool animation = false,
	  double size = 5, string color = "#23d0fc", double opacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return panel.Geom_Point(new Source<T2>(source), x, y, sizeBy, colorBy, onclick, onmouseover, onmouseout, tooltip, animation, size, color, opacity, scale);
	}

	/// <summary>
	/// Adds a point layer: one circle per item.
	/// </summary>
	/// <param name="source">Items to plot; one point per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Position along y, in y-axis data units. Defaults to the plot's y selector.</param>
	/// <param name="sizeBy">Data-driven size: participates in the size scale and the legend. Build with <c>Scale_Size_Continuous</c>.</param>
	/// <param name="colorBy">Data-driven color: participates in the color scale and the legend. Build with <c>Scale_Color_Discrete</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the point when no explicit hover handlers are set.</param>
	/// <param name="animation">Adds the <c>animate-point</c> css class to each circle.</param>
	/// <param name="size">Circle radius in pixels.</param>
	/// <param name="color">Constant fill and stroke color for the whole layer; with <paramref name="colorBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Stroke and fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Point<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, double>? sizeBy = null,
	  IAestheticMapping<T2, string>? colorBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  bool animation = false,
	  double size = 5, string color = "#23d0fc", double opacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Point(source, x, y, sizeBy, colorBy, onclick, onmouseover, onmouseout, tooltip, animation, size, color, opacity, scale);

		return context;
	}

	/// <summary>
	/// Adds a point layer: one circle per item.
	/// </summary>
	/// <param name="source">Items to plot; one point per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Position along y, in y-axis data units. Defaults to the plot's y selector.</param>
	/// <param name="sizeBy">Data-driven size: participates in the size scale and the legend. Build with <c>Scale_Size_Continuous</c>.</param>
	/// <param name="colorBy">Data-driven color: participates in the color scale and the legend. Build with <c>Scale_Color_Discrete</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the point when no explicit hover handlers are set.</param>
	/// <param name="animation">Adds the <c>animate-point</c> css class to each circle.</param>
	/// <param name="size">Circle radius in pixels.</param>
	/// <param name="color">Constant fill and stroke color for the whole layer; with <paramref name="colorBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Stroke and fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Point<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, double>? sizeBy = null,
	  IAestheticMapping<T2, string>? colorBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  bool animation = false,
	  double size = 5, string color = "#23d0fc", double opacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return context.Geom_Point(new Source<T2>(source), x, y, sizeBy, colorBy, onclick, onmouseover, onmouseout, tooltip, animation, size, color, opacity, scale);
	}

	/// <summary>
	/// Adds a point layer: one circle per item.
	/// </summary>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Position along y, in y-axis data units. Defaults to the plot's y selector.</param>
	/// <param name="sizeBy">Data-driven size: participates in the size scale and the legend. Build with <c>Scale_Size_Continuous</c>.</param>
	/// <param name="colorBy">Data-driven color: participates in the color scale and the legend. Build with <c>Scale_Color_Discrete</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the point when no explicit hover handlers are set.</param>
	/// <param name="animation">Adds the <c>animate-point</c> css class to each circle.</param>
	/// <param name="size">Circle radius in pixels.</param>
	/// <param name="color">Constant fill and stroke color for the whole layer; with <paramref name="colorBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Stroke and fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T, TX, TY> Geom_Point<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  IAestheticMapping<T, double>? sizeBy = null,
	  IAestheticMapping<T, string>? colorBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  bool animation = false,
	  double size = 5, string color = "#23d0fc", double opacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Point(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, sizeBy ?? (inherit ? panel.Context.Aesthetics.Size : null), colorBy ?? (inherit ? panel.Context.Aesthetics.Color : null), onclick, onmouseover, onmouseout, tooltip, animation, size, color, opacity, scale);
	}

	/// <summary>
	/// Adds a point layer: one circle per item.
	/// </summary>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Position along y, in y-axis data units. Defaults to the plot's y selector.</param>
	/// <param name="sizeBy">Data-driven size: participates in the size scale and the legend. Build with <c>Scale_Size_Continuous</c>.</param>
	/// <param name="colorBy">Data-driven color: participates in the color scale and the legend. Build with <c>Scale_Color_Discrete</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the point when no explicit hover handlers are set.</param>
	/// <param name="animation">Adds the <c>animate-point</c> css class to each circle.</param>
	/// <param name="size">Circle radius in pixels.</param>
	/// <param name="color">Constant fill and stroke color for the whole layer; with <paramref name="colorBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Stroke and fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T, TX, TY> Geom_Point<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  IAestheticMapping<T, double>? sizeBy = null,
	  IAestheticMapping<T, string>? colorBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  bool animation = false,
	  double size = 5, string color = "#23d0fc", double opacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Point(x, y, sizeBy, colorBy, onclick, onmouseover, onmouseout, tooltip, animation, size, color, opacity, scale);

		return context;
	}
}

namespace GGNet;

using Geoms.Bar;
using Scales;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds a bar layer: bars grow from zero along y, grouped and stacked or dodged per x key. Under <c>Flip()</c> the statistics run along x instead.
	/// </summary>
	/// <param name="source">Items to plot; one bar segment per item.</param>
	/// <param name="x">Bar position, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Segment length, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the bar’s top-center when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="strokeColor">Outline color; <c>"inherit"</c> takes the theme’s.</param>
	/// <param name="strokeOpacity">Outline opacity, 0–1.</param>
	/// <param name="strokeWidth">Outline width in pixels; 0 draws no outline.</param>
	/// <param name="position"><c>Stack</c> piles segments per key; <c>Dodge</c> places them side by side.</param>
	/// <param name="width">Bar width in x-axis data units, as a fraction of the smallest key spacing — not pixels. 0.9 leaves a gap between categories.</param>
	/// <param name="animation">Adds the <c>animate-bar</c> css class to each bar.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Bar<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
	  PositionAdjustment position = PositionAdjustment.Stack,
	  double width = 0.9,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		panel.AddTyped(() =>
		{
			var geom = new Bar<T2, TX1, TY1>(source, x, y, fillBy, tooltip, position, width, animation, scale)
			{
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout,
				Aesthetic = new()
				{
					Fill = fill,
					FillOpacity = fillOpacity,
					Stroke = strokeColor,
					StrokeOpacity = strokeOpacity,
					StrokeWidth = strokeWidth,
				}
			};

			return geom;
		});

		return panel;
	}

	/// <summary>
	/// Adds a bar layer: bars grow from zero along y, grouped and stacked or dodged per x key. Under <c>Flip()</c> the statistics run along x instead.
	/// </summary>
	/// <param name="source">Items to plot; one bar segment per item.</param>
	/// <param name="x">Bar position, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Segment length, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the bar’s top-center when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="strokeColor">Outline color; <c>"inherit"</c> takes the theme’s.</param>
	/// <param name="strokeOpacity">Outline opacity, 0–1.</param>
	/// <param name="strokeWidth">Outline width in pixels; 0 draws no outline.</param>
	/// <param name="position"><c>Stack</c> piles segments per key; <c>Dodge</c> places them side by side.</param>
	/// <param name="width">Bar width in x-axis data units, as a fraction of the smallest key spacing — not pixels. 0.9 leaves a gap between categories.</param>
	/// <param name="animation">Adds the <c>animate-bar</c> css class to each bar.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Bar<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
	  PositionAdjustment position = PositionAdjustment.Stack,
	  double width = 0.9,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Bar(source, x, y, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, strokeColor, strokeOpacity, strokeWidth, position, width, animation, scale);

		return context;
	}

	/// <summary>
	/// Adds a bar layer: bars grow from zero along y, grouped and stacked or dodged per x key. Under <c>Flip()</c> the statistics run along x instead.
	/// </summary>
	/// <param name="x">Bar position, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Segment length, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the bar’s top-center when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="strokeColor">Outline color; <c>"inherit"</c> takes the theme’s.</param>
	/// <param name="strokeOpacity">Outline opacity, 0–1.</param>
	/// <param name="strokeWidth">Outline width in pixels; 0 draws no outline.</param>
	/// <param name="position"><c>Stack</c> piles segments per key; <c>Dodge</c> places them side by side.</param>
	/// <param name="width">Bar width in x-axis data units, as a fraction of the smallest key spacing — not pixels. 0.9 leaves a gap between categories.</param>
	/// <param name="animation">Adds the <c>animate-bar</c> css class to each bar.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T, TX, TY> Geom_Bar<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
	  PositionAdjustment position = PositionAdjustment.Stack,
	  double width = 0.9,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Bar(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, fillBy ?? (inherit ? panel.Context.Aesthetics.Fill : null), onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, strokeColor, strokeOpacity, strokeWidth, position, width, animation, scale);
	}

	/// <summary>
	/// Adds a bar layer: bars grow from zero along y, grouped and stacked or dodged per x key. Under <c>Flip()</c> the statistics run along x instead.
	/// </summary>
	/// <param name="x">Bar position, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Segment length, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the bar’s top-center when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="strokeColor">Outline color; <c>"inherit"</c> takes the theme’s.</param>
	/// <param name="strokeOpacity">Outline opacity, 0–1.</param>
	/// <param name="strokeWidth">Outline width in pixels; 0 draws no outline.</param>
	/// <param name="position"><c>Stack</c> piles segments per key; <c>Dodge</c> places them side by side.</param>
	/// <param name="width">Bar width in x-axis data units, as a fraction of the smallest key spacing — not pixels. 0.9 leaves a gap between categories.</param>
	/// <param name="animation">Adds the <c>animate-bar</c> css class to each bar.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T, TX, TY> Geom_Bar<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
	  PositionAdjustment position = PositionAdjustment.Stack,
	  double width = 0.9,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Bar(x, y, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, strokeColor, strokeOpacity, strokeWidth, position, width, animation, scale);

		return context;
	}
}

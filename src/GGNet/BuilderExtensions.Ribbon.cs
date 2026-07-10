namespace GGNet;

using Geoms.Ribbon;
using Scales;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds a ribbon layer: the band between ymin and ymax along x.
	/// </summary>
	/// <param name="source">Items to plot; one band cross-section per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="ymin">Lower band edge, in y-axis data units.</param>
	/// <param name="ymax">Upper band edge, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hovered cross-section when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Ribbon<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? ymin = null,
	  Func<T2, TY1>? ymax = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		panel.AddTyped(() =>
		{
			var geom = new Ribbon<T2, TX1, TY1>(source, x, ymin, ymax, fillBy, tooltip, scale)
			{
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout,
				Aesthetic = new()
				{
					Fill = fill,
					FillOpacity = fillOpacity
				}
			};

			return geom;
		});

		return panel;
	}

	/// <summary>
	/// Adds a ribbon layer: the band between ymin and ymax along x.
	/// </summary>
	/// <param name="source">Items to plot; one band cross-section per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="ymin">Lower band edge, in y-axis data units.</param>
	/// <param name="ymax">Upper band edge, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hovered cross-section when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Ribbon<T1, TX1, TY1, T2>(
	   this PanelFactory<T1, TX1, TY1> panel,
	   IEnumerable<T2> source,
	   Func<T2, TX1>? x = null,
	   Func<T2, TY1>? ymin = null,
	   Func<T2, TY1>? ymax = null,
	   IAestheticMapping<T2, string>? fillBy = null,
	   Func<T2, MouseEventArgs, Task>? onclick = null,
	   Func<T2, MouseEventArgs, Task>? onmouseover = null,
	   Func<T2, MouseEventArgs, Task>? onmouseout = null,
	   Func<T2, RenderFragment>? tooltip = null,
	   string fill = "#23d0fc", double fillOpacity = 1.0,
	   (bool x, bool y)? scale = null, bool inherit = true)
	   where TX1 : struct
	   where TY1 : struct
	{
		return panel.Geom_Ribbon(new Source<T2>(source), x, ymin, ymax, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, scale);
	}

	/// <summary>
	/// Adds a ribbon layer: the band between ymin and ymax along x.
	/// </summary>
	/// <param name="source">Items to plot; one band cross-section per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="ymin">Lower band edge, in y-axis data units.</param>
	/// <param name="ymax">Upper band edge, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hovered cross-section when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Ribbon<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? ymin = null,
	  Func<T2, TY1>? ymax = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Ribbon(source, x, ymin, ymax, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, scale);

		return context;
	}

	/// <summary>
	/// Adds a ribbon layer: the band between ymin and ymax along x.
	/// </summary>
	/// <param name="source">Items to plot; one band cross-section per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="ymin">Lower band edge, in y-axis data units.</param>
	/// <param name="ymax">Upper band edge, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hovered cross-section when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Ribbon<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? ymin = null,
	  Func<T2, TY1>? ymax = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return context.Geom_Ribbon(new Source<T2>(source), x, ymin, ymax, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, scale);
	}

	/// <summary>
	/// Adds a ribbon layer: the band between ymin and ymax along x.
	/// </summary>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="ymin">Lower band edge, in y-axis data units.</param>
	/// <param name="ymax">Upper band edge, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hovered cross-section when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T, TX, TY> Geom_Ribbon<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? ymin = null,
	  Func<T, TY>? ymax = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Ribbon(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, ymin ?? panel.Context.Selectors.Y, ymax ?? panel.Context.Selectors.Y, fillBy ?? (inherit ? panel.Context.Aesthetics.Fill : null), onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, scale);
	}

	/// <summary>
	/// Adds a ribbon layer: the band between ymin and ymax along x.
	/// </summary>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="ymin">Lower band edge, in y-axis data units.</param>
	/// <param name="ymax">Upper band edge, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hovered cross-section when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T, TX, TY> Geom_Ribbon<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? ymin = null,
	  Func<T, TY>? ymax = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Ribbon(x, ymin, ymax, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, scale);

		return context;
	}
}

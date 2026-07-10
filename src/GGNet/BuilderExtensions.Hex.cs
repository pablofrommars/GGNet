namespace GGNet;

using Geoms.Hex;
using Exceptions;
using Scales;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds a hex layer: one hexagon per item (binned density maps).
	/// </summary>
	/// <param name="source">Items to plot; one hexagon per item.</param>
	/// <param name="x">Hexagon center x, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Hexagon center y, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="dx">Horizontal half-extent per item, in x-axis data units.</param>
	/// <param name="dy">Vertical half-extent per item, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hexagon center when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Fill opacity, 0–1.</param>
	/// <param name="animation">Adds the <c>animate-hex</c> css class.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Hex<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, TX1>? dx = null,
	  Func<T2, TY1>? dy = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double opacity = 1.0,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		if (dx is null)
		{
			throw new GGNetUserException($"{nameof(dx)} selector should not be null");
		}

		if (dy is null)
		{
			throw new GGNetUserException($"{nameof(dy)} selector should not be null");
		}

		panel.AddTyped(() =>
		{
			var geom = new Hex<T2, TX1, TY1>(source, x, y, dx, dy, fillBy, tooltip, animation, scale)
			{
				Aesthetic = new()
				{
					Fill = fill,
					FillOpacity = opacity
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
	/// Adds a hex layer: one hexagon per item (binned density maps).
	/// </summary>
	/// <param name="source">Items to plot; one hexagon per item.</param>
	/// <param name="x">Hexagon center x, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Hexagon center y, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="dx">Horizontal half-extent per item, in x-axis data units.</param>
	/// <param name="dy">Vertical half-extent per item, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hexagon center when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Fill opacity, 0–1.</param>
	/// <param name="animation">Adds the <c>animate-hex</c> css class.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Hex<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, TX1>? dx = null,
	  Func<T2, TY1>? dy = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double opacity = 1.0,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return panel.Geom_Hex(new Source<T2>(source), x, y, dx, dy, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, opacity, animation, scale);
	}

	/// <summary>
	/// Adds a hex layer: one hexagon per item (binned density maps).
	/// </summary>
	/// <param name="source">Items to plot; one hexagon per item.</param>
	/// <param name="x">Hexagon center x, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Hexagon center y, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="dx">Horizontal half-extent per item, in x-axis data units.</param>
	/// <param name="dy">Vertical half-extent per item, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hexagon center when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Fill opacity, 0–1.</param>
	/// <param name="animation">Adds the <c>animate-hex</c> css class.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Hex<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, TX1>? dx = null,
	  Func<T2, TY1>? dy = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double opacity = 1.0,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Hex(source, x, y, dx, dy, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, opacity, animation, scale);

		return context;
	}

	/// <summary>
	/// Adds a hex layer: one hexagon per item (binned density maps).
	/// </summary>
	/// <param name="source">Items to plot; one hexagon per item.</param>
	/// <param name="x">Hexagon center x, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Hexagon center y, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="dx">Horizontal half-extent per item, in x-axis data units.</param>
	/// <param name="dy">Vertical half-extent per item, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hexagon center when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Fill opacity, 0–1.</param>
	/// <param name="animation">Adds the <c>animate-hex</c> css class.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Hex<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, TX1>? dx = null,
	  Func<T2, TY1>? dy = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double opacity = 1.0,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return context.Geom_Hex(new Source<T2>(source), x, y, dx, dy, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, opacity, animation, scale);
	}

	/// <summary>
	/// Adds a hex layer: one hexagon per item (binned density maps).
	/// </summary>
	/// <param name="x">Hexagon center x, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Hexagon center y, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="dx">Horizontal half-extent per item, in x-axis data units.</param>
	/// <param name="dy">Vertical half-extent per item, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hexagon center when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Fill opacity, 0–1.</param>
	/// <param name="animation">Adds the <c>animate-hex</c> css class.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T, TX, TY> Geom_Hex<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, TX>? dx = null,
	  Func<T, TY>? dy = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double opacity = 1.0,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Hex(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, dx, dy, fillBy ?? (inherit ? panel.Context.Aesthetics.Fill : null), onclick, onmouseover, onmouseout, tooltip, fill, opacity, animation, scale);
	}

	/// <summary>
	/// Adds a hex layer: one hexagon per item (binned density maps).
	/// </summary>
	/// <param name="x">Hexagon center x, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Hexagon center y, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="dx">Horizontal half-extent per item, in x-axis data units.</param>
	/// <param name="dy">Vertical half-extent per item, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hexagon center when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="opacity">Fill opacity, 0–1.</param>
	/// <param name="animation">Adds the <c>animate-hex</c> css class.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T, TX, TY> Geom_Hex<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, TX>? dx = null,
	  Func<T, TY>? dy = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double opacity = 1.0,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Hex(x, y, dx, dy, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, opacity, animation, scale);

		return context;
	}
}

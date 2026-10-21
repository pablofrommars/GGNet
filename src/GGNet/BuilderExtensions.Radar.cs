namespace GGNet;

using Geoms.Radar;
using Scales;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds a radar layer: a closed polygon per mapped fill over the angular categories. Implies polar coordinates.
	/// </summary>
	/// <param name="source">Items to plot; one vertex per category.</param>
	/// <param name="x">Angular category. Defaults to the plot's x selector only when no source is supplied; required with an explicit source.</param>
	/// <param name="y">Radial value, zero at the center. Defaults to the plot's y selector only when no source is supplied; required with an explicit source.</param>
	/// <param name="fillBy">Explicit data-driven fill mapping, overriding inherited Fill. Configure inherited mappings with <c>Scale_Fill_Discrete</c>/<c>Scale_Fill_Continuous</c>. Equal mapped fill colors share a polygon; varying colors can split one entity. Color mappings are not inherited.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hovered vertex when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill and stroke for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Resolve missing aesthetic mappings from the plot when this layer is added. Explicit mappings win. With an explicit source, an incompatible inherited row type throws; supply an explicit mapping or set <c>inherit: false</c>. Does not inherit selectors for an explicit source.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Radar<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 0.25, double strokeWidth = 2.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		fillBy = ResolveMapping(fillBy, panel.Context.Aesthetics.Fill, inherit, nameof(fillBy));

		panel.Context.CoordSystem = CoordSystem.Polar;

		panel.AddTyped(() =>
		{
			var geom = new Radar<T2, TX1, TY1>(source, x, y, fillBy, tooltip, scale)
			{
				Aesthetic = new()
				{
					Fill = fill,
					FillOpacity = fillOpacity,
					Stroke = fill,
					StrokeWidth = strokeWidth
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
	/// Adds a radar layer: a closed polygon per mapped fill over the angular categories. Implies polar coordinates.
	/// </summary>
	/// <param name="source">Items to plot; one vertex per category.</param>
	/// <param name="x">Angular category. Defaults to the plot's x selector only when no source is supplied; required with an explicit source.</param>
	/// <param name="y">Radial value, zero at the center. Defaults to the plot's y selector only when no source is supplied; required with an explicit source.</param>
	/// <param name="fillBy">Explicit data-driven fill mapping, overriding inherited Fill. Configure inherited mappings with <c>Scale_Fill_Discrete</c>/<c>Scale_Fill_Continuous</c>. Equal mapped fill colors share a polygon; varying colors can split one entity. Color mappings are not inherited.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hovered vertex when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill and stroke for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Resolve missing aesthetic mappings from the plot when this layer is added. Explicit mappings win. With an explicit source, an incompatible inherited row type throws; supply an explicit mapping or set <c>inherit: false</c>. Does not inherit selectors for an explicit source.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Radar<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 0.25, double strokeWidth = 2.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Radar(source, x, y, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, strokeWidth, scale, inherit: inherit);

		return context;
	}

	/// <summary>
	/// Adds a radar layer: a closed polygon per mapped fill over the angular categories. Implies polar coordinates.
	/// </summary>
	/// <param name="x">Angular category. Defaults to the plot's x selector only when no source is supplied; required with an explicit source.</param>
	/// <param name="y">Radial value, zero at the center. Defaults to the plot's y selector only when no source is supplied; required with an explicit source.</param>
	/// <param name="fillBy">Explicit data-driven fill mapping, overriding inherited Fill. Configure inherited mappings with <c>Scale_Fill_Discrete</c>/<c>Scale_Fill_Continuous</c>. Equal mapped fill colors share a polygon; varying colors can split one entity. Color mappings are not inherited.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hovered vertex when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill and stroke for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Resolve missing aesthetic mappings from the plot when this layer is added. Explicit mappings win. With an explicit source, an incompatible inherited row type throws; supply an explicit mapping or set <c>inherit: false</c>. Does not inherit selectors for an explicit source.</param>
	public static PanelFactory<T, TX, TY> Geom_Radar<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 0.25, double strokeWidth = 2.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Radar(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, strokeWidth, scale, inherit: inherit);
	}

	/// <summary>
	/// Adds a radar layer: a closed polygon per mapped fill over the angular categories. Implies polar coordinates.
	/// </summary>
	/// <param name="x">Angular category. Defaults to the plot's x selector only when no source is supplied; required with an explicit source.</param>
	/// <param name="y">Radial value, zero at the center. Defaults to the plot's y selector only when no source is supplied; required with an explicit source.</param>
	/// <param name="fillBy">Explicit data-driven fill mapping, overriding inherited Fill. Configure inherited mappings with <c>Scale_Fill_Discrete</c>/<c>Scale_Fill_Continuous</c>. Equal mapped fill colors share a polygon; varying colors can split one entity. Color mappings are not inherited.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip content per item, shown on hover at the hovered vertex when no explicit hover handlers are set.</param>
	/// <param name="fill">Constant fill and stroke for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Resolve missing aesthetic mappings from the plot when this layer is added. Explicit mappings win. With an explicit source, an incompatible inherited row type throws; supply an explicit mapping or set <c>inherit: false</c>. Does not inherit selectors for an explicit source.</param>
	public static PlotContext<T, TX, TY> Geom_Radar<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 0.25, double strokeWidth = 2.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Radar(x, y, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, strokeWidth, scale, inherit: inherit);

		return context;
	}
}

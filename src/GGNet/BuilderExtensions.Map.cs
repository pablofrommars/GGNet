namespace GGNet;

using Geoms.Map;
using Scales;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds a map layer: one filled multi-polygon per item (choropleths).
	/// </summary>
	/// <param name="source">Items to plot; one region per item.</param>
	/// <param name="polygons">The region’s polygons, in longitude/latitude data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip anchor point and content per item, shown on hover when no explicit hover handlers are set.</param>
	/// <param name="animation">Adds the <c>animate-map</c> css class.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="stroke">Border color.</param>
	/// <param name="strokeWidth">Border width in pixels; 0 draws no border.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, double, double> Geom_Map<T1, T2>(
	  this PanelFactory<T1, double, double> panel,
	  Source<T2> source,
	  Func<T2, Geospacial.Polygon[]> polygons,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double strokeWidth = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		panel.AddTyped(() =>
		{
			var geom = new Map<T2>(source, polygons, fillBy, tooltip, animation, scale)
			{
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout,
				Aesthetic = new()
				{
					Fill = fill,
					FillOpacity = fillOpacity,
					Stroke = stroke,
					StrokeWidth = strokeWidth
				}
			};

			return geom;
		});

		return panel;
	}

	/// <summary>
	/// Adds a map layer: one filled multi-polygon per item (choropleths).
	/// </summary>
	/// <param name="source">Items to plot; one region per item.</param>
	/// <param name="polygons">The region’s polygons, in longitude/latitude data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip anchor point and content per item, shown on hover when no explicit hover handlers are set.</param>
	/// <param name="animation">Adds the <c>animate-map</c> css class.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="stroke">Border color.</param>
	/// <param name="strokeWidth">Border width in pixels; 0 draws no border.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, double, double> Geom_Map<T1, T2>(
	  this PanelFactory<T1, double, double> panel,
	  IEnumerable<T2> source,
	  Func<T2, Geospacial.Polygon[]> polygons,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double strokeWidth = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		return Geom_Map(panel, new Source<T2>(source), polygons, fillBy, onclick, onmouseover, onmouseout, tooltip, animation, fill, fillOpacity, stroke, strokeWidth, scale);
	}

	/// <summary>
	/// Adds a map layer: one filled multi-polygon per item (choropleths).
	/// </summary>
	/// <param name="polygons">The region’s polygons, in longitude/latitude data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip anchor point and content per item, shown on hover when no explicit hover handlers are set.</param>
	/// <param name="animation">Adds the <c>animate-map</c> css class.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="stroke">Border color.</param>
	/// <param name="strokeWidth">Border width in pixels; 0 draws no border.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T, double, double> Geom_Map<T>(
	  this PanelFactory<T, double, double> panel,
	  Geospacial.Polygon[] polygons,
	  IAestheticMapping<Geospacial.Polygon[], string>? fillBy = null,
	  Func<Geospacial.Polygon[], MouseEventArgs, Task>? onclick = null,
	  Func<Geospacial.Polygon[], MouseEventArgs, Task>? onmouseover = null,
	  Func<Geospacial.Polygon[], MouseEventArgs, Task>? onmouseout = null,
	  Func<Geospacial.Polygon[], (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double strokeWidth = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		return Geom_Map(panel, [.. new[] { polygons }], o => o, fillBy, onclick, onmouseover, onmouseout, tooltip, animation, fill, fillOpacity, stroke, strokeWidth, scale);
	}

	/// <summary>
	/// Adds a map layer: one filled multi-polygon per item (choropleths).
	/// </summary>
	/// <param name="source">Items to plot; one region per item.</param>
	/// <param name="polygons">The region’s polygons, in longitude/latitude data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip anchor point and content per item, shown on hover when no explicit hover handlers are set.</param>
	/// <param name="animation">Adds the <c>animate-map</c> css class.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="stroke">Border color.</param>
	/// <param name="strokeWidth">Border width in pixels; 0 draws no border.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, double, double> Geom_Map<T1, T2>(
	  this PlotContext<T1, double, double> context,
	  Source<T2> source,
	  Func<T2, Geospacial.Polygon[]> polygons,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double strokeWidth = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		context.Default_Panel().Geom_Map(source, polygons, fillBy, onclick, onmouseover, onmouseout, tooltip, animation, fill, fillOpacity, stroke, strokeWidth, scale);

		return context;
	}

	/// <summary>
	/// Adds a map layer: one filled multi-polygon per item (choropleths).
	/// </summary>
	/// <param name="source">Items to plot; one region per item.</param>
	/// <param name="polygons">The region’s polygons, in longitude/latitude data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip anchor point and content per item, shown on hover when no explicit hover handlers are set.</param>
	/// <param name="animation">Adds the <c>animate-map</c> css class.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="stroke">Border color.</param>
	/// <param name="strokeWidth">Border width in pixels; 0 draws no border.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, double, double> Geom_Map<T1, T2>(
	  this PlotContext<T1, double, double> context,
	  IEnumerable<T2> source,
	  Func<T2, Geospacial.Polygon[]> polygons,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double strokeWidth = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		return Geom_Map(context, new Source<T2>(source), polygons, fillBy, onclick, onmouseover, onmouseout, tooltip, animation, fill, fillOpacity, stroke, strokeWidth, scale);
	}

	/// <summary>
	/// Adds a map layer: one filled multi-polygon per item (choropleths).
	/// </summary>
	/// <param name="polygons">The region’s polygons, in longitude/latitude data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip anchor point and content per item, shown on hover when no explicit hover handlers are set.</param>
	/// <param name="animation">Adds the <c>animate-map</c> css class.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="stroke">Border color.</param>
	/// <param name="strokeWidth">Border width in pixels; 0 draws no border.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T, double, double> Geom_Map<T>(
	  this PlotContext<T, double, double> context,
	  Geospacial.Polygon[] polygons,
	  IAestheticMapping<Geospacial.Polygon[], string>? fillBy = null,
	  Func<Geospacial.Polygon[], MouseEventArgs, Task>? onclick = null,
	  Func<Geospacial.Polygon[], MouseEventArgs, Task>? onmouseover = null,
	  Func<Geospacial.Polygon[], MouseEventArgs, Task>? onmouseout = null,
	  Func<Geospacial.Polygon[], (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double strokeWidth = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		return Geom_Map(context, new Source<Geospacial.Polygon[]>(new[] { polygons }), o => o, fillBy, onclick, onmouseover, onmouseout, tooltip, animation, fill, fillOpacity, stroke, strokeWidth, scale);
	}

	/// <summary>
	/// Adds a map layer: one filled multi-polygon per item (choropleths).
	/// </summary>
	/// <param name="polygons">The region’s polygons, in longitude/latitude data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip anchor point and content per item, shown on hover when no explicit hover handlers are set.</param>
	/// <param name="animation">Adds the <c>animate-map</c> css class.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="stroke">Border color.</param>
	/// <param name="strokeWidth">Border width in pixels; 0 draws no border.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T, double, double> Geom_Map<T>(
	  this PanelFactory<T, double, double> panel,
	  Func<T, Geospacial.Polygon[]> polygons,
	  IAestheticMapping<T, string>? fillBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double strokeWidth = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		return Geom_Map(panel, panel.Context.RequireSource(), polygons, fillBy ?? (inherit ? panel.Context.Aesthetics.Fill : null), onclick, onmouseover, onmouseout, tooltip, animation, fill, fillOpacity, stroke, strokeWidth, scale);
	}

	/// <summary>
	/// Adds a map layer: one filled multi-polygon per item (choropleths).
	/// </summary>
	/// <param name="polygons">The region’s polygons, in longitude/latitude data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="tooltip">Tooltip anchor point and content per item, shown on hover when no explicit hover handlers are set.</param>
	/// <param name="animation">Adds the <c>animate-map</c> css class.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="stroke">Border color.</param>
	/// <param name="strokeWidth">Border width in pixels; 0 draws no border.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T, double, double> Geom_Map<T>(
	  this PlotContext<T, double, double> context,
	  Func<T, Geospacial.Polygon[]> polygons,
	  IAestheticMapping<T, string>? fillBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double strokeWidth = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		context.Default_Panel().Geom_Map(polygons, fillBy, onclick, onmouseover, onmouseout, tooltip, animation, fill, fillOpacity, stroke, strokeWidth, scale);

		return context;
	}
}

namespace GGNet;

using Geoms.Map;
using Elements;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
	public static PanelFactory<T1, double, double> Geom_Map<T1, T2>(
	  this PanelFactory<T1, double, double> panel,
	  Source<T2> source,
	  Func<T2, Geospacial.Polygon[]> polygons,
	  IAestheticMapping<T2, string>? _fill = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double width = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		panel.AddTyped(() =>
		{
			var geom = new Map<T2>(source, polygons, _fill, tooltip, animation, scale)
			{
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout,
				Aesthetic = new()
				{
					Fill = fill,
					FillOpacity = fillOpacity,
					Stroke = stroke,
					StrokeWidth = width
				}
			};

			return geom;
		});

		return panel;
	}

	public static PanelFactory<T1, double, double> Geom_Map<T1, T2>(
	  this PanelFactory<T1, double, double> panel,
	  IEnumerable<T2> source,
	  Func<T2, Geospacial.Polygon[]> polygons,
	  IAestheticMapping<T2, string>? _fill = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double width = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		return Geom_Map(panel, new Source<T2>(source), polygons, _fill, onclick, onmouseover, onmouseout, tooltip, animation, fill, fillOpacity, stroke, width, scale);
	}

	public static PanelFactory<T, double, double> Geom_Map<T>(
	  this PanelFactory<T, double, double> panel,
	  Geospacial.Polygon[] polygons,
	  IAestheticMapping<Geospacial.Polygon[], string>? _fill = null,
	  Func<Geospacial.Polygon[], MouseEventArgs, Task>? onclick = null,
	  Func<Geospacial.Polygon[], MouseEventArgs, Task>? onmouseover = null,
	  Func<Geospacial.Polygon[], MouseEventArgs, Task>? onmouseout = null,
	  Func<Geospacial.Polygon[], (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double width = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		return Geom_Map(panel, [.. new[] { polygons }], o => o, _fill, onclick, onmouseover, onmouseout, tooltip, animation, fill, fillOpacity, stroke, width, scale);
	}

	public static PlotContext<T1, double, double> Geom_Map<T1, T2>(
	  this PlotContext<T1, double, double> context,
	  Source<T2> source,
	  Func<T2, Geospacial.Polygon[]> polygons,
	  IAestheticMapping<T2, string>? _fill = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double width = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		context.Default_Panel().Geom_Map(source, polygons, _fill, onclick, onmouseover, onmouseout, tooltip, animation, fill, fillOpacity, stroke, width, scale);

		return context;
	}

	public static PlotContext<T1, double, double> Geom_Map<T1, T2>(
	  this PlotContext<T1, double, double> context,
	  IEnumerable<T2> source,
	  Func<T2, Geospacial.Polygon[]> polygons,
	  IAestheticMapping<T2, string>? _fill = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double width = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		return Geom_Map(context, new Source<T2>(source), polygons, _fill, onclick, onmouseover, onmouseout, tooltip, animation, fill, fillOpacity, stroke, width, scale);
	}

	public static PlotContext<T, double, double> Geom_Map<T>(
	  this PlotContext<T, double, double> context,
	  Geospacial.Polygon[] polygons,
	  IAestheticMapping<Geospacial.Polygon[], string>? _fill = null,
	  Func<Geospacial.Polygon[], MouseEventArgs, Task>? onclick = null,
	  Func<Geospacial.Polygon[], MouseEventArgs, Task>? onmouseover = null,
	  Func<Geospacial.Polygon[], MouseEventArgs, Task>? onmouseout = null,
	  Func<Geospacial.Polygon[], (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double width = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		return Geom_Map(context, new Source<Geospacial.Polygon[]>(new[] { polygons }), o => o, _fill, onclick, onmouseover, onmouseout, tooltip, animation, fill, fillOpacity, stroke, width, scale);
	}

	public static PanelFactory<T, double, double> Geom_Map<T>(
	  this PanelFactory<T, double, double> panel,
	  Func<T, Geospacial.Polygon[]> polygons,
	  IAestheticMapping<T, string>? _fill = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double width = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		return Geom_Map(panel, panel.Context.RequireSource(), polygons, _fill ?? (inherit ? panel.Context.Aesthetics.Fill : null), onclick, onmouseover, onmouseout, tooltip, animation, fill, fillOpacity, stroke, width, scale);
	}

	public static PlotContext<T, double, double> Geom_Map<T>(
	  this PlotContext<T, double, double> context,
	  Func<T, Geospacial.Polygon[]> polygons,
	  IAestheticMapping<T, string>? _fill = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, (Geospacial.Point point, RenderFragment content)>? tooltip = null,
	  bool animation = false,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string stroke = "#000000", double width = 0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	{
		context.Default_Panel().Geom_Map(polygons, _fill, onclick, onmouseover, onmouseout, tooltip, animation, fill, fillOpacity, stroke, width, scale);

		return context;
	}
}

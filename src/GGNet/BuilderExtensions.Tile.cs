namespace GGNet;

using Geoms.Tile;
using Elements;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
	public static PanelFactory<T1, TX1, TY1> Geom_Tile<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1> x,
	  Func<T2, TY1> y,
	  Func<T2, double> width,
	  Func<T2, double> height,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		panel.AddTyped(() =>
		{
			var geom = new Tile<T2, TX1, TY1>(source, x, y, width, height, fillBy, tooltip, scale)
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

	public static PlotContext<T1, TX1, TY1> Geom_Tile<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  Source<T2> source,
	  Func<T2, TX1> x,
	  Func<T2, TY1> y,
	  Func<T2, double> width,
	  Func<T2, double> height,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Tile(source, x, y, width, height, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, strokeColor, strokeOpacity, strokeWidth, scale);

		return context;
	}

	public static PlotContext<T1, TX1, TY1> Geom_Tile<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IEnumerable<T2> source,
	  Func<T2, TX1> x,
	  Func<T2, TY1> y,
	  Func<T2, double> width,
	  Func<T2, double> height,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	  => context.Geom_Tile(new Source<T2>(source), x, y, width, height, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, strokeColor, strokeOpacity, strokeWidth, scale);

	public static PanelFactory<T, TX, TY> Geom_Tile<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX> x,
	  Func<T, TY> y,
	  Func<T, double> width,
	  Func<T, double> height,
	  IAestheticMapping<T, string>? fillBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Tile(panel, panel.Context.RequireSource(), (x ?? panel.Context.Selectors.X)!, (y ?? panel.Context.Selectors.Y)!, width, height, fillBy ?? (inherit ? panel.Context.Aesthetics.Fill : null), onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, strokeColor, strokeOpacity, strokeWidth, scale);
	}

	public static PlotContext<T, TX, TY> Geom_Tile<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX> x,
	  Func<T, TY> y,
	  Func<T, double> width,
	  Func<T, double> height,
	  IAestheticMapping<T, string>? fillBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Tile(x, y, width, height, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, strokeColor, strokeOpacity, strokeWidth, scale);

		return context;
	}
}

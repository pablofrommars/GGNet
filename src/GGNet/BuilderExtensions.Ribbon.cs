namespace GGNet;

using Geoms.Ribbon;
using Elements;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
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

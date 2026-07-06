namespace GGNet;

using Geoms.Hex;
using Elements;
using Exceptions;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
	public static PanelFactory<T1, TX1, TY1> Geom_Hex<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, TX1>? dx = null,
	  Func<T2, TY1>? dy = null,
	  IAestheticMapping<T2, string>? _fill = null,
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
			var geom = new Hex<T2, TX1, TY1>(source, x, y, dx, dy, _fill, tooltip, animation, scale)
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

	public static PanelFactory<T1, TX1, TY1> Geom_Hex<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, TX1>? dx = null,
	  Func<T2, TY1>? dy = null,
	  IAestheticMapping<T2, string>? _fill = null,
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
		return panel.Geom_Hex(new Source<T2>(source), x, y, dx, dy, _fill, onclick, onmouseover, onmouseout, tooltip, fill, opacity, animation, scale);
	}

	public static PlotContext<T1, TX1, TY1> Geom_Hex<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, TX1>? dx = null,
	  Func<T2, TY1>? dy = null,
	  IAestheticMapping<T2, string>? _fill = null,
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
		context.Default_Panel().Geom_Hex(source, x, y, dx, dy, _fill, onclick, onmouseover, onmouseout, tooltip, fill, opacity, animation, scale);

		return context;
	}

	public static PlotContext<T1, TX1, TY1> Geom_Hex<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, TX1>? dx = null,
	  Func<T2, TY1>? dy = null,
	  IAestheticMapping<T2, string>? _fill = null,
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
		return context.Geom_Hex(new Source<T2>(source), x, y, dx, dy, _fill, onclick, onmouseover, onmouseout, tooltip, fill, opacity, animation, scale);
	}

	public static PanelFactory<T, TX, TY> Geom_Hex<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, TX>? dx = null,
	  Func<T, TY>? dy = null,
	  IAestheticMapping<T, string>? _fill = null,
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
		return Geom_Hex(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, dx, dy, _fill ?? (inherit ? panel.Context.Aesthetics.Fill : null), onclick, onmouseover, onmouseout, tooltip, fill, opacity, animation, scale);
	}

	public static PlotContext<T, TX, TY> Geom_Hex<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, TX>? dx = null,
	  Func<T, TY>? dy = null,
	  IAestheticMapping<T, string>? _fill = null,
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
		context.Default_Panel().Geom_Hex(x, y, dx, dy, _fill, onclick, onmouseover, onmouseout, tooltip, fill, opacity, animation, scale);

		return context;
	}
}

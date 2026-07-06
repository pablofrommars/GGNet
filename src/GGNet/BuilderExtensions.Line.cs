namespace GGNet;

using Geoms.Line;
using Elements;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
	public static PanelFactory<T1, TX1, TY1> Geom_Line<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, string>? _color = null,
	  IAestheticMapping<T2, LineType>? _lineType = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  (bool x, bool y)? scale = null, bool inherit = true, bool piecewise = false)
	  where TX1 : struct
	  where TY1 : struct
	{
		panel.AddTyped(() =>
		{
			var geom = new Line<T2, TX1, TY1>(source, x, y, _color, _lineType, tooltip, scale, piecewise)
			{
				Aesthetic = new()
				{
					Stroke = color,
					StrokeOpacity = opacity,
					StrokeWidth = strokeWidth,
					LineType = lineType
				},
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout
			};

			return geom;
		});

		return panel;
	}

	public static PlotContext<T1, TX1, TY1> Geom_Line<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, string>? _color = null,
	  IAestheticMapping<T2, LineType>? _lineType = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  (bool x, bool y)? scale = null, bool inherit = true, bool piecewise = false)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Line(source, x, y, _color, _lineType, onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType, scale, piecewise);

		return context;
	}

	public static PanelFactory<T, TX, TY> Geom_Line<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  IAestheticMapping<T, string>? _color = null,
	  IAestheticMapping<T, LineType>? _lineType = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  (bool x, bool y)? scale = null, bool inherit = true, bool piecewise = false)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Line(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, _color ?? (inherit ? panel.Context.Aesthetics.Color : null), _lineType ?? (inherit ? panel.Context.Aesthetics.LineType : null), onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType, scale, piecewise);
	}

	public static PlotContext<T, TX, TY> Geom_Line<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  IAestheticMapping<T, string>? _color = null,
	  IAestheticMapping<T, LineType>? _lineType = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  (bool x, bool y)? scale = null, bool inherit = true, bool piecewise = false)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Line(x, y, _color, _lineType, onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType, scale, piecewise);

		return context;
	}
}

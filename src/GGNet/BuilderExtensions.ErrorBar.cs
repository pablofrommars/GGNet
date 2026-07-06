namespace GGNet;

using Geoms.ErrorBar;
using Elements;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
	public static PanelFactory<T1, TX1, TY1> Geom_ErrorBar<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, TY1>? ymin = null,
	  Func<T2, TY1>? ymax = null,
	  IAestheticMapping<T2, string>? _color = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  double radius = 5,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		panel.AddTyped(() =>
		{
			var geom = new ErrorBar<T2, TX1, TY1>(source, x, y, ymin, ymax, _color, tooltip, position, animation, scale)
			{
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout,
				Line = new()
				{
					StrokeWidth = strokeWidth,
					Stroke = color,
					StrokeOpacity = opacity,
					LineType = lineType
				},
				Circle = new()
				{
					Fill = color,
					FillOpacity = opacity,
					Radius = radius
				}
			};

			return geom;
		});

		return panel;
	}

	public static PanelFactory<T1, TX1, TY1> Geom_ErrorBar<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, TY1>? ymin = null,
	  Func<T2, TY1>? ymax = null,
	  IAestheticMapping<T2, string>? _color = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  double radius = 5,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return panel.Geom_ErrorBar(new Source<T2>(source), x, y, ymin, ymax, _color, onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType, radius, position, animation, scale);
	}

	public static PlotContext<T1, TX1, TY1> Geom_ErrorBar<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, TY1>? ymin = null,
	  Func<T2, TY1>? ymax = null,
	  IAestheticMapping<T2, string>? _color = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  double radius = 5,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_ErrorBar(source, x, y, ymin, ymax, _color, onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType, radius, position, animation, scale);

		return context;
	}

	public static PlotContext<T1, TX1, TY1> Geom_ErrorBar<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, TY1>? ymin = null,
	  Func<T2, TY1>? ymax = null,
	  IAestheticMapping<T2, string>? _color = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  double radius = 5,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return context.Geom_ErrorBar(new Source<T2>(source), x, y, ymin, ymax, _color, onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType, radius, position, animation, scale);
	}

	public static PanelFactory<T, TX, TY> Geom_ErrorBar<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, TY>? ymin = null,
	  Func<T, TY>? ymax = null,
	  IAestheticMapping<T, string>? _color = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  double radius = 5,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		return Geom_ErrorBar(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, ymin ?? panel.Context.Selectors.Y, ymax ?? panel.Context.Selectors.Y, _color ?? (inherit ? panel.Context.Aesthetics.Color : null), onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType, radius, position, animation, scale);
	}

	public static PlotContext<T, TX, TY> Geom_ErrorBar<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, TY>? ymin = null,
	  Func<T, TY>? ymax = null,
	  IAestheticMapping<T, string>? _color = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  double radius = 5,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_ErrorBar(x, y, ymin, ymax, _color, onclick, onmouseover, onmouseout, tooltip, strokeWidth, color, opacity, lineType, radius, position, animation, scale);

		return context;
	}
}

namespace GGNet;

using Geoms.Bar;
using Elements;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
	public static PanelFactory<T1, TX1, TY1> Geom_Bar<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
	  PositionAdjustment position = PositionAdjustment.Stack,
	  double width = 0.9,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		panel.AddTyped(() =>
		{
			var geom = new Bar<T2, TX1, TY1>(source, x, y, fillBy, tooltip, position, width, animation, scale)
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

	public static PlotContext<T1, TX1, TY1> Geom_Bar<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  Func<T2, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
	  PositionAdjustment position = PositionAdjustment.Stack,
	  double width = 0.9,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Bar(source, x, y, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, strokeColor, strokeOpacity, strokeWidth, position, width, animation, scale);

		return context;
	}

	public static PanelFactory<T, TX, TY> Geom_Bar<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
	  PositionAdjustment position = PositionAdjustment.Stack,
	  double width = 0.9,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Bar(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, fillBy ?? (inherit ? panel.Context.Aesthetics.Fill : null), onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, strokeColor, strokeOpacity, strokeWidth, position, width, animation, scale);
	}

	public static PlotContext<T, TX, TY> Geom_Bar<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  Func<T, RenderFragment>? tooltip = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  string strokeColor = "inherit", double strokeOpacity = 1.0, double strokeWidth = 0.0,
	  PositionAdjustment position = PositionAdjustment.Stack,
	  double width = 0.9,
	  bool animation = false,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Bar(x, y, fillBy, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, strokeColor, strokeOpacity, strokeWidth, position, width, animation, scale);

		return context;
	}
}

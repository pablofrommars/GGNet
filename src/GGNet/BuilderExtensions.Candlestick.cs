namespace GGNet;

using Geoms.Candlestick;
using Elements;
using Exceptions;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
	public static PanelFactory<T1, TX1, TY1> Geom_Candlestick<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? open = null,
	  Func<T2, TY1>? high = null,
	  Func<T2, TY1>? low = null,
	  Func<T2, TY1>? close = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)
	  where TX1 : struct
	  where TY1 : struct
	{
		if (open is null)
		{
			throw new GGNetUserException($"{nameof(open)} selector should not be null");
		}

		if (high is null)
		{
			throw new GGNetUserException($"{nameof(high)} selector should not be null");
		}

		if (low is null)
		{
			throw new GGNetUserException($"{nameof(low)} selector should not be null");
		}

		if (close is null)
		{
			throw new GGNetUserException($"{nameof(close)} selector should not be null");
		}

		panel.AddTyped(() =>
		{
			var geom = new Candlestick<T2, TX1, TY1>(source, x, open, high, low, close)
			{
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout,
				Line = new()
				{
					Stroke = color,
					StrokeOpacity = opacity,
					StrokeWidth = strokeWidth,
					LineType = lineType
				},
				Rectangle = new()
				{
					Fill = color,
					FillOpacity = opacity
				}
			};

			return geom;
		});

		return panel;
	}

	public static PlotContext<T1, TX1, TY1> Geom_Candlestick<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? open = null,
	  Func<T2, TY1>? high = null,
	  Func<T2, TY1>? low = null,
	  Func<T2, TY1>? close = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Candlestick(source, x, open, high, low, close, onclick, onmouseover, onmouseout, strokeWidth, color, opacity, lineType);

		return context;
	}

	public static PanelFactory<T, TX, TY> Geom_Candlestick<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? open = null,
	  Func<T, TY>? high = null,
	  Func<T, TY>? low = null,
	  Func<T, TY>? close = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Candlestick(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, open, high, low, close, onclick, onmouseover, onmouseout, strokeWidth, color, opacity, lineType);
	}

	public static PlotContext<T, TX, TY> Geom_Candlestick<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? open = null,
	  Func<T, TY>? high = null,
	  Func<T, TY>? low = null,
	  Func<T, TY>? close = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Candlestick(x, open, high, low, close, onclick, onmouseover, onmouseout, strokeWidth, color, opacity, lineType);

		return context;
	}
}

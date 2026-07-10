namespace GGNet;

using Geoms.OHLC;
using Exceptions;
using static LineType;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds an OHLC layer: a high–low bar with open and close ticks per x.
	/// </summary>
	/// <param name="source">Items to plot; one bar per item.</param>
	/// <param name="x">Bar position, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="open">Opening value, in y-axis data units.</param>
	/// <param name="high">High value, in y-axis data units.</param>
	/// <param name="low">Low value, in y-axis data units.</param>
	/// <param name="close">Closing value, in y-axis data units.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke color for the layer.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_OHLC<T1, TX1, TY1, T2>(
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
			var geom = new OHLC<T2, TX1, TY1>(source, x, open, high, low, close)
			{
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout,
				Aesthetic = new()
				{
					Stroke = color,
					StrokeOpacity = opacity,
					StrokeWidth = strokeWidth,
					LineType = lineType

				}
			};

			return geom;
		});

		return panel;
	}

	/// <summary>
	/// Adds an OHLC layer: a high–low bar with open and close ticks per x.
	/// </summary>
	/// <param name="source">Items to plot; one bar per item.</param>
	/// <param name="x">Bar position, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="open">Opening value, in y-axis data units.</param>
	/// <param name="high">High value, in y-axis data units.</param>
	/// <param name="low">Low value, in y-axis data units.</param>
	/// <param name="close">Closing value, in y-axis data units.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke color for the layer.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	public static PlotContext<T1, TX1, TY1> Geom_OHLC<T1, TX1, TY1, T2>(
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
		context.Default_Panel().Geom_OHLC(source, x, open, high, low, close, onclick, onmouseover, onmouseout, strokeWidth, color, opacity, lineType);

		return context;
	}

	/// <summary>
	/// Adds an OHLC layer: a high–low bar with open and close ticks per x.
	/// </summary>
	/// <param name="x">Bar position, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="open">Opening value, in y-axis data units.</param>
	/// <param name="high">High value, in y-axis data units.</param>
	/// <param name="low">Low value, in y-axis data units.</param>
	/// <param name="close">Closing value, in y-axis data units.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke color for the layer.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	public static PanelFactory<T, TX, TY> Geom_OHLC<T, TX, TY>(
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
		return Geom_OHLC(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, open, high, low, close, onclick, onmouseover, onmouseout, strokeWidth, color, opacity, lineType);
	}

	/// <summary>
	/// Adds an OHLC layer: a high–low bar with open and close ticks per x.
	/// </summary>
	/// <param name="x">Bar position, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="open">Opening value, in y-axis data units.</param>
	/// <param name="high">High value, in y-axis data units.</param>
	/// <param name="low">Low value, in y-axis data units.</param>
	/// <param name="close">Closing value, in y-axis data units.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke color for the layer.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	public static PlotContext<T, TX, TY> Geom_OHLC<T, TX, TY>(
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
		context.Default_Panel().Geom_OHLC(x, open, high, low, close, onclick, onmouseover, onmouseout, strokeWidth, color, opacity, lineType);

		return context;
	}
}

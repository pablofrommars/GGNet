namespace GGNet;

using Geoms.Volume;
using Exceptions;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds a volume layer: one bar from zero per x.
	/// </summary>
	/// <param name="source">Items to plot; one bar per item.</param>
	/// <param name="x">Bar position, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="volume">Bar height, in y-axis data units. Required.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="fill">Constant fill for the layer.</param>
	/// <param name="opacity">Fill opacity, 0–1.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Volume<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? volume = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  string fill = "#23d0fc", double opacity = 1.0)
	  where TX1 : struct
	  where TY1 : struct
	{
		if (volume is null)
		{
			throw new GGNetUserException($"{nameof(volume)} selector should not be null");
		}

		panel.AddTyped(() =>
		{
			var geom = new Volume<T2, TX1, TY1>(source, x, volume)
			{
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout,
				Aesthetic = new()
				{
					Fill = fill,
					FillOpacity = opacity
				}
			};

			return geom;
		});

		return panel;
	}

	/// <summary>
	/// Adds a volume layer: one bar from zero per x.
	/// </summary>
	/// <param name="source">Items to plot; one bar per item.</param>
	/// <param name="x">Bar position, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="volume">Bar height, in y-axis data units. Required.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="fill">Constant fill for the layer.</param>
	/// <param name="opacity">Fill opacity, 0–1.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Volume<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? volume = null,
	  Func<T2, MouseEventArgs, Task>? onclick = null,
	  Func<T2, MouseEventArgs, Task>? onmouseover = null,
	  Func<T2, MouseEventArgs, Task>? onmouseout = null,
	  string fill = "#23d0fc", double opacity = 1.0)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Volume(source, x, volume, onclick, onmouseover, onmouseout, fill, opacity);

		return context;
	}

	/// <summary>
	/// Adds a volume layer: one bar from zero per x.
	/// </summary>
	/// <param name="x">Bar position, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="volume">Bar height, in y-axis data units. Required.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="fill">Constant fill for the layer.</param>
	/// <param name="opacity">Fill opacity, 0–1.</param>
	public static PanelFactory<T, TX, TY> Geom_Volume<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? volume = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  string fill = "#23d0fc", double opacity = 1.0)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Volume(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, volume, onclick, onmouseover, onmouseout, fill, opacity);
	}

	/// <summary>
	/// Adds a volume layer: one bar from zero per x.
	/// </summary>
	/// <param name="x">Bar position, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="volume">Bar height, in y-axis data units. Required.</param>
	/// <param name="onclick">Per-item click handler.</param>
	/// <param name="onmouseover">Per-item hover handler; setting it disables the default tooltip hover.</param>
	/// <param name="onmouseout">Per-item hover-end handler.</param>
	/// <param name="fill">Constant fill for the layer.</param>
	/// <param name="opacity">Fill opacity, 0–1.</param>
	public static PlotContext<T, TX, TY> Geom_Volume<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? volume = null,
	  Func<T, MouseEventArgs, Task>? onclick = null,
	  Func<T, MouseEventArgs, Task>? onmouseover = null,
	  Func<T, MouseEventArgs, Task>? onmouseout = null,
	  string fill = "#23d0fc", double opacity = 1.0)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Volume(x, volume, onclick, onmouseover, onmouseout, fill, opacity);

		return context;
	}
}

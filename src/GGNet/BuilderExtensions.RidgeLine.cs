namespace GGNet;

using Geoms.RidgeLine;
using Elements;
using Exceptions;
using Scales;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds a ridgeline layer: an area of the given height along x, drawn per y row.
	/// </summary>
	/// <param name="source">Items to plot; one profile vertex per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Row baseline, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="height">Profile height above the baseline at x, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_RidgeLine<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? height = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		if (height is null)
		{
			throw new GGNetUserException($"{nameof(height)} selector should not be null");
		}

		panel.AddTyped(() =>
		{
			var geom = new RidgeLine<T2, TX1, TY1>(source, x, y, height, fillBy, scale)
			{
				Aesthetic = new Rectangle
				{
					Fill = fill,
					FillOpacity = fillOpacity
				}
			};

			return geom;
		});

		return panel;
	}

	/// <summary>
	/// Adds a ridgeline layer: an area of the given height along x, drawn per y row.
	/// </summary>
	/// <param name="source">Items to plot; one profile vertex per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Row baseline, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="height">Profile height above the baseline at x, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_RidgeLine<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? height = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return panel.Geom_RidgeLine(new Source<T2>(source), x, y, height, fillBy, fill, fillOpacity, scale);
	}

	/// <summary>
	/// Adds a ridgeline layer: an area of the given height along x, drawn per y row.
	/// </summary>
	/// <param name="source">Items to plot; one profile vertex per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Row baseline, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="height">Profile height above the baseline at x, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, TX1, TY1> Geom_RidgeLine<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? height = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_RidgeLine(source, x, y, height, fillBy, fill, fillOpacity, scale);

		return context;
	}

	/// <summary>
	/// Adds a ridgeline layer: an area of the given height along x, drawn per y row.
	/// </summary>
	/// <param name="source">Items to plot; one profile vertex per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Row baseline, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="height">Profile height above the baseline at x, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, TX1, TY1> Geom_RidgeLine<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? height = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return context.Geom_RidgeLine(new Source<T2>(source), x, y, height, fillBy, fill, fillOpacity, scale);
	}

	/// <summary>
	/// Adds a ridgeline layer: an area of the given height along x, drawn per y row.
	/// </summary>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Row baseline, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="height">Profile height above the baseline at x, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T, TX, TY> Geom_RidgeLine<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, double>? height = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		return Geom_RidgeLine(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, height, fillBy ?? (inherit ? panel.Context.Aesthetics.Fill : null), fill, fillOpacity, scale);
	}

	/// <summary>
	/// Adds a ridgeline layer: an area of the given height along x, drawn per y row.
	/// </summary>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Row baseline, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="height">Profile height above the baseline at x, in y-axis data units.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T, TX, TY> Geom_RidgeLine<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, double>? height = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_RidgeLine(x, y, height, fillBy, fill, fillOpacity, scale);

		return context;
	}
}

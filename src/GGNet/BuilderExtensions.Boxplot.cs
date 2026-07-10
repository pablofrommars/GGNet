namespace GGNet;

using Geoms.Boxplot;
using Scales;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds a boxplot layer. Horizontal by data design: x carries the measurements, y the category.
	/// </summary>
	/// <param name="source">Items to aggregate; quartiles and whiskers are computed per (y, fill) group.</param>
	/// <param name="x">Measurement, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Category position, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="size">Box breadth in y-axis data units, as a fraction of the smallest category spacing.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Boxplot<T1, TX1, TY1, T2>(
	this PanelFactory<T1, TX1, TY1> panel,
	IReadOnlyList<T2> source,
	Func<T2, TX1>? x = null,
	Func<T2, TY1>? y = null,
	IAestheticMapping<T2, string>? fillBy = null,
	double size = 0.8,
	string fill = "#23d0fc", double fillOpacity = 1.0, double strokeWidth = 2.0,
	(bool x, bool y)? scale = null, bool inherit = true)
	where TX1 : struct
	where TY1 : struct
	{
		panel.AddTyped(() =>
		{
			var geom = new Boxplot<T2, TX1, TY1>(source, x, y, fillBy, size, scale)
			{
				Aesthetic = new()
				{
					Fill = fill,
					FillOpacity = fillOpacity,
					StrokeWidth = strokeWidth
				}
			};

			return geom;
		});

		return panel;
	}

	/// <summary>
	/// Adds a boxplot layer. Horizontal by data design: x carries the measurements, y the category.
	/// </summary>
	/// <param name="source">Items to aggregate; quartiles and whiskers are computed per (y, fill) group.</param>
	/// <param name="x">Measurement, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Category position, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="size">Box breadth in y-axis data units, as a fraction of the smallest category spacing.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Boxplot<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  double size = 0.8,
	  string fill = "#23d0fc", double fillOpacity = 1.0, double strokeWidth = 2.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Boxplot(source, x, y, fillBy, size, fill, fillOpacity, strokeWidth, scale);

		return context;
	}

	/// <summary>
	/// Adds a boxplot layer. Horizontal by data design: x carries the measurements, y the category.
	/// </summary>
	/// <param name="x">Measurement, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Category position, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="size">Box breadth in y-axis data units, as a fraction of the smallest category spacing.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T, TX, TY> Geom_Boxplot<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  double size = 0.8,
	  string fill = "#23d0fc", double fillOpacity = 1.0, double strokeWidth = 2.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Boxplot(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, fillBy ?? (inherit ? panel.Context.Aesthetics.Fill : null), size, fill, fillOpacity, strokeWidth, scale);
	}

	/// <summary>
	/// Adds a boxplot layer. Horizontal by data design: x carries the measurements, y the category.
	/// </summary>
	/// <param name="x">Measurement, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Category position, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="size">Box breadth in y-axis data units, as a fraction of the smallest category spacing.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T, TX, TY> Geom_Boxplot<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  double size = 0.8,
	  string fill = "#23d0fc", double fillOpacity = 1.0, double strokeWidth = 2.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Boxplot(x, y, fillBy, size, fill, fillOpacity, strokeWidth, scale);

		return context;
	}
}

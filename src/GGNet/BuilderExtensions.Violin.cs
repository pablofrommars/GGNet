namespace GGNet;

using Geoms.Violin;
using Exceptions;
using Scales;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds a violin layer from a precomputed density profile: width is the density at each y.
	/// </summary>
	/// <param name="source">Profile points; one violin outline vertex per item.</param>
	/// <param name="x">Violin center, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Profile position, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="width">Density at y, in x-axis data units — the violin’s half-profile. Required.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="stroke">Outline color; null draws no outline.</param>
	/// <param name="position"><c>Dodge</c> separates violins sharing a center; <c>Identity</c> overlays them.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Violin<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? width = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string? stroke = null,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		if (width is null)
		{
			throw new GGNetUserException($"{nameof(width)} selector should not be null");
		}

		panel.AddTyped(() =>
		{
			var geom = new Violin<T2, TX1, TY1>(source, x, y, width, fillBy, position, scale)
			{
				Aesthetic = new()
				{
					Fill = fill,
					FillOpacity = fillOpacity,
					Stroke = string.IsNullOrEmpty(stroke) ? "inherit" : stroke,
					StrokeWidth = string.IsNullOrEmpty(stroke) ? 0.0 : 0.3
				}
			};

			return geom;
		});

		return panel;
	}

	/// <summary>
	/// Adds a violin layer from a precomputed density profile: width is the density at each y.
	/// </summary>
	/// <param name="source">Profile points; one violin outline vertex per item.</param>
	/// <param name="x">Violin center, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Profile position, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="width">Density at y, in x-axis data units — the violin’s half-profile. Required.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="stroke">Outline color; null draws no outline.</param>
	/// <param name="position"><c>Dodge</c> separates violins sharing a center; <c>Identity</c> overlays them.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Violin<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? width = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string? stroke = null,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return panel.Geom_Violin(new Source<T2>(source), x, y, width, fillBy, fill, fillOpacity, stroke, position, scale);
	}

	/// <summary>
	/// Adds a violin layer from a precomputed density profile: width is the density at each y.
	/// </summary>
	/// <param name="source">Profile points; one violin outline vertex per item.</param>
	/// <param name="x">Violin center, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Profile position, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="width">Density at y, in x-axis data units — the violin’s half-profile. Required.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="stroke">Outline color; null draws no outline.</param>
	/// <param name="position"><c>Dodge</c> separates violins sharing a center; <c>Identity</c> overlays them.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Violin<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? width = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string? stroke = null,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Violin(source, x, y, width, fillBy, fill, fillOpacity, stroke, position, scale);

		return context;
	}

	/// <summary>
	/// Adds a violin layer from a precomputed density profile: width is the density at each y.
	/// </summary>
	/// <param name="source">Profile points; one violin outline vertex per item.</param>
	/// <param name="x">Violin center, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Profile position, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="width">Density at y, in x-axis data units — the violin’s half-profile. Required.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="stroke">Outline color; null draws no outline.</param>
	/// <param name="position"><c>Dodge</c> separates violins sharing a center; <c>Identity</c> overlays them.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Violin<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? width = null,
	  IAestheticMapping<T2, string>? fillBy = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string? stroke = null,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return context.Geom_Violin(new Source<T2>(source), x, y, width, fillBy, fill, fillOpacity, stroke, position, scale);
	}

	/// <summary>
	/// Adds a violin layer from a precomputed density profile: width is the density at each y.
	/// </summary>
	/// <param name="x">Violin center, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Profile position, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="width">Density at y, in x-axis data units — the violin’s half-profile. Required.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="stroke">Outline color; null draws no outline.</param>
	/// <param name="position"><c>Dodge</c> separates violins sharing a center; <c>Identity</c> overlays them.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T, TX, TY> Geom_Violin<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, double>? width = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string? stroke = null,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Violin(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, width, fillBy ?? (inherit ? panel.Context.Aesthetics.Fill : null), fill, fillOpacity, stroke, position, scale);
	}

	/// <summary>
	/// Adds a violin layer from a precomputed density profile: width is the density at each y.
	/// </summary>
	/// <param name="x">Violin center, in x-axis data units. Defaults to the plot’s x selector.</param>
	/// <param name="y">Profile position, in y-axis data units. Defaults to the plot’s y selector.</param>
	/// <param name="width">Density at y, in x-axis data units — the violin’s half-profile. Required.</param>
	/// <param name="fillBy">Data-driven fill: participates in the fill scale and the legend. Build with <c>Scale_Fill_Discrete</c>/<c>_Continuous</c>.</param>
	/// <param name="fill">Constant fill for the whole layer; with <paramref name="fillBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="fillOpacity">Fill opacity, 0–1.</param>
	/// <param name="stroke">Outline color; null draws no outline.</param>
	/// <param name="position"><c>Dodge</c> separates violins sharing a center; <c>Identity</c> overlays them.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T, TX, TY> Geom_Violin<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, double>? width = null,
	  IAestheticMapping<T, string>? fillBy = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string? stroke = null,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Violin(x, y, width, fillBy, fill, fillOpacity, stroke, position, scale);

		return context;
	}
}

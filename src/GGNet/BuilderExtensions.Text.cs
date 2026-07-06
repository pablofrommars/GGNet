namespace GGNet;

using Geoms.Text;
using Scales;

using static Anchor;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds a text layer: one label per item.
	/// </summary>
	/// <param name="source">Items to plot; one label per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Position along y, in y-axis data units. Defaults to the plot's y selector.</param>
	/// <param name="angleBy">Per-item rotation in degrees. Data-driven but scale-less: no legend.</param>
	/// <param name="text">Label content per item; formatted with the invariant culture.</param>
	/// <param name="colorBy">Data-driven color: participates in the color scale and the legend. Build with <c>Scale_Color_Discrete</c>.</param>
	/// <param name="size">Label font size; null takes the theme’s.</param>
	/// <param name="anchor">Text anchor relative to (x, y).</param>
	/// <param name="weight">Label font weight (<c>"normal"</c>, <c>"bold"</c>).</param>
	/// <param name="style">Label font style (<c>"normal"</c>, <c>"italic"</c>).</param>
	/// <param name="color">Constant text color for the whole layer; with <paramref name="colorBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="angle">Constant rotation in degrees.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T1, TX1, TY1> Geom_Text<T1, TX1, TY1, T2, TT>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? angleBy = null,
	  Func<T2, TT>? text = null,
	  IAestheticMapping<T2, string>? colorBy = null,
	  Size? size = null, Anchor anchor = Middle, string weight = "normal", string style = "normal", string color = "#23d0fc", double angle = 0.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		panel.AddTyped(() =>
		{
			var geom = new Text<T2, TX1, TY1, TT>(source, x, y, angleBy, text, colorBy, scale)
			{
				Aesthetic = new()
				{
					Anchor = anchor,
					FontSize = size ?? 1,
					FontWeight = weight,
					FontStyle = style,
					Color = color,
					Angle = angle
				}
			};

			return geom;
		});

		return panel;
	}

	/// <summary>
	/// Adds a text layer: one label per item.
	/// </summary>
	/// <param name="source">Items to plot; one label per item.</param>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Position along y, in y-axis data units. Defaults to the plot's y selector.</param>
	/// <param name="angleBy">Per-item rotation in degrees. Data-driven but scale-less: no legend.</param>
	/// <param name="text">Label content per item; formatted with the invariant culture.</param>
	/// <param name="colorBy">Data-driven color: participates in the color scale and the legend. Build with <c>Scale_Color_Discrete</c>.</param>
	/// <param name="size">Label font size; null takes the theme’s.</param>
	/// <param name="anchor">Text anchor relative to (x, y).</param>
	/// <param name="weight">Label font weight (<c>"normal"</c>, <c>"bold"</c>).</param>
	/// <param name="style">Label font style (<c>"normal"</c>, <c>"italic"</c>).</param>
	/// <param name="color">Constant text color for the whole layer; with <paramref name="colorBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="angle">Constant rotation in degrees.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T1, TX1, TY1> Geom_Text<T1, TX1, TY1, T2, TT>(
	  this PlotContext<T1, TX1, TY1> context,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? angleBy = null,
	  Func<T2, TT>? text = null,
	  IAestheticMapping<T2, string>? colorBy = null,
	  Size? size = null, Anchor anchor = Middle, string weight = "normal", string style = "normal", string color = "#23d0fc", double angle = 0.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Text(source, x, y, angleBy, text, colorBy, size, anchor, weight, style, color, angle, scale);

		return context;
	}

	/// <summary>
	/// Adds a text layer: one label per item.
	/// </summary>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Position along y, in y-axis data units. Defaults to the plot's y selector.</param>
	/// <param name="angleBy">Per-item rotation in degrees. Data-driven but scale-less: no legend.</param>
	/// <param name="text">Label content per item; formatted with the invariant culture.</param>
	/// <param name="colorBy">Data-driven color: participates in the color scale and the legend. Build with <c>Scale_Color_Discrete</c>.</param>
	/// <param name="size">Label font size; null takes the theme’s.</param>
	/// <param name="anchor">Text anchor relative to (x, y).</param>
	/// <param name="weight">Label font weight (<c>"normal"</c>, <c>"bold"</c>).</param>
	/// <param name="style">Label font style (<c>"normal"</c>, <c>"italic"</c>).</param>
	/// <param name="color">Constant text color for the whole layer; with <paramref name="colorBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="angle">Constant rotation in degrees.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PanelFactory<T, TX, TY> Geom_Text<T, TX, TY, TT>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, double>? angleBy = null,
	  Func<T, TT>? text = null,
	  IAestheticMapping<T, string>? colorBy = null,
	  Size? size = null, Anchor anchor = Middle, string weight = "normal", string style = "normal", string color = "#23d0fc", double angle = 0.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Text(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, angleBy, text, colorBy ?? (inherit ? panel.Context.Aesthetics.Color : null), size, anchor, weight, style, color, angle, scale);
	}

	/// <summary>
	/// Adds a text layer: one label per item.
	/// </summary>
	/// <param name="x">Position along x, in x-axis data units. Defaults to the plot's x selector.</param>
	/// <param name="y">Position along y, in y-axis data units. Defaults to the plot's y selector.</param>
	/// <param name="angleBy">Per-item rotation in degrees. Data-driven but scale-less: no legend.</param>
	/// <param name="text">Label content per item; formatted with the invariant culture.</param>
	/// <param name="colorBy">Data-driven color: participates in the color scale and the legend. Build with <c>Scale_Color_Discrete</c>.</param>
	/// <param name="size">Label font size; null takes the theme’s.</param>
	/// <param name="anchor">Text anchor relative to (x, y).</param>
	/// <param name="weight">Label font weight (<c>"normal"</c>, <c>"bold"</c>).</param>
	/// <param name="style">Label font style (<c>"normal"</c>, <c>"italic"</c>).</param>
	/// <param name="color">Constant text color for the whole layer; with <paramref name="colorBy"/> set it still colors other aesthetics’ legend swatches.</param>
	/// <param name="angle">Constant rotation in degrees.</param>
	/// <param name="scale">Whether this layer trains the (x, y) position scales; default trains both.</param>
	/// <param name="inherit">Inherit the plot's aesthetic mappings when none is given here.</param>
	public static PlotContext<T, TX, TY> Geom_Text<T, TX, TY, TT>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, double>? angleBy = null,
	  Func<T, TT>? text = null,
	  IAestheticMapping<T, string>? colorBy = null,
	  Size? size = null, Anchor anchor = Middle, string weight = "normal", string style = "normal", string color = "#23d0fc", double angle = 0.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Text(x, y, angleBy, text, colorBy, size, anchor, weight, style, color, angle, scale);

		return context;
	}
}

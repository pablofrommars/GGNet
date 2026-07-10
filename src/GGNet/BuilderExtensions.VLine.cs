namespace GGNet;

using Geoms.VLine;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds vertical reference lines with labels, one per item. Annotation: takes no event block.
	/// </summary>
	/// <param name="source">Items to annotate; one line per item.</param>
	/// <param name="x">Line position, in x-axis data units.</param>
	/// <param name="label">Label text per item.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke and label color.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	/// <param name="size">Label font size; null takes the theme’s.</param>
	/// <param name="anchor"><c>End</c> places the label at the top, rotated 90°; <c>Start</c> at the bottom, −90°.</param>
	/// <param name="weight">Label font weight (<c>"normal"</c>, <c>"bold"</c>).</param>
	/// <param name="style">Label font style (<c>"normal"</c>, <c>"italic"</c>).</param>
	public static PanelFactory<T1, TX1, TY> Geom_VLine<T1, TX1, TY, T2>(
	  this PanelFactory<T1, TX1, TY> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1> x,
	  Func<T2, string> label,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
	  where TX1 : struct
	  where TY : struct
	{
		panel.AddTyped(() =>
		{
			var geom = new VLine<T2, TX1, TY>(source, x, label)
			{
				Line = new()
				{
					Stroke = color,
					StrokeOpacity = opacity,
					StrokeWidth = strokeWidth,
					LineType = lineType
				},
				Text = new()
				{
					Anchor = anchor == End ? End : Start,
					FontSize = size ?? 0.75,
					FontWeight = weight,
					FontStyle = style,
					Color = color,
					Opacity = opacity
				}
			};

			return geom;
		});

		return panel;
	}

	/// <summary>
	/// Adds vertical reference lines with labels, one per item. Annotation: takes no event block.
	/// </summary>
	/// <param name="source">Items to annotate; one line per item.</param>
	/// <param name="x">Line position, in x-axis data units.</param>
	/// <param name="label">Label text per item.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke and label color.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	/// <param name="size">Label font size; null takes the theme’s.</param>
	/// <param name="anchor"><c>End</c> places the label at the top, rotated 90°; <c>Start</c> at the bottom, −90°.</param>
	/// <param name="weight">Label font weight (<c>"normal"</c>, <c>"bold"</c>).</param>
	/// <param name="style">Label font style (<c>"normal"</c>, <c>"italic"</c>).</param>
	public static PlotContext<T1, TX1, TY> Geom_VLine<T1, TX1, TY, T2>(
	  this PlotContext<T1, TX1, TY> context,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1> x,
	  Func<T2, string> label,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
	  where TX1 : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_VLine(source, x, label, strokeWidth, color, opacity, lineType, size, anchor, weight, style);

		return context;
	}

	/// <summary>
	/// Adds vertical reference lines with labels, one per item. Annotation: takes no event block.
	/// </summary>
	/// <param name="x">Line position, in x-axis data units.</param>
	/// <param name="label">Label text per item.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke and label color.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	/// <param name="size">Label font size; null takes the theme’s.</param>
	/// <param name="anchor"><c>End</c> places the label at the top, rotated 90°; <c>Start</c> at the bottom, −90°.</param>
	/// <param name="weight">Label font weight (<c>"normal"</c>, <c>"bold"</c>).</param>
	/// <param name="style">Label font style (<c>"normal"</c>, <c>"italic"</c>).</param>
	public static PanelFactory<T, TX, TY> Geom_VLine<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX> x,
	  Func<T, string> label,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
	  where TX : struct
	  where TY : struct
	{
		return Geom_VLine(panel, panel.Context.RequireSource(), x, label, strokeWidth, color, opacity, lineType, size, anchor, weight, style);
	}

	/// <summary>
	/// Adds vertical reference lines with labels, one per item. Annotation: takes no event block.
	/// </summary>
	/// <param name="x">Line position, in x-axis data units.</param>
	/// <param name="label">Label text per item.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke and label color.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	/// <param name="size">Label font size; null takes the theme’s.</param>
	/// <param name="anchor"><c>End</c> places the label at the top, rotated 90°; <c>Start</c> at the bottom, −90°.</param>
	/// <param name="weight">Label font weight (<c>"normal"</c>, <c>"bold"</c>).</param>
	/// <param name="style">Label font style (<c>"normal"</c>, <c>"italic"</c>).</param>
	public static PlotContext<T, TX, TY> Geom_VLine<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX> x,
	  Func<T, string> label,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_VLine(x, label, strokeWidth, color, opacity, lineType, size, anchor, weight, style);

		return context;
	}
}

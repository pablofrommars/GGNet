namespace GGNet;

using Geoms.HLine;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Adds horizontal reference lines with labels, one per item. Annotation: takes no event block.
	/// </summary>
	/// <param name="source">Items to annotate; one line per item.</param>
	/// <param name="y">Line position, in y-axis data units.</param>
	/// <param name="label">Label text per item.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke and label color.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	/// <param name="size">Label font size; null takes the theme’s.</param>
	/// <param name="anchor"><c>End</c> places the label at the right edge; <c>Start</c> at the left.</param>
	/// <param name="weight">Label font weight (<c>"normal"</c>, <c>"bold"</c>).</param>
	/// <param name="style">Label font style (<c>"normal"</c>, <c>"italic"</c>).</param>
	public static PanelFactory<T1, TX, TY1> Geom_HLine<T1, TX, TY1, T2>(
	  this PanelFactory<T1, TX, TY1> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TY1> y,
	  Func<T2, string> label,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
	  where TX : struct
	  where TY1 : struct
	{
		panel.AddTyped(() =>
		{
			var geom = new HLine<T2, TX, TY1>(source, y, label)
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
	/// Adds horizontal reference lines with labels, one per item. Annotation: takes no event block.
	/// </summary>
	/// <param name="source">Items to annotate; one line per item.</param>
	/// <param name="y">Line position, in y-axis data units.</param>
	/// <param name="label">Label text per item.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke and label color.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	/// <param name="size">Label font size; null takes the theme’s.</param>
	/// <param name="anchor"><c>End</c> places the label at the right edge; <c>Start</c> at the left.</param>
	/// <param name="weight">Label font weight (<c>"normal"</c>, <c>"bold"</c>).</param>
	/// <param name="style">Label font style (<c>"normal"</c>, <c>"italic"</c>).</param>
	public static PlotContext<T1, TX, TY1> Geom_HLine<T1, TX, TY1, T2>(
	  this PlotContext<T1, TX, TY1> context,
	  IReadOnlyList<T2> source,
	  Func<T2, TY1> y,
	  Func<T2, string> label,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
	  where TX : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_HLine(source, y, label, strokeWidth, color, opacity, lineType, size, anchor, weight, style);

		return context;
	}

	/// <summary>
	/// Adds horizontal reference lines with labels, one per item. Annotation: takes no event block.
	/// </summary>
	/// <param name="y">Line position, in y-axis data units.</param>
	/// <param name="label">Label text per item.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke and label color.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	/// <param name="size">Label font size; null takes the theme’s.</param>
	/// <param name="anchor"><c>End</c> places the label at the right edge; <c>Start</c> at the left.</param>
	/// <param name="weight">Label font weight (<c>"normal"</c>, <c>"bold"</c>).</param>
	/// <param name="style">Label font style (<c>"normal"</c>, <c>"italic"</c>).</param>
	public static PanelFactory<T, TX, TY> Geom_HLine<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TY> y,
	  Func<T, string> label,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
	  where TX : struct
	  where TY : struct
	{
		return Geom_HLine(panel, panel.Context.RequireSource(), y, label, strokeWidth, color, opacity, lineType, size, anchor, weight, style);
	}

	/// <summary>
	/// Adds horizontal reference lines with labels, one per item. Annotation: takes no event block.
	/// </summary>
	/// <param name="y">Line position, in y-axis data units.</param>
	/// <param name="label">Label text per item.</param>
	/// <param name="strokeWidth">Stroke width in pixels.</param>
	/// <param name="color">Constant stroke and label color.</param>
	/// <param name="opacity">Stroke opacity, 0–1.</param>
	/// <param name="lineType">Dash pattern.</param>
	/// <param name="size">Label font size; null takes the theme’s.</param>
	/// <param name="anchor"><c>End</c> places the label at the right edge; <c>Start</c> at the left.</param>
	/// <param name="weight">Label font weight (<c>"normal"</c>, <c>"bold"</c>).</param>
	/// <param name="style">Label font style (<c>"normal"</c>, <c>"italic"</c>).</param>
	public static PlotContext<T, TX, TY> Geom_HLine<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TY> y,
	  Func<T, string> label,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_HLine(y, label, strokeWidth, color, opacity, lineType, size, anchor, weight, style);

		return context;
	}
}

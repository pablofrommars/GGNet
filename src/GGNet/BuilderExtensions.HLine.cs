namespace GGNet;

using Geoms.HLine;
using Elements;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
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

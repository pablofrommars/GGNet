namespace GGNet;

using Geoms.VLine;
using Elements;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
	public static PanelFactory<T1, TX1, TY> Geom_VLine<T1, TX1, TY, T2>(
	  this PanelFactory<T1, TX1, TY> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1> x,
	  Func<T2, string> label,
	  double width = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
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
					StrokeWidth = width,
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

	public static PlotContext<T1, TX1, TY> Geom_VLine<T1, TX1, TY, T2>(
	  this PlotContext<T1, TX1, TY> context,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1> x,
	  Func<T2, string> label,
	  double width = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
	  where TX1 : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_VLine(source, x, label, width, color, opacity, lineType, size, anchor, weight, style);

		return context;
	}

	public static PanelFactory<T, TX, TY> Geom_VLine<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX> x,
	  Func<T, string> label,
	  double width = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
	  where TX : struct
	  where TY : struct
	{
		return Geom_VLine(panel, panel.Context.RequireSource(), x, label, width, color, opacity, lineType, size, anchor, weight, style);
	}

	public static PlotContext<T, TX, TY> Geom_VLine<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX> x,
	  Func<T, string> label,
	  double width = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
	  Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_VLine(x, label, width, color, opacity, lineType, size, anchor, weight, style);

		return context;
	}
}

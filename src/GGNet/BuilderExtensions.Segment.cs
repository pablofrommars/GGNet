namespace GGNet;

using Geoms.Segment;
using Elements;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
	public static PanelFactory<T1, TX1, TY1> Geom_Segment<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IReadOnlyList<T2> source,
	  Func<T2, TX1> x,
	  Func<T2, TX1> xend,
	  Func<T2, TY1> y,
	  Func<T2, TY1> yend,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)
	  where TX1 : struct
	  where TY1 : struct
	{
		panel.AddTyped(() =>
		{
			var geom = new Segment<T2, TX1, TY1>(source, x, xend, y, yend)
			{
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

	public static PlotContext<T1, TX1, TY1> Geom_Segment<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  Source<T2> source,
	  Func<T2, TX1> x,
	  Func<T2, TX1> xend,
	  Func<T2, TY1> y,
	  Func<T2, TY1> yend,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Segment(source, x, xend, y, yend, strokeWidth, color, opacity, lineType);

		return context;
	}

	public static PlotContext<T1, TX1, TY1> Geom_Segment<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IEnumerable<T2> source,
	  Func<T2, TX1> x,
	  Func<T2, TX1> xend,
	  Func<T2, TY1> y,
	  Func<T2, TY1> yend,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)
	  where TX1 : struct
	  where TY1 : struct
	  => context.Geom_Segment(new Source<T2>(source), x, xend, y, yend, strokeWidth, color, opacity, lineType);

	public static PanelFactory<T, TX, TY> Geom_Segment<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX> x,
	  Func<T, TX> xend,
	  Func<T, TY> y,
	  Func<T, TY> yend,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Segment(panel, panel.Context.RequireSource(), x, xend, y, yend, strokeWidth, color, opacity, lineType);
	}

	public static PlotContext<T, TX, TY> Geom_Segment<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX> x,
	  Func<T, TX> xend,
	  Func<T, TY> y,
	  Func<T, TY> yend,
	  double strokeWidth = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Segment(x, xend, y, yend, strokeWidth, color, opacity, lineType);

		return context;
	}
}

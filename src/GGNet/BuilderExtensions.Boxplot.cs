namespace GGNet;

using Geoms.Boxplot;
using Elements;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
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

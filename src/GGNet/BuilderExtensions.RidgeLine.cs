namespace GGNet;

using Geoms.RidgeLine;
using Elements;
using Exceptions;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
	public static PanelFactory<T1, TX1, TY1> Geom_RidgeLine<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? height = null,
	  IAestheticMapping<T2, string>? _fill = null,
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
			var geom = new RidgeLine<T2, TX1, TY1>(source, x, y, height, _fill, scale)
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

	public static PanelFactory<T1, TX1, TY1> Geom_RidgeLine<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? height = null,
	  IAestheticMapping<T2, string>? _fill = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return panel.Geom_RidgeLine(new Source<T2>(source), x, y, height, _fill, fill, fillOpacity, scale);
	}

	public static PlotContext<T1, TX1, TY1> Geom_RidgeLine<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? height = null,
	  IAestheticMapping<T2, string>? _fill = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_RidgeLine(source, x, y, height, _fill, fill, fillOpacity, scale);

		return context;
	}

	public static PlotContext<T1, TX1, TY1> Geom_RidgeLine<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? height = null,
	  IAestheticMapping<T2, string>? _fill = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return context.Geom_RidgeLine(new Source<T2>(source), x, y, height, _fill, fill, fillOpacity, scale);
	}

	public static PanelFactory<T, TX, TY> Geom_RidgeLine<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, double>? height = null,
	  IAestheticMapping<T, string>? _fill = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		return Geom_RidgeLine(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, height, _fill ?? (inherit ? panel.Context.Aesthetics.Fill : null), fill, fillOpacity, scale);
	}

	public static PlotContext<T, TX, TY> Geom_RidgeLine<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, double>? height = null,
	  IAestheticMapping<T, string>? _fill = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_RidgeLine(x, y, height, _fill, fill, fillOpacity, scale);

		return context;
	}
}

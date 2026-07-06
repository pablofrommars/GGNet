namespace GGNet;

using Geoms.Violin;
using Elements;
using Exceptions;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
	public static PanelFactory<T1, TX1, TY1> Geom_Violin<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? width = null,
	  IAestheticMapping<T2, string>? _fill = null,
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
			var geom = new Violin<T2, TX1, TY1>(source, x, y, width, _fill, position, scale)
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

	public static PanelFactory<T1, TX1, TY1> Geom_Violin<T1, TX1, TY1, T2>(
	  this PanelFactory<T1, TX1, TY1> panel,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? width = null,
	  IAestheticMapping<T2, string>? _fill = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string? stroke = null,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return panel.Geom_Violin(new Source<T2>(source), x, y, width, _fill, fill, fillOpacity, stroke, position, scale);
	}

	public static PlotContext<T1, TX1, TY1> Geom_Violin<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  Source<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? width = null,
	  IAestheticMapping<T2, string>? _fill = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string? stroke = null,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		context.Default_Panel().Geom_Violin(source, x, y, width, _fill, fill, fillOpacity, stroke, position, scale);

		return context;
	}

	public static PlotContext<T1, TX1, TY1> Geom_Violin<T1, TX1, TY1, T2>(
	  this PlotContext<T1, TX1, TY1> context,
	  IEnumerable<T2> source,
	  Func<T2, TX1>? x = null,
	  Func<T2, TY1>? y = null,
	  Func<T2, double>? width = null,
	  IAestheticMapping<T2, string>? _fill = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string? stroke = null,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX1 : struct
	  where TY1 : struct
	{
		return context.Geom_Violin(new Source<T2>(source), x, y, width, _fill, fill, fillOpacity, stroke, position, scale);
	}

	public static PanelFactory<T, TX, TY> Geom_Violin<T, TX, TY>(
	  this PanelFactory<T, TX, TY> panel,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, double>? width = null,
	  IAestheticMapping<T, string>? _fill = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string? stroke = null,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		return Geom_Violin(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, width, _fill ?? (inherit ? panel.Context.Aesthetics.Fill : null), fill, fillOpacity, stroke, position, scale);
	}

	public static PlotContext<T, TX, TY> Geom_Violin<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TX>? x = null,
	  Func<T, TY>? y = null,
	  Func<T, double>? width = null,
	  IAestheticMapping<T, string>? _fill = null,
	  string fill = "#23d0fc", double fillOpacity = 1.0, string? stroke = null,
	  PositionAdjustment position = PositionAdjustment.Identity,
	  (bool x, bool y)? scale = null, bool inherit = true)
	  where TX : struct
	  where TY : struct
	{
		context.Default_Panel().Geom_Violin(x, y, width, _fill, fill, fillOpacity, stroke, position, scale);

		return context;
	}
}

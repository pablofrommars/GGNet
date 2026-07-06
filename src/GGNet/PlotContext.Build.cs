namespace GGNet;

using Exceptions;

public partial class PlotContext
{
	private static PlotContext<T, TX, TY> BuildBase<T, TX, TY>(IReadOnlyList<T>? source, Func<T, TX>? x, Func<T, TY>? y)
	  where TX : struct
	  where TY : struct
	{
		var context = new PlotContext<T, TX, TY>()
		{
			Source = source,
		};

		context.Selectors.X = x;
		context.Selectors.Y = y;

		return context;
	}

	// Default scales are decided here, where overload resolution has already
	// dispatched on TX/TY; Init invokes them only when the user registered no
	// scale. Polar plots use different expansions: the angular scale spans the
	// full turn (discrete (0,0,0,1) maps n categories to fractions i/n, the
	// extra slot being the closing wrap segment) and the radial scale is
	// zero-based at the center.

	private static void ContinuousX<T, TY>(PlotContext<T, double, TY> context)
	  where TY : struct
	  => context.XScaleDefault = polar => context.Scale_X_Continuous(expand: polar ? (0.0, 0.0, 0.0, 0.0) : null);

	private static void DiscreteX<T, TX, TY>(PlotContext<T, TX, TY> context)
	  where TX : struct, Enum
	  where TY : struct
	  => context.XScaleDefault = polar => context.Scale_X_Discrete(expand: polar ? (0.0, 0.0, 0.0, 1.0) : null);

	private static void ContinuousY<T, TX>(PlotContext<T, TX, double> context)
	  where TX : struct
	  => context.YScaleDefault = polar => context.Scale_Y_Continuous(expand: polar ? (0.0, 0.0, 0.05, 0.0) : null);

	private static void DiscreteY<T, TX, TY>(PlotContext<T, TX, TY> context)
	  where TX : struct
	  where TY : struct, Enum
	  => context.YScaleDefault = _ => context.Scale_Y_Discrete();

	public static PlotContext<T, LocalDate, double> Build<T>(IReadOnlyList<T> source, Func<T, LocalDate> x, Func<T, double>? y = null)
	{
		var context = BuildBase(source, x, y);

		context.XScaleDefault = _ => context.Scale_X_Discrete_Date();
		ContinuousY(context);

		return context;
	}

	public static PlotContext<T, LocalDateTime, double> Build<T>(IReadOnlyList<T> source, Func<T, LocalDateTime> x, Func<T, double>? y = null)
	{
		var context = BuildBase(source, x, y);

		context.XScaleDefault = _ => context.Scale_X_Discrete_DateTime();
		ContinuousY(context);

		return context;
	}

	public static PlotContext<T, Instant, double> Build<T>(IReadOnlyList<T> source, Func<T, Instant> x, Func<T, double>? y = null)
	{
		var context = BuildBase(source, x, y);

		context.XScaleDefault = _ => throw new GGNetUserException("Scale_X_Instant required");
		ContinuousY(context);

		return context;
	}

	public static PlotContext<T, TX, double> Build<T, TX>(IReadOnlyList<T> source, Func<T, TX> x, Func<T, double>? y = null)
	  where TX : struct, Enum
	{
		var context = BuildBase(source, x, y);

		DiscreteX(context);
		ContinuousY(context);

		return context;
	}

	public static PlotContext<T, TX, TY> Build<T, TX, TY>(IReadOnlyList<T> source, Func<T, TX> x, Func<T, TY> y)
	  where TX : struct, Enum
	  where TY : struct, Enum
	{
		var context = BuildBase(source, x, y);

		DiscreteX(context);
		DiscreteY(context);

		return context;
	}

	public static PlotContext<T, double, TY> Build<T, TY>(IReadOnlyList<T> source, Func<T, double> x, Func<T, TY> y)
	  where TY : struct, Enum
	{
		var context = BuildBase(source, x, y);

		ContinuousX(context);
		DiscreteY(context);

		return context;
	}

	public static PlotContext<T, double, double> Build<T>(IReadOnlyList<T> source, Func<T, double>? x = null, Func<T, double>? y = null)
	{
		var context = BuildBase(source, x, y);

		ContinuousX(context);
		ContinuousY(context);

		return context;
	}

	public static PlotContext<NoData, double, double> Build()
	{
		var context = BuildBase<NoData, double, double>(null, null, null);

		ContinuousX(context);
		ContinuousY(context);

		return context;
	}
}

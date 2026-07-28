namespace GGNet;

using Exceptions;
using Facets;
using Formats;

using Geoms;

using Scales;
using Transformations;

using static Position;

public static partial class BuilderExtensions
{
	/// <summary>
	/// Configures a discrete date x scale with day/month tick labeling.
	/// </summary>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	/// <param name="expand">Padding beyond the data range as (lower multiplier, lower additive, upper multiplier, upper additive) — multipliers scale the range span, additives are in the scale's own space (data units on an untransformed scale, log10 exponents on a log scale, category indices on a discrete one).</param>
	public static PlotContext<T, LocalDate, TY> Scale_X_Discrete_Date<T, TY>(
	  this PlotContext<T, LocalDate, TY> context,
	  (LocalDate? min, LocalDate? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null)
	  where TY : struct
	{
		context.Positions.X.Factory = () => new DiscreteDates(null, limits, expand);

		return context;
	}

	/// <summary>
	/// Configures a discrete date-time x scale with time-of-day and date-boundary tick labeling.
	/// </summary>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	/// <param name="expand">Padding beyond the data range as (lower multiplier, lower additive, upper multiplier, upper additive) — multipliers scale the range span, additives are in the scale's own space (data units on an untransformed scale, log10 exponents on a log scale, category indices on a discrete one).</param>
	public static PlotContext<T, LocalDateTime, TY> Scale_X_Discrete_DateTime<T, TY>(
	  this PlotContext<T, LocalDateTime, TY> context,
	  (LocalDateTime? min, LocalDateTime? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null)
	  where TY : struct
	{
		context.Positions.X.Factory = () => new DateTimePosition(null, limits, expand);

		return context;
	}

	/// <summary>
	/// Configures the instant x scale; required for Instant selectors.
	/// </summary>
	/// <param name="start">Window start; null keeps the data-driven bound.</param>
	/// <param name="end">Window end; null keeps the data-driven bound.</param>
	/// <param name="formatter">Break-label formatter; defaults to invariant H:mm:ss in UTC.</param>
	public static PlotContext<T, Instant, TY> Scale_X_Instant<T, TY>(
	  this PlotContext<T, Instant, TY> context,
	  Instant? start = null, Instant? end = null,
	  IFormatter<Instant>? formatter = null)
	  where TY : struct
	{
		context.Positions.X.Factory = () => new InstantPosition(start, end, formatter);

		return context;
	}

	/// <summary>
	/// Configures the continuous x scale (Wilkinson-extended breaks).
	/// </summary>
	/// <param name="transformation">Value transformation (log10, sqrt) applied before mapping; breaks and labels stay in data units.</param>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	/// <param name="expand">Padding beyond the data range as (lower multiplier, lower additive, upper multiplier, upper additive) — multipliers scale the range span, additives are in the scale's own space (data units on an untransformed scale, log10 exponents on a log scale, category indices on a discrete one).</param>
	/// <param name="formatter">Break-label formatter; defaults to invariant general formatting.</param>
	/// <param name="hide">Train and map normally but render no axis.</param>
	/// <param name="includeMinorBreaks">Midpoint gridlines between major breaks.</param>
	public static PlotContext<T, double, TY> Scale_X_Continuous<T, TY>(
	  this PlotContext<T, double, TY> context,
	  ITransformation<double>? transformation = null,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  IFormatter<double>? formatter = null,
	  bool hide = false,
	  bool includeMinorBreaks = true)
	  where TY : struct
	{
		context.Positions.X.Factory = () => new Extended(transformation, limits, expand, formatter, hide, includeMinorBreaks);

		return context;
	}

	/// <summary>
	/// Configures the discrete x scale: one slot per distinct trained value, in sort order.
	/// </summary>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	/// <param name="expand">Padding beyond the data range as (lower multiplier, lower additive, upper multiplier, upper additive) — multipliers scale the range span, additives are in the scale's own space (data units on an untransformed scale, log10 exponents on a log scale, category indices on a discrete one).</param>
	/// <param name="formatter">Break-label formatter; defaults to invariant general formatting.</param>
	/// <param name="offset">Break and label offset from the category index, in axis units.</param>
	/// <param name="hide">Train and map normally but render no axis.</param>
	public static PlotContext<T, TX, TY> Scale_X_Discrete<T, TX, TY>(
	   this PlotContext<T, TX, TY> context,
	   (TX? min, TX? max)? limits = null,
	   (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	   IFormatter<TX>? formatter = null,
	   double offset = 0.0,
	   bool hide = false)
	   where TX : struct
	   where TY : struct
	{
		context.Positions.X.Factory = () => new DiscretePosition<TX>(null, limits, expand, formatter, offset, hide);

		return context;
	}

	/// <summary>
	/// Clamps the x range, in data units.
	/// </summary>
	/// <param name="min">Lower bound; null keeps the data-driven bound.</param>
	/// <param name="max">Upper bound; null keeps the data-driven bound.</param>
	public static PlotContext<T, double, TY> XLim<T, TY>(this PlotContext<T, double, TY> context, double? min = null, double? max = null)
	  where TY : struct
	{
		if (context.Positions.X.Factory is null)
		{
			context.Scale_X_Continuous(formatter: new DoubleFormatter("N2"));
		}

		var old = context.Positions.X.Factory!;

		context.Positions.X.Factory = () =>
		{
			var scale = old();

			scale.Limits = (min, max);

			return scale;
		};

		return context;
	}

	/// <summary>
	/// Clamps the x range, in data units.
	/// </summary>
	/// <param name="min">Lower bound; null keeps the data-driven bound.</param>
	/// <param name="max">Upper bound; null keeps the data-driven bound.</param>
	public static PlotContext<T, LocalDate, TY> XLim<T, TY>(this PlotContext<T, LocalDate, TY> context, LocalDate? min = null, LocalDate? max = null)
	  where TY : struct
	{
		if (context.Positions.X.Factory is null)
		{
			context.Scale_X_Discrete_Date();
		}

		var old = context.Positions.X.Factory!;

		context.Positions.X.Factory = () =>
		{
			var scale = old();

			scale.Limits = (min, max);

			return scale;
		};

		return context;
	}

	/// <summary>
	/// Configures the continuous y scale (Wilkinson-extended breaks).
	/// </summary>
	/// <param name="transformation">Value transformation (log10, sqrt) applied before mapping; breaks and labels stay in data units.</param>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	/// <param name="expand">Padding beyond the data range as (lower multiplier, lower additive, upper multiplier, upper additive) — multipliers scale the range span, additives are in the scale's own space (data units on an untransformed scale, log10 exponents on a log scale, category indices on a discrete one).</param>
	/// <param name="formatter">Break-label formatter; defaults to invariant general formatting.</param>
	/// <param name="hide">Train and map normally but render no axis.</param>
	/// <param name="includeMinorBreaks">Midpoint gridlines between major breaks.</param>
	public static PlotContext<T, TX, double> Scale_Y_Continuous<T, TX>(
	  this PlotContext<T, TX, double> context,
	  ITransformation<double>? transformation = null,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  IFormatter<double>? formatter = null,
	  bool hide = false,
	  bool includeMinorBreaks = true)
	  where TX : struct
	{
		context.Positions.Y.Factory = () => new Extended(transformation, limits, expand, formatter, hide, includeMinorBreaks);

		return context;
	}

	/// <summary>
	/// Configures the continuous y scale (Wilkinson-extended breaks).
	/// </summary>
	/// <param name="transformation">Value transformation (log10, sqrt) applied before mapping; breaks and labels stay in data units.</param>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	/// <param name="expand">Padding beyond the data range as (lower multiplier, lower additive, upper multiplier, upper additive) — multipliers scale the range span, additives are in the scale's own space (data units on an untransformed scale, log10 exponents on a log scale, category indices on a discrete one).</param>
	/// <param name="formatter">Break-label formatter; defaults to invariant general formatting.</param>
	/// <param name="hide">Train and map normally but render no axis.</param>
	/// <param name="includeMinorBreaks">Midpoint gridlines between major breaks.</param>
	public static PanelFactory<T, TX, double> Scale_Y_Continuous<T, TX>(
	  this PanelFactory<T, TX, double> panel,
	   ITransformation<double>? transformation = null,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  IFormatter<double>? formatter = null,
	   bool hide = false,
	  bool includeMinorBreaks = true)
	  where TX : struct
	{
		panel.Y = () => new Extended(transformation, limits, expand, formatter, hide, includeMinorBreaks);

		return panel;
	}

	/// <summary>
	/// Configures a square-root-transformed continuous x scale.
	/// </summary>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	/// <param name="expand">Padding beyond the data range as (lower multiplier, lower additive, upper multiplier, upper additive) — multipliers scale the range span, additives are in the scale's own space (data units on an untransformed scale, log10 exponents on a log scale, category indices on a discrete one).</param>
	/// <param name="formatter">Break-label formatter; defaults to invariant general formatting.</param>
	/// <param name="hide">Train and map normally but render no axis.</param>
	/// <param name="includeMinorBreaks">Midpoint gridlines between major breaks.</param>
	public static PlotContext<T, double, TY> Scale_X_Sqrt<T, TY>(
	  this PlotContext<T, double, TY> context,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  IFormatter<double>? formatter = null,
	  bool hide = false,
	  bool includeMinorBreaks = true)
	  where TY : struct
	  => context.Scale_X_Continuous(Sqrt.Instance, limits, expand, formatter, hide, includeMinorBreaks);

	/// <summary>
	/// Configures a base-10 logarithmic continuous x scale.
	/// </summary>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	/// <param name="expand">Padding beyond the data range as (lower multiplier, lower additive, upper multiplier, upper additive) — multipliers scale the range span, additives are in the scale's own space (data units on an untransformed scale, log10 exponents on a log scale, category indices on a discrete one).</param>
	/// <param name="formatter">Break-label formatter; defaults to invariant general formatting.</param>
	public static PlotContext<T, double, TY> Scale_X_Log10<T, TY>(
	  this PlotContext<T, double, TY> context,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  IFormatter<double>? formatter = null)
	  where TY : struct
	{
		context.Positions.X.Factory = () => new Scales.Log10(limits, expand, formatter);

		return context;
	}

	/// <summary>
	/// Configures a square-root-transformed continuous y scale.
	/// </summary>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	/// <param name="expand">Padding beyond the data range as (lower multiplier, lower additive, upper multiplier, upper additive) — multipliers scale the range span, additives are in the scale's own space (data units on an untransformed scale, log10 exponents on a log scale, category indices on a discrete one).</param>
	/// <param name="formatter">Break-label formatter; defaults to invariant general formatting.</param>
	/// <param name="hide">Train and map normally but render no axis.</param>
	public static PanelFactory<T, TX, double> Scale_Y_Sqrt<T, TX>(
	  this PanelFactory<T, TX, double> panel,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  IFormatter<double>? formatter = null,
	  bool hide = false)
	  where TX : struct
	  => panel.Scale_Y_Continuous(Sqrt.Instance, limits, expand, formatter, hide);

	/// <summary>
	/// Configures a square-root-transformed continuous y scale.
	/// </summary>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	/// <param name="expand">Padding beyond the data range as (lower multiplier, lower additive, upper multiplier, upper additive) — multipliers scale the range span, additives are in the scale's own space (data units on an untransformed scale, log10 exponents on a log scale, category indices on a discrete one).</param>
	/// <param name="formatter">Break-label formatter; defaults to invariant general formatting.</param>
	/// <param name="hide">Train and map normally but render no axis.</param>
	public static PlotContext<T, TX, double> Scale_Y_Sqrt<T, TX>(
	  this PlotContext<T, TX, double> context,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  IFormatter<double>? formatter = null,
	  bool hide = false)
	  where TX : struct
	  => context.Scale_Y_Continuous(Sqrt.Instance, limits, expand, formatter, hide);

	/// <summary>
	/// Configures a base-10 logarithmic continuous y scale.
	/// </summary>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	/// <param name="expand">Padding beyond the data range as (lower multiplier, lower additive, upper multiplier, upper additive) — multipliers scale the range span, additives are in the scale's own space (data units on an untransformed scale, log10 exponents on a log scale, category indices on a discrete one).</param>
	/// <param name="formatter">Break-label formatter; defaults to invariant general formatting.</param>
	public static PanelFactory<T, TX, double> Scale_Y_Log10<T, TX>(
	  this PanelFactory<T, TX, double> panel,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  IFormatter<double>? formatter = null)
	  where TX : struct
	{
		panel.Y = () => new Scales.Log10(limits, expand, formatter);

		return panel;
	}

	/// <summary>
	/// Configures a base-10 logarithmic continuous y scale.
	/// </summary>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	/// <param name="expand">Padding beyond the data range as (lower multiplier, lower additive, upper multiplier, upper additive) — multipliers scale the range span, additives are in the scale's own space (data units on an untransformed scale, log10 exponents on a log scale, category indices on a discrete one).</param>
	/// <param name="formatter">Break-label formatter; defaults to invariant general formatting.</param>
	public static PlotContext<T, TX, double> Scale_Y_Log10<T, TX>(
	  this PlotContext<T, TX, double> context,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  IFormatter<double>? formatter = null)
	  where TX : struct
	{
		context.Positions.Y.Factory = () => new Scales.Log10(limits, expand, formatter);

		return context;
	}

	/// <summary>
	/// Configures the discrete y scale: one slot per distinct trained value, in sort order.
	/// </summary>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	/// <param name="expand">Padding beyond the data range as (lower multiplier, lower additive, upper multiplier, upper additive) — multipliers scale the range span, additives are in the scale's own space (data units on an untransformed scale, log10 exponents on a log scale, category indices on a discrete one).</param>
	/// <param name="formatter">Break-label formatter; defaults to invariant general formatting.</param>
	/// <param name="offset">Break and label offset from the category index, in axis units.</param>
	/// <param name="hide">Train and map normally but render no axis.</param>
	public static PlotContext<T, TX, TY> Scale_Y_Discrete<T, TX, TY>(
	   this PlotContext<T, TX, TY> context,
	   (TY? min, TY? max)? limits = null,
	   (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	   IFormatter<TY>? formatter = null,
	   double offset = 0.0,
	   bool hide = false)
	   where TX : struct
	   where TY : struct
	{
		context.Positions.Y.Factory = () => new DiscretePosition<TY>(null, limits, expand, formatter, offset, hide);

		return context;
	}

	/// <summary>
	/// Configures the x scale with degree (E/W) labels.
	/// </summary>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	public static PlotContext<T, double, TY> Scale_Longitude<T, TY>(
	  this PlotContext<T, double, TY> context,
	  (double? min, double? max)? limits = null)
	  where TY : struct
	  => context.Scale_X_Continuous(null, limits ?? (-180, 180), (0, 0, 0, 0), Longitude.Instance);

	/// <summary>
	/// Configures the y scale with degree (N/S) labels.
	/// </summary>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	public static PanelFactory<T, TX, double> Scale_Latitude<T, TX>(
	   this PanelFactory<T, TX, double> panel,
	   (double? min, double? max)? limits = null)
	   where TX : struct
	   => panel.Scale_Y_Continuous(null, limits ?? (-90, 90), (0, 0, 0, 0), Latitude.Instance);

	/// <summary>
	/// Configures the y scale with degree (N/S) labels.
	/// </summary>
	/// <param name="limits">Clamp the trained range; null on either side keeps the data-driven bound.</param>
	public static PlotContext<T, TX, double> Scale_Latitude<T, TX>(
	  this PlotContext<T, TX, double> context,
	  (double? min, double? max)? limits = null)
	  where TX : struct
	  => context.Scale_Y_Continuous(null, limits ?? (-90, 90), (0, 0, 0, 0), Latitude.Instance);

	/// <summary>
	/// Clamps the y range, in data units.
	/// </summary>
	/// <param name="min">Lower bound; null keeps the data-driven bound.</param>
	/// <param name="max">Upper bound; null keeps the data-driven bound.</param>
	public static PlotContext<T, TX, double> YLim<T, TX>(this PlotContext<T, TX, double> context, double? min = null, double? max = null)
	   where TX : struct
	{
		if (context.Positions.Y.Factory is null)
		{
			context.Scale_Y_Continuous();
		}

		var old = context.Positions.Y.Factory!;

		context.Positions.Y.Factory = () =>
		{
			var scale = old();

			scale.Limits = (min, max);

			return scale;
		};

		return context;
	}

	/// <summary>
	/// Clamps the y range, in data units.
	/// </summary>
	/// <param name="min">Lower bound; null keeps the data-driven bound.</param>
	/// <param name="max">Upper bound; null keeps the data-driven bound.</param>
	public static PanelFactory<T, TX, double> YLim<T, TX>(this PanelFactory<T, TX, double> panel, double? min = null, double? max = null)
	  where TX : struct
	{
		if (panel.Y is null)
		{
			panel.Scale_Y_Continuous(formatter: new DoubleFormatter("N2"));
		}

		var old = panel.Y!;

		panel.Y = () =>
		{
			var scale = old();

			scale.Limits = (min, max);

			return scale;
		};

		return panel;
	}

	internal static PanelFactory<T, TX, TY> Default_Panel<T, TX, TY>(this PlotContext<T, TX, TY> context)
	  where TX : struct
	  where TY : struct
	{
		context.DefaultFactory ??= new PanelFactory<T, TX, TY>(context);

		return context.DefaultFactory;
	}

	/// <summary>
	/// Adds a sub-panel; layers chained on the factory belong to it.
	/// </summary>
	/// <param name="factory">Configures the sub-panel: chain Geom_/Scale_ calls on the given factory.</param>
	/// <param name="width">Panel share of the plot width, 0–1.</param>
	/// <param name="height">Panel share of the plot height, 0–1.</param>
	/// <param name="onClick">Panel-level click handler.</param>
	public static PlotContext<T, TX, TY> Panel<T, TX, TY>(this PlotContext<T, TX, TY> context, Func<PanelFactory<T, TX, TY>, PanelFactory<T, TX, TY>> factory, double width = 1.0, double height = 1.0, Func<MouseEventArgs, Task>? onClick = null)
	  where TX : struct
	  where TY : struct
	{
		var panel = factory(new PanelFactory<T, TX, TY>(context, width, height, onClick));

		context.PanelFactories.Add(panel);

		return context;
	}

	/// <summary>
	/// Sets the plot title.
	/// </summary>
	/// <param name="title">Markdown-rendered title.</param>
	public static PlotContext<T, TX, TY> Title<T, TX, TY>(this PlotContext<T, TX, TY> context, [StringSyntax("Markdown")] string title)
	  where TX : struct
	  where TY : struct
	{
		context.Title = Markdown.Text(title);

		return context;
	}

	/// <summary>
	/// Sets the plot subtitle.
	/// </summary>
	/// <param name="subtitle">Markdown-rendered subtitle.</param>
	public static PlotContext<T, TX, TY> SubTitle<T, TX, TY>(this PlotContext<T, TX, TY> context, [StringSyntax("Markdown")] string subtitle)
	  where TX : struct
	  where TY : struct
	{
		context.SubTitle = Markdown.Text(subtitle);

		return context;
	}

	/// <summary>
	/// Sets the bottom-right caption.
	/// </summary>
	/// <param name="caption">Markdown-rendered caption.</param>
	public static PlotContext<T, TX, TY> Caption<T, TX, TY>(this PlotContext<T, TX, TY> context, [StringSyntax("Markdown")] string caption)
	  where TX : struct
	  where TY : struct
	{
		context.Caption = Markdown.Text(caption);

		return context;
	}

	/// <summary>
	/// Sets the x-axis title.
	/// </summary>
	/// <param name="xlab">Markdown-rendered axis title.</param>
	public static PlotContext<T, TX, TY> XLab<T, TX, TY>(this PlotContext<T, TX, TY> context, [StringSyntax("Markdown")] string xlab)
	  where TX : struct
	  where TY : struct
	{
		context.XLab = Markdown.Text(xlab);

		return context;
	}

	/// <summary>
	/// Sets the y-axis title.
	/// </summary>
	/// <param name="ylab">Markdown-rendered axis title.</param>
	public static PanelFactory<T, TX, TY> YLab<T, TX, TY>(this PanelFactory<T, TX, TY> panel, [StringSyntax("Markdown")] string ylab)
	  where TX : struct
	  where TY : struct
	{
		panel.YLab = Markdown.Text(ylab);

		return panel;
	}

	/// <summary>
	/// Sets the y-axis title.
	/// </summary>
	/// <param name="ylab">Markdown-rendered axis title.</param>
	public static PlotContext<T, TX, TY> YLab<T, TX, TY>(this PlotContext<T, TX, TY> context, [StringSyntax("Markdown")] string ylab)
	  where TX : struct
	  where TY : struct
	{

		context.Default_Panel().YLab(ylab);

		return context;
	}

	/// <summary>
	/// Runs axis-asymmetric statistics (bar grouping, stacking, dodging) along x instead of y — supply selectors already swapped: x carries values, y categories. Symmetric geoms need no Flip. Incompatible with polar.
	/// </summary>
	public static PlotContext<T, TX, TY> Flip<T, TX, TY>(this PlotContext<T, TX, TY> context)
	  where TX : struct
	  where TY : struct
	{
		context.Flip = true;

		return context;
	}

	/// <summary>
	/// Switches the plot to polar coordinates: x becomes angular, y radial.
	/// </summary>
	/// <param name="startAngle">Angle of the first category in radians; −π/2 (the default) is 12 o’clock.</param>
	/// <param name="clockwise">Direction of increasing angular values.</param>
	public static PlotContext<T, TX, TY> Coord_Polar<T, TX, TY>(this PlotContext<T, TX, TY> context, double startAngle = -Math.PI / 2.0, bool clockwise = true)
	  where TX : struct
	  where TY : struct
	{
		context.CoordSystem = CoordSystem.Polar;

		context.PolarOptions.StartAngle = startAngle;
		context.PolarOptions.Clockwise = clockwise;

		return context;
	}

	// Geoms are constructed with the panel's own axis types, so binding is fully typed.

	internal static PanelFactory<T1, TX1, TY1> AddTyped<T1, TX1, TY1, T2>(this PanelFactory<T1, TX1, TY1> panel, Func<Geom<T2, TX1, TY1>> builder)
	  where TX1 : struct
	  where TY1 : struct
	{
		panel.Add((p, f) =>
		{
			var geom = builder();

			geom.Init(p, f);

			return geom;
		});

		return panel;
	}

	/// <summary>
	/// Maps a selector to stroke/point colors; geoms consume it via colorBy or inherit.
	/// </summary>
	/// <param name="selector">Value mapped to a color per item.</param>
	/// <param name="palette">Colors assigned to the selector’s distinct values, in sort order.</param>
	/// <param name="guide">Show a legend entry for this scale.</param>
	/// <param name="name">Legend title.</param>
	public static PlotContext<T, TX, TY> Scale_Color_Discrete<T, TX, TY, TKey>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TKey> selector,
	  Palettes.Discrete<TKey, string> palette,
	  bool guide = true,
	  string? name = null)
	  where TX : struct
	  where TY : struct
	  where TKey : notnull
	{
		var scale = new ColorDiscrete<TKey>(palette);

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.Color = new Aesthetic<T, TKey, string>(selector, scale, guide, name);

		return context;
	}

	/// <summary>
	/// Maps a selector to stroke/point colors; geoms consume it via colorBy or inherit.
	/// </summary>
	/// <param name="selector">Value mapped to a color per item.</param>
	/// <param name="palette">Colors assigned to the selector’s distinct values, in sort order.</param>
	/// <param name="direction">Palette direction: 1 forward, −1 reversed.</param>
	/// <param name="guide">Show a legend entry for this scale.</param>
	/// <param name="name">Legend title.</param>
	public static PlotContext<T, TX, TY> Scale_Color_Discrete<T, TX, TY, TKey>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TKey> selector,
	  string[] palette,
	  int direction = 1,
	  bool guide = true,
	  string? name = null)
	  where TX : struct
	  where TY : struct
	  where TKey : notnull
	{
		var scale = new ColorDiscrete<TKey>(palette, direction);

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.Color = new Aesthetic<T, TKey, string>(selector, scale, guide, name);

		return context;
	}

	/// <summary>
	/// Uses the selector’s value directly as the color; no scale, no legend.
	/// </summary>
	/// <param name="selector">Color per item (any css color).</param>
	public static PlotContext<T, TX, TY> Scale_Color_Identity<T, TX, TY>(this PlotContext<T, TX, TY> context, Func<T, string> selector)
	  where TX : struct
	  where TY : struct
	{
		var scale = new Scales.Identity<string>();

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.Color = new Aesthetic<T, string, string>(selector, scale, false, null);

		return context;
	}

	/// <summary>
	/// Maps a selector to fill colors; geoms consume it via fillBy or inherit.
	/// </summary>
	/// <param name="selector">Value mapped to a fill per item.</param>
	/// <param name="palette">Colors assigned to the selector’s distinct values, in sort order.</param>
	/// <param name="guide">Show a legend entry for this scale.</param>
	/// <param name="name">Legend title.</param>
	public static PlotContext<T, TX, TY> Scale_Fill_Discrete<T, TX, TY, TKey>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TKey> selector,
	  Palettes.Discrete<TKey, string> palette,
	  bool guide = true,
	  string? name = null)
	  where TX : struct
	  where TY : struct
	  where TKey : notnull
	{
		var scale = new FillDiscrete<TKey>(palette);

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.Fill = new Aesthetic<T, TKey, string>(selector, scale, guide, name);

		return context;
	}

	/// <summary>
	/// Maps a selector to fill colors; geoms consume it via fillBy or inherit.
	/// </summary>
	/// <param name="selector">Value mapped to a fill per item.</param>
	/// <param name="palette">Colors assigned to the selector’s distinct values, in sort order.</param>
	/// <param name="direction">Palette direction: 1 forward, −1 reversed.</param>
	/// <param name="guide">Show a legend entry for this scale.</param>
	/// <param name="name">Legend title.</param>
	public static PlotContext<T, TX, TY> Scale_Fill_Discrete<T, TX, TY, TKey>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TKey> selector,
	  string[] palette,
	  int direction = 1,
	  bool guide = true,
	  string? name = null)
	  where TX : struct
	  where TY : struct
	  where TKey : notnull
	{
		var scale = new FillDiscrete<TKey>(palette, direction);

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.Fill = new Aesthetic<T, TKey, string>(selector, scale, guide, name);

		return context;
	}

	/// <summary>
	/// Maps a numeric selector onto a binned gradient; renders a colorbar legend.
	/// </summary>
	/// <param name="selector">Value mapped to a fill per item.</param>
	/// <param name="palette">Gradient stops, interpolated across the bins.</param>
	/// <param name="m">Number of gradient bins.</param>
	/// <param name="formatter">Colorbar tick formatter; defaults to invariant 0.##.</param>
	/// <param name="guide">Show a legend entry for this scale.</param>
	/// <param name="name">Legend title.</param>
	public static PlotContext<T, TX, TY> Scale_Fill_Continuous<T, TX, TY>(this PlotContext<T, TX, TY> context,
	  Func<T, double> selector,
	  string[] palette,
	  int m = 5,
	  IFormatter<double>? formatter = null,
	  bool guide = true,
	  string? name = null)
	  where TX : struct
	  where TY : struct
	{
		var scale = new FillContinuous(palette, m, formatter);

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.Fill = new Aesthetic<T, double, string>(selector, scale, guide, name);

		return context;
	}

	/// <summary>
	/// Uses the selector’s value directly as the fill; no scale, no legend.
	/// </summary>
	/// <param name="selector">Fill per item (any css color).</param>
	public static PlotContext<T, TX, TY> Scale_Fill_Identity<T, TX, TY>(this PlotContext<T, TX, TY> context, Func<T, string> selector)
	  where TX : struct
	  where TY : struct
	{
		var scale = new Scales.Identity<string>();

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.Fill = new Aesthetic<T, string, string>(selector, scale, false, null);

		return context;
	}

	/// <summary>
	/// Maps a numeric selector to point sizes.
	/// </summary>
	/// <param name="selector">Value mapped to a size per item.</param>
	/// <param name="formatter">Legend label formatter; defaults to invariant 0.##.</param>
	/// <param name="limits">Input clamp for the mapping.</param>
	/// <param name="range">Output size range in pixels.</param>
	/// <param name="oob">Map out-of-range values to the nearest edge instead of dropping them.</param>
	/// <param name="guide">Show a legend entry for this scale.</param>
	/// <param name="name">Legend title.</param>
	public static PlotContext<T, TX, TY> Scale_Size_Continuous<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, double> selector,
	  (double min, double max)? limits = null,
	  (double min, double max)? range = null,
	  bool oob = false,
	  bool guide = true,
	  string? name = null,
	  IFormatter<double>? formatter = null)
	  where TX : struct
	  where TY : struct
	{
		var scale = new SizeContinuous(limits, range, oob, formatter);

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.Size = new Aesthetic<T, double, double>(selector, scale, guide, name);

		return context;
	}

	/// <summary>
	/// Uses the selector’s value directly as the size in pixels; no scale, no legend.
	/// </summary>
	/// <param name="selector">Size per item, in pixels.</param>
	public static PlotContext<T, TX, TY> Scale_Size_Identity<T, TX, TY>(this PlotContext<T, TX, TY> context, Func<T, double> selector)
	  where TX : struct
	  where TY : struct
	{
		var scale = new Scales.Identity<double>();

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.Size = new Aesthetic<T, double, double>(selector, scale, false, null);

		return context;
	}

	/// <summary>
	/// Maps a selector to dash patterns; geoms consume it via lineTypeBy or inherit.
	/// </summary>
	/// <param name="selector">Value mapped to a dash pattern per item.</param>
	/// <param name="palette">Dash patterns assigned to distinct values, in sort order; null cycles the built-in set.</param>
	/// <param name="guide">Show a legend entry for this scale.</param>
	/// <param name="name">Legend title.</param>
	public static PlotContext<T, TX, TY> Scale_LineType_Discrete<T, TX, TY, TKey>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TKey> selector,
	  Palettes.Discrete<TKey, LineType> palette,
	  bool guide = true,
	  string? name = null)
	  where TX : struct
	  where TY : struct
	  where TKey : notnull
	{
		var scale = new LineTypeDiscrete<TKey>(palette);

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.LineType = new Aesthetic<T, TKey, LineType>(selector, scale, guide, name);

		return context;
	}

	/// <summary>
	/// Maps a selector to dash patterns; geoms consume it via lineTypeBy or inherit.
	/// </summary>
	/// <param name="selector">Value mapped to a dash pattern per item.</param>
	/// <param name="palette">Dash patterns assigned to distinct values, in sort order; null cycles the built-in set.</param>
	/// <param name="direction">Palette direction: 1 forward, −1 reversed.</param>
	/// <param name="guide">Show a legend entry for this scale.</param>
	/// <param name="name">Legend title.</param>
	public static PlotContext<T, TX, TY> Scale_LineType_Discrete<T, TX, TY, TKey>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, TKey> selector,
	  LineType[]? palette = null,
	  int direction = 1,
	  bool guide = true,
	  string? name = null)
	  where TX : struct
	  where TY : struct
	  where TKey : notnull
	{
		var scale = new LineTypeDiscrete<TKey>(palette, direction);

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.LineType = new Aesthetic<T, TKey, LineType>(selector, scale, guide, name);

		return context;
	}

	/// <summary>
	/// Uses the selector’s value directly as the dash pattern; no scale, no legend.
	/// </summary>
	/// <param name="selector">Dash pattern per item.</param>
	public static PlotContext<T, TX, TY> Scale_LineType_Identity<T, TX, TY>(this PlotContext<T, TX, TY> context, Func<T, LineType> selector)
	  where TX : struct
	  where TY : struct
	{
		var scale = new Scales.Identity<LineType>();

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.LineType = new Aesthetic<T, LineType, LineType>(selector, scale, false, null);

		return context;
	}

	/// <summary>
	/// Splits the plot into one panel per distinct key, wrapped into a grid.
	/// </summary>
	/// <param name="selector">Facet key per item.</param>
	/// <param name="freeX">Independent x scale per panel instead of shared.</param>
	/// <param name="freeY">Independent y scale per panel instead of shared.</param>
	/// <param name="nrows">Fix the row count; default derives from the key count.</param>
	/// <param name="ncolumns">Fix the column count; default derives from the key count.</param>
	public static PlotContext<T, TX, TY> Facet_Wrap<T, TX, TY, TKey>(this PlotContext<T, TX, TY> context, Func<T, TKey> selector, bool freeX = false, bool freeY = false, int? nrows = null, int? ncolumns = null)
	  where TX : struct
	  where TY : struct
	{
		if (nrows is <= 0)
		{
			throw new GGNetUserException("nrows must be positive");
		}

		if (ncolumns is <= 0)
		{
			throw new GGNetUserException("ncolumns must be positive");
		}

		context.Faceting = new Faceting1D<T, TKey>(selector, freeX, freeY, nrows, ncolumns);

		return context;
	}

	/// <summary>
	/// Splits the plot into a panel grid over two keys.
	/// </summary>
	/// <param name="row">Row key per item.</param>
	/// <param name="column">Column key per item.</param>
	/// <param name="freeX">Independent x scale per panel instead of shared.</param>
	/// <param name="freeY">Independent y scale per panel instead of shared.</param>
	public static PlotContext<T, TX, TY> Facet_Grid<T, TX, TY, TRow, TColumn>(this PlotContext<T, TX, TY> context, Func<T, TRow> row, Func<T, TColumn> column, bool freeX = false, bool freeY = false)
	  where TX : struct
	  where TY : struct
	{
		context.Faceting = new Faceting2D<T, TRow, TColumn>(row, column, freeX, freeY);

		return context;
	}

	/// <summary>
	/// Finishes the chain: attaches the style and returns the context ready to render.
	/// </summary>
	/// <param name="style">Prebuilt style; null takes <c>Style.Default(axisY, legend)</c>.</param>
	/// <param name="axisY">Y-axis side when <paramref name="style"/> is null.</param>
	/// <param name="legend">Legend position when <paramref name="style"/> is null.</param>
	public static PlotContext<T, TX, TY> Style<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Style? style = null,
	  Position axisY = Left,
	  Position legend = Right)
	  where TX : struct
	  where TY : struct
	{
		context.Style = style ?? GGNet.Style.Default(axisY, legend);

		return context;
	}
}

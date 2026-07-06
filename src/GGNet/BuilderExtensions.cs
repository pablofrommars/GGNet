namespace GGNet;

using Elements;
using Exceptions;
using Facets;
using Formats;

using Geoms;

using Scales;
using Transformations;

using static Position;
using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
	public static PlotContext<T, LocalDate, TY> Scale_X_Discrete_Date<T, TY>(
	  this PlotContext<T, LocalDate, TY> context,
	  (LocalDate? min, LocalDate? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null)
	  where TY : struct
	{
		context.Positions.X.Factory = () => new DiscretDates(null, limits, expand);

		return context;
	}

	public static PlotContext<T, LocalDateTime, TY> Scale_X_Discrete_DateTime<T, TY>(
	  this PlotContext<T, LocalDateTime, TY> context,
	  (LocalDateTime? min, LocalDateTime? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null)
	  where TY : struct
	{
		context.Positions.X.Factory = () => new DateTimePosition(null, limits, expand);

		return context;
	}

	public static PlotContext<T, Instant, TY> Scale_X_Instant<T, TY>(
	  this PlotContext<T, Instant, TY> context,
	  Instant? start = null, Instant? end = null,
	  string format = "H:mm:ss",
	  string timezone = "UTC")
	  where TY : struct
	{
		context.Positions.X.Factory = () => new InstantPosition(start, end, format, timezone);

		return context;
	}

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

	public static PlotContext<T, double, TY> Scale_X_Continuous<T, TY>(
	  this PlotContext<T, double, TY> context,
	  string? format,
	  ITransformation<double>? transformation = null,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  bool hide = false,
	  bool includeMinorBreaks = true)
	  where TY : struct
	{
		context.Scale_X_Continuous(transformation, limits, expand, !string.IsNullOrEmpty(format) ? new DoubleFormatter(format) : null, hide, includeMinorBreaks);

		return context;
	}

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

	public static PlotContext<T, double, TY> Scale_X_Discrete<T, TY>(
	 this PlotContext<T, double, TY> context,
	  string? format = null,
	 (double? min, double? max)? limits = null,
	 (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	 double offset = 0.0,
	 bool hide = false)
	 where TY : struct
	{
		context.Scale_X_Discrete(limits, expand, !string.IsNullOrEmpty(format) ? new DoubleFormatter(format) : null, offset, hide);

		return context;
	}

	public static PlotContext<T, double, TY> XLim<T, TY>(this PlotContext<T, double, TY> context, double? min = null, double? max = null)
	  where TY : struct
	{
		if (context.Positions.X.Factory is null)
		{
			context.Scale_X_Continuous(format: "N2");
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

	public static PlotContext<T, TX, double> Scale_Y_Continuous<T, TX>(
	   this PlotContext<T, TX, double> context,
	   string? format,
	   ITransformation<double>? transformation = null,
	   (double? min, double? max)? limits = null,
	   (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	   bool hide = false,
	  bool includeMinorBreaks = true)
	   where TX : struct
	{
		context.Positions.Y.Factory = () => new Extended(transformation, limits, expand, !string.IsNullOrEmpty(format) ? new DoubleFormatter(format) : null, hide, includeMinorBreaks);

		return context;
	}

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

	public static PanelFactory<T, TX, double> Scale_Y_Continuous<T, TX>(
	  this PanelFactory<T, TX, double> panel,
	  string? format,
	  ITransformation<double>? transformation = null,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	   bool hide = false,
	  bool includeMinorBreaks = true)
		  where TX : struct
	{
		panel.Scale_Y_Continuous(transformation, limits, expand, !string.IsNullOrEmpty(format) ? new DoubleFormatter(format) : null, hide, includeMinorBreaks);

		return panel;
	}

	public static PlotContext<T, double, TY> Scale_X_Sqrt<T, TY>(
	  this PlotContext<T, double, TY> context,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  string? format = null,
	  bool hide = false,
	  bool includeMinorBreaks = true)
	  where TY : struct
	  => context.Scale_X_Continuous(format, Sqrt.Instance, limits, expand, hide, includeMinorBreaks);

	public static PlotContext<T, double, TY> Scale_X_Log10<T, TY>(
	  this PlotContext<T, double, TY> context,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  string? format = null)
	  where TY : struct
	{
		context.Positions.X.Factory = () => new Scales.Log10(limits, expand, !string.IsNullOrEmpty(format) ? new DoubleFormatter(format) : null);

		return context;
	}

	public static PanelFactory<T, TX, double> Scale_Y_Sqrt<T, TX>(
	  this PanelFactory<T, TX, double> panel,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  string? format = null,
	  bool hide = false)
	  where TX : struct
	  => panel.Scale_Y_Continuous(format, Sqrt.Instance, limits, expand, hide);

	public static PlotContext<T, TX, double> Scale_Y_Sqrt<T, TX>(
	  this PlotContext<T, TX, double> context,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  string? format = null,
	  bool hide = false)
	  where TX : struct
	  => context.Scale_Y_Continuous(format, Sqrt.Instance, limits, expand, hide);

	public static PanelFactory<T, TX, double> Scale_Y_Log10<T, TX>(
	  this PanelFactory<T, TX, double> panel,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  string? format = null)
	  where TX : struct
	{
		panel.Y = () => new Scales.Log10(limits, expand, !string.IsNullOrEmpty(format) ? new DoubleFormatter(format) : null);

		return panel;
	}

	public static PlotContext<T, TX, double> Scale_Y_Log10<T, TX>(
	  this PlotContext<T, TX, double> context,
	  (double? min, double? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  string? format = null)
	  where TX : struct
	{
		context.Positions.Y.Factory = () => new Scales.Log10(limits, expand, !string.IsNullOrEmpty(format) ? new DoubleFormatter(format) : null);

		return context;
	}

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

	public static PlotContext<T, TX, double> Scale_Y_Discrete<T, TX>(
	 this PlotContext<T, TX, double> context,
	  string? format,
	 (double? min, double? max)? limits = null,
	 (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	 double offset = 0.0,
	 bool hide = false)
	 where TX : struct
	{
		context.Scale_Y_Discrete(limits, expand, !string.IsNullOrEmpty(format) ? new DoubleFormatter(format) : null, offset, hide);

		return context;
	}

	public static PlotContext<T, double, TY> Scale_Longitude<T, TY>(
	  this PlotContext<T, double, TY> context,
	  (double? min, double? max)? limits = null)
	  where TY : struct
	  => context.Scale_X_Continuous(null, limits ?? (-180, 180), (0, 0, 0, 0), Longitude.Instance);

	public static PanelFactory<T, TX, double> Scale_Latitude<T, TX>(
	   this PanelFactory<T, TX, double> panel,
	   (double? min, double? max)? limits = null)
	   where TX : struct
	   => panel.Scale_Y_Continuous(null, limits ?? (-90, 90), (0, 0, 0, 0), Latitude.Instance);

	public static PlotContext<T, TX, double> Scale_Latitude<T, TX>(
	  this PlotContext<T, TX, double> context,
	  (double? min, double? max)? limits = null)
	  where TX : struct
	  => context.Scale_Y_Continuous(null, limits ?? (-90, 90), (0, 0, 0, 0), Latitude.Instance);

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

	public static PanelFactory<T, TX, double> YLim<T, TX>(this PanelFactory<T, TX, double> panel, double? min = null, double? max = null)
	  where TX : struct
	{
		if (panel.Y is null)
		{
			panel.Scale_Y_Continuous(format: "N2");
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

	public static PlotContext<T, TX, TY> Panel<T, TX, TY>(this PlotContext<T, TX, TY> context, Func<PanelFactory<T, TX, TY>, PanelFactory<T, TX, TY>> factory, double width = 1.0, double height = 1.0, Func<MouseEventArgs, Task>? onClick = null)
	  where TX : struct
	  where TY : struct
	{
		var panel = factory(new PanelFactory<T, TX, TY>(context, width, height, onClick));

		context.PanelFactories.Add(panel);

		return context;
	}

	public static PlotContext<T, TX, TY> Title<T, TX, TY>(this PlotContext<T, TX, TY> context, [StringSyntax("Markdown")] string title)
	  where TX : struct
	  where TY : struct
	{
		context.Title = Markdown.Text(title);

		return context;
	}

	public static PlotContext<T, TX, TY> SubTitle<T, TX, TY>(this PlotContext<T, TX, TY> context, [StringSyntax("Markdown")] string subtitle)
	  where TX : struct
	  where TY : struct
	{
		context.SubTitle = Markdown.Text(subtitle);

		return context;
	}

	public static PlotContext<T, TX, TY> Caption<T, TX, TY>(this PlotContext<T, TX, TY> context, [StringSyntax("Markdown")] string caption)
	  where TX : struct
	  where TY : struct
	{
		context.Caption = Markdown.Text(caption);

		return context;
	}

	public static PlotContext<T, TX, TY> XLab<T, TX, TY>(this PlotContext<T, TX, TY> context, [StringSyntax("Markdown")] string xlab)
	  where TX : struct
	  where TY : struct
	{
		context.XLab = Markdown.Text(xlab);

		return context;
	}

	public static PanelFactory<T, TX, TY> YLab<T, TX, TY>(this PanelFactory<T, TX, TY> panel, [StringSyntax("Markdown")] string ylab)
	  where TX : struct
	  where TY : struct
	{
		panel.YLab = Markdown.Text(ylab);

		return panel;
	}

	public static PlotContext<T, TX, TY> YLab<T, TX, TY>(this PlotContext<T, TX, TY> context, [StringSyntax("Markdown")] string ylab)
	  where TX : struct
	  where TY : struct
	{

		context.Default_Panel().YLab(ylab);

		return context;
	}

	public static PlotContext<T, TX, TY> Flip<T, TX, TY>(this PlotContext<T, TX, TY> context)
	  where TX : struct
	  where TY : struct
	{
		context.Flip = true;

		return context;
	}

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

	public static PlotContext<T, TX, TY> Scale_Color_Identity<T, TX, TY>(this PlotContext<T, TX, TY> context, Func<T, string> selector)
	  where TX : struct
	  where TY : struct
	{
		var scale = new Scales.Identity<string>();

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.Color = new Aesthetic<T, string, string>(selector, scale, false, null);

		return context;
	}

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

	public static PlotContext<T, TX, TY> Scale_Fill_Continuous<T, TX, TY>(this PlotContext<T, TX, TY> context,
	  Func<T, double> selector,
	  string[] palette,
	  int m = 5,
	  string format = "0.##",
	  bool guide = true,
	  string? name = null)
	  where TX : struct
	  where TY : struct
	{
		var scale = new FillContinuous(palette, m, format);

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.Fill = new Aesthetic<T, double, string>(selector, scale, guide, name);

		return context;
	}

	public static PlotContext<T, TX, TY> Scale_Fill_Identity<T, TX, TY>(this PlotContext<T, TX, TY> context, Func<T, string> selector)
	  where TX : struct
	  where TY : struct
	{
		var scale = new Scales.Identity<string>();

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.Fill = new Aesthetic<T, string, string>(selector, scale, false, null);

		return context;
	}

	public static PlotContext<T, TX, TY> Scale_Size_Continuous<T, TX, TY>(
	  this PlotContext<T, TX, TY> context,
	  Func<T, double> selector,
	  (double min, double max)? limits = null,
	  (double min, double max)? range = null,
	  bool oob = false,
	  bool guide = true,
	  string? name = null,
	  string format = "0.##")
	  where TX : struct
	  where TY : struct
	{
		var scale = new SizeContinuous(limits, range, oob, format);

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.Size = new Aesthetic<T, double, double>(selector, scale, guide, name);

		return context;
	}

	public static PlotContext<T, TX, TY> Scale_Size_Identity<T, TX, TY>(this PlotContext<T, TX, TY> context, Func<T, double> selector)
	  where TX : struct
	  where TY : struct
	{
		var scale = new Scales.Identity<double>();

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.Size = new Aesthetic<T, double, double>(selector, scale, false, null);

		return context;
	}

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

	public static PlotContext<T, TX, TY> Scale_LineType_Identity<T, TX, TY>(this PlotContext<T, TX, TY> context, Func<T, LineType> selector)
	  where TX : struct
	  where TY : struct
	{
		var scale = new Scales.Identity<LineType>();

		context.Aesthetics.Scales.Add(scale);

		context.Aesthetics.LineType = new Aesthetic<T, LineType, LineType>(selector, scale, false, null);

		return context;
	}

	public static PlotContext<T, TX, TY> Facet_Wrap<T, TX, TY, TKey>(this PlotContext<T, TX, TY> context, Func<T, TKey> selector, bool freeX = false, bool freeY = false, int? nrows = null, int? ncolumns = null)
	  where TX : struct
	  where TY : struct
	{
		context.Faceting = new Faceting1D<T, TKey>(selector, freeX, freeY, nrows, ncolumns);

		return context;
	}

	public static PlotContext<T, TX, TY> Facet_Grid<T, TX, TY, TRow, TColumn>(this PlotContext<T, TX, TY> context, Func<T, TRow> row, Func<T, TColumn> column, bool freeX = false, bool freeY = false)
	  where TX : struct
	  where TY : struct
	{
		context.Faceting = new Faceting2D<T, TRow, TColumn>(row, column, freeX, freeY);

		return context;
	}

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

namespace GGNet;

using Elements;
using Exceptions;
using Facets;
using Formats;
using Geoms;
using Geoms.Point;
using Geoms.Line;
using Geoms.Bar;
using Geoms.Segment;
using Geoms.Area;
using Geoms.Ribbon;
using Geoms.ErrorBar;
using Geoms.Text;
using Geoms.VLine;
using Geoms.HLine;
using Geoms.ABLine;
using Geoms.OHLC;
using Geoms.Candlestick;
using Geoms.Volume;
using Geoms.Hex;
using Geoms.RidgeLine;
using Geoms.Violin;
using Geoms.Map;
using Scales;
using Transformations;

using static Position;
using static Anchor;
using static LineType;

public static class Plot
{
  private static PlotContext<T, TX, TY> NewBase<T, TX, TY>(Source<T>? source, Func<T, TX>? x, Func<T, TY>? y)
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

  public static PlotContext<T, LocalDate, double> New<T>(Source<T> source, Func<T, LocalDate> x, Func<T, double>? y = null) => NewBase(source, x, y);

  public static PlotContext<T, LocalDate, double> New<T>(IEnumerable<T> items, Func<T, LocalDate> x, Func<T, double>? y = null) => NewBase(new Source<T>(items), x, y);

  public static PlotContext<T, LocalDateTime, double> New<T>(Source<T> source, Func<T, LocalDateTime> x, Func<T, double>? y = null) => NewBase(source, x, y);

  public static PlotContext<T, LocalDateTime, double> New<T>(IEnumerable<T> items, Func<T, LocalDateTime> x, Func<T, double>? y = null) => NewBase(new Source<T>(items), x, y);

  public static PlotContext<T, Instant, double> New<T>(Source<T> source, Func<T, Instant> x, Func<T, double>? y = null) => NewBase(source, x, y);

  public static PlotContext<T, Instant, double> New<T>(IEnumerable<T> items, Func<T, Instant> x, Func<T, double>? y = null) => NewBase(new Source<T>(items), x, y);

  public static PlotContext<T, TX, double> New<T, TX>(Source<T> source, Func<T, TX> x, Func<T, double>? y = null)
    where TX : struct, Enum
  {
    return NewBase(source, x, y);
  }

  public static PlotContext<T, TX, double> New<T, TX>(IEnumerable<T> items, Func<T, TX> x, Func<T, double>? y = null)
     where TX : struct, Enum
  {
    return NewBase(new Source<T>(items), x, y);
  }

  public static PlotContext<T, TX, TY> New<T, TX, TY>(Source<T> source, Func<T, TX> x, Func<T, TY> y)
    where TX : struct, Enum
    where TY : struct, Enum
  {
    return NewBase(source, x, y);
  }

  public static PlotContext<T, TX, TY> New<T, TX, TY>(IEnumerable<T> items, Func<T, TX> x, Func<T, TY> y)
    where TX : struct, Enum
    where TY : struct, Enum
  {
    return NewBase(new Source<T>(items), x, y);
  }

  public static PlotContext<T, double, TY> New<T, TY>(Source<T> source, Func<T, double> x, Func<T, TY> y)
    where TY : struct, Enum
  {
    return NewBase(source, x, y);
  }

  public static PlotContext<T, double, TY> New<T, TY>(IEnumerable<T> items, Func<T, double> x, Func<T, TY> y)
    where TY : struct, Enum
  {
    return NewBase(new Source<T>(items), x, y);
  }

  public static PlotContext<T, double, double> New<T>(Source<T> source, Func<T, double>? x = null, Func<T, double>? y = null) => NewBase(source, x, y);

  public static PlotContext<T, double, double> New<T>(IEnumerable<T> items, Func<T, double>? x = null, Func<T, double>? y = null) => NewBase(new Source<T>(items), x, y);

  public static PlotContext<IWaiver, double, double> New() => NewBase<IWaiver, double, double>(null, null, null);

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
    Instant start, Instant end,
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
    IFormatter<double>? formatter = null)
    where TY : struct
  {
    context.Positions.X.Factory = () => new Extended(transformation, limits, expand, formatter);

    return context;
  }

  public static PlotContext<T, double, TY> Scale_X_Continuous<T, TY>(
    this PlotContext<T, double, TY> context,
    string? format,
    ITransformation<double>? transformation = null,
    (double? min, double? max)? limits = null,
    (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null)
    where TY : struct
  {
    context.Scale_X_Continuous(transformation, limits, expand, !string.IsNullOrEmpty(format) ? new DoubleFormatter(format) : null);

    return context;
  }

  public static PlotContext<T, TX, TY> Scale_X_Discrete<T, TX, TY>(
     this PlotContext<T, TX, TY> context,
     (TX? min, TX? max)? limits = null,
     (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
     IFormatter<TX>? formatter = null)
     where TX : struct
     where TY : struct
  {
    context.Positions.X.Factory = () => new DiscretePosition<TX>(null, limits, expand, formatter);

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
    IFormatter<double>? formatter = null)
    where TX : struct
  {
    context.Positions.Y.Factory = () => new Extended(transformation, limits, expand, formatter);

    return context;
  }

  public static PlotContext<T, TX, double> Scale_Y_Continuous<T, TX>(
     this PlotContext<T, TX, double> context,
     string? format,
     ITransformation<double>? transformation = null,
     (double? min, double? max)? limits = null,
     (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null)
     where TX : struct
  {
    context.Positions.Y.Factory = () => new Extended(transformation, limits, expand, !string.IsNullOrEmpty(format) ? new DoubleFormatter(format) : null);

    return context;
  }

  public static PanelFactory<T, TX, double> Scale_Y_Continuous<T, TX>(
    this PanelFactory<T, TX, double> panel,
     ITransformation<double>? transformation = null,
    (double? min, double? max)? limits = null,
    (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
    IFormatter<double>? formatter = null)
    where TX : struct
  {
    panel.Y = () => new Extended(transformation, limits, expand, formatter);

    return panel;
  }

  public static PanelFactory<T, TX, double> Scale_Y_Continuous<T, TX>(
    this PanelFactory<T, TX, double> panel,
       string? format,
    ITransformation<double>? transformation = null,
       (double? min, double? max)? limits = null,
       (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null)
         where TX : struct
  {
    panel.Scale_Y_Continuous(transformation, limits, expand, !string.IsNullOrEmpty(format) ? new DoubleFormatter(format) : null);

    return panel;
  }

  public static PlotContext<T, double, TY> Scale_X_Sqrt<T, TY>(
    this PlotContext<T, double, TY> context,
    (double? min, double? max)? limits = null,
    (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
    string? format = null)
    where TY : struct
    => context.Scale_X_Continuous(format, Sqrt.Instance, limits, expand);

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
    string? format = null)
    where TX : struct
    => panel.Scale_Y_Continuous(format, Sqrt.Instance, limits, expand);

  public static PlotContext<T, TX, double> Scale_Y_Sqrt<T, TX>(
    this PlotContext<T, TX, double> context,
    (double? min, double? max)? limits = null,
    (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
    string? format = null)
    where TX : struct
    => context.Scale_Y_Continuous(format, Sqrt.Instance, limits, expand);

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
     IFormatter<TY>? formatter = null)
     where TX : struct
     where TY : struct
  {
    context.Positions.Y.Factory = () => new DiscretePosition<TY>(null, limits, expand, formatter);

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

  public static PlotContext<T, TX, TY> Panel<T, TX, TY>(this PlotContext<T, TX, TY> context, Func<PanelFactory<T, TX, TY>, PanelFactory<T, TX, TY>> factory, double width = 1.0, double height = 1.0)
    where TX : struct
    where TY : struct
  {
    var panel = factory(new PanelFactory<T, TX, TY>(context, width, height));

    context.PanelFactories.Add(panel);

    return context;
  }

  public static PlotContext<T, TX, TY> Title<T, TX, TY>(this PlotContext<T, TX, TY> context, string title)
    where TX : struct
    where TY : struct
  {
    context.Title = title;

    return context;
  }

  public static PlotContext<T, TX, TY> SubTitle<T, TX, TY>(this PlotContext<T, TX, TY> context, string subtitle)
    where TX : struct
    where TY : struct
  {
    context.SubTitle = subtitle;

    return context;
  }

  public static PlotContext<T, TX, TY> Caption<T, TX, TY>(this PlotContext<T, TX, TY> context, string caption)
    where TX : struct
    where TY : struct
  {
    context.Caption = caption;

    return context;
  }

  public static PlotContext<T, TX, TY> XLab<T, TX, TY>(this PlotContext<T, TX, TY> context, string xlab)
    where TX : struct
    where TY : struct
  {
    context.XLab = xlab;

    return context;
  }

  public static PanelFactory<T, TX, TY> YLab<T, TX, TY>(this PanelFactory<T, TX, TY> panel, string ylab)
    where TX : struct
    where TY : struct
  {
    panel.YLab = ylab;

    return panel;
  }

  public static PlotContext<T, TX, TY> YLab<T, TX, TY>(this PlotContext<T, TX, TY> context, string ylab)
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

  internal static PanelFactory<T1, TX1, TY1> Add<T1, TX1, TY1, T2, TX2, TY2>(this PanelFactory<T1, TX1, TY1> panel, Func<Geom<T2, TX2, TY2>> builder)
    where TX1 : struct
    where TY1 : struct
    where TX2 : struct
    where TY2 : struct
  {
    panel.Add((p, f) =>
    {
      var geom = builder();

      geom.Init(p, f);

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Point<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, double>? _size = null,
    IAestheticMapping<T2, string>? _color = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    bool animation = false,
    double size = 5, string color = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    panel.Add(() =>
    {
      var geom = new Point<T2, TX2, TY2>(source, x, y, _size, _color, tooltip, animation, scale, inherit)
      {
        OnClick = onclick,
        OnMouseOver = onmouseover,
        OnMouseOut = onmouseout,
        Aesthetic = new()
        {
          Radius = size,
          Fill = color,
          Alpha = alpha,
        }
      };

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Point<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, double>? _size = null,
    IAestheticMapping<T2, string>? _color = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    bool animation = false,
    double size = 5, string color = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return panel.Geom_Point(new Source<T2>(source), x, y, _size, _color, onclick, onmouseover, onmouseout, tooltip, animation, size, color, alpha, scale, inherit);
  }

  public static PlotContext<T1, TX1, TY1> Geom_Point<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, double>? _size = null,
    IAestheticMapping<T2, string>? _color = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    bool animation = false,
    double size = 5, string color = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_Point(source, x, y, _size, _color, onclick, onmouseover, onmouseout, tooltip, animation, size, color, alpha, scale, inherit);

    return context;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Point<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, double>? _size = null,
    IAestheticMapping<T2, string>? _color = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    bool animation = false,
    double size = 5, string color = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return context.Geom_Point(new Source<T2>(source), x, y, _size, _color, onclick, onmouseover, onmouseout, tooltip, animation, size, color, alpha, scale, inherit);
  }

  public static PanelFactory<T, TX, TY> Geom_Point<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    IAestheticMapping<T, double>? _size = null,
    IAestheticMapping<T, string>? _color = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, string>? tooltip = null,
    bool animation = false,
    double size = 5, string color = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    return Geom_Point(panel, panel.Context.Source!, x, y, _size, _color, onclick, onmouseover, onmouseout, tooltip, animation, size, color, alpha, scale, inherit);
  }

  public static PlotContext<T, TX, TY> Geom_Point<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    IAestheticMapping<T, double>? _size = null,
    IAestheticMapping<T, string>? _color = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, string>? tooltip = null,
    bool animation = false,
    double size = 5, string color = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_Point(x, y, _size, _color, onclick, onmouseover, onmouseout, tooltip, animation, size, color, alpha, scale, inherit);

    return context;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Line<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, string>? _color = null,
    IAestheticMapping<T2, LineType>? _lineType = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    panel.Add(() =>
    {
      var geom = new Line<T2, TX2, TY2>(source, x, y, _color, _lineType, tooltip, scale, inherit)
      {
        Aesthetic = new()
        {
          Width = width,
          Fill = color,
          Alpha = alpha,
          LineType = lineType
        },
        OnClick = onclick,
        OnMouseOver = onmouseover,
        OnMouseOut = onmouseout
      };

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Line<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, string>? _color = null,
    IAestheticMapping<T2, LineType>? _lineType = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return panel.Geom_Line(new Source<T2>(source), x, y, _color, _lineType, onclick, onmouseover, onmouseout, tooltip, width, color, alpha, lineType, scale, inherit);
  }

  public static PlotContext<T1, TX1, TY1> Geom_Line<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, string>? _color = null,
    IAestheticMapping<T2, LineType>? _lineType = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_Line(source, x, y, _color, _lineType, onclick, onmouseover, onmouseout, tooltip, width, color, alpha, lineType, scale, inherit);

    return context;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Line<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, string>? _color = null,
    IAestheticMapping<T2, LineType>? _lineType = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return context.Geom_Line(new Source<T2>(source), x, y, _color, _lineType, onclick, onmouseover, onmouseout, tooltip, width, color, alpha, lineType, scale, inherit);
  }

  public static PanelFactory<T, TX, TY> Geom_Line<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    IAestheticMapping<T, string>? _color = null,
    IAestheticMapping<T, LineType>? _lineType = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, string>? tooltip = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    return Geom_Line(panel, panel.Context.Source!, x, y, _color, _lineType, onclick, onmouseover, onmouseout, tooltip, width, color, alpha, lineType, scale, inherit);
  }

  public static PlotContext<T, TX, TY> Geom_Line<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    IAestheticMapping<T, string>? _color = null,
    IAestheticMapping<T, LineType>? _lineType = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, string>? tooltip = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_Line(x, y, _color, _lineType, onclick, onmouseover, onmouseout, tooltip, width, color, alpha, lineType, scale, inherit);

    return context;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Bar<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    string strokeColor = "inherit", double strokeWidth = 0.0,
    PositionAdjustment position = PositionAdjustment.Stack,
    double width = 0.9,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    panel.Add(() =>
    {
      var geom = new Bar<T2, TX2, TY2>(source, x, y, _fill, tooltip, position, width, animation, scale, inherit)
      {
        OnClick = onclick,
        OnMouseOver = onmouseover,
        OnMouseOut = onmouseout,
        Aesthetic = new()
        {
          Fill = fill,
          Alpha = alpha,
          Color = strokeColor,
          Width = strokeWidth,
        }
      };

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Bar<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    string strokeColor = "inherit", double strokeWidth = 0.0,
    PositionAdjustment position = PositionAdjustment.Stack,
    double width = 0.9,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return panel.Geom_Bar(new Source<T2>(source), x, y, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, strokeColor, strokeWidth, position, width, animation, scale, inherit);
  }

  public static PlotContext<T1, TX1, TY1> Geom_Bar<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    string strokeColor = "inherit", double strokeWidth = 0.0,
    PositionAdjustment position = PositionAdjustment.Stack,
    double width = 0.9,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_Bar(source, x, y, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, strokeColor, strokeWidth, position, width, animation, scale, inherit);

    return context;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Bar<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    string strokeColor = "inherit", double strokeWidth = 0.0,
    PositionAdjustment position = PositionAdjustment.Stack,
    double width = 0.9,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return context.Geom_Bar(new Source<T2>(source), x, y, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, strokeColor, strokeWidth, position, width, animation, scale, inherit);
  }

  public static PanelFactory<T, TX, TY> Geom_Bar<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    IAestheticMapping<T, string>? _fill = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    string strokeColor = "inherit", double strokeWidth = 0.0,
    PositionAdjustment position = PositionAdjustment.Stack,
    double width = 0.9,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    return Geom_Bar(panel, panel.Context.Source!, x, y, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, strokeColor, strokeWidth, position, width, animation, scale, inherit);
  }

  public static PlotContext<T, TX, TY> Geom_Bar<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    IAestheticMapping<T, string>? _fill = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    string strokeColor = "inherit", double strokeWidth = 0.0,
    PositionAdjustment position = PositionAdjustment.Stack,
    double width = 0.9,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_Bar(x, y, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, strokeColor, strokeWidth, position, width, animation, scale, inherit);

    return context;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Segment<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, TX2> x,
    Func<T2, TX2> xend,
    Func<T2, TY2> y,
    Func<T2, TY2> yend,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    panel.Add(() =>
    {
      var geom = new Segment<T2, TX2, TY2>(source, x, xend, y, yend)
      {
        Aesthetic = new()
        {
          Width = width,
          Fill = color,
          Alpha = alpha,
          LineType = lineType
        }
      };

      return geom;
    });

    return panel;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Segment<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX2> x,
    Func<T2, TX2> xend,
    Func<T2, TY2> y,
    Func<T2, TY2> yend,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_Segment(source, x, xend, y, yend, width, color, alpha, lineType);

    return context;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Segment<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    IEnumerable<T2> source,
    Func<T2, TX2> x,
    Func<T2, TX2> xend,
    Func<T2, TY2> y,
    Func<T2, TY2> yend,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
    => context.Geom_Segment(new Source<T2>(source), x, xend, y, yend, width, color, alpha, lineType);

  public static PanelFactory<T, TX, TY> Geom_Segment<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX> x,
    Func<T, TX> xend,
    Func<T, TY> y,
    Func<T, TY> yend,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid)
    where TX : struct
    where TY : struct
  {
    return Geom_Segment(panel, panel.Context.Source!, x, xend, y, yend, width, color, alpha, lineType);
  }

  public static PlotContext<T, TX, TY> Geom_Segment<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX> x,
    Func<T, TX> xend,
    Func<T, TY> y,
    Func<T, TY> yend,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_Segment(x, xend, y, yend, width, color, alpha, lineType);

    return context;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Area<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    PositionAdjustment position = PositionAdjustment.Identity,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    panel.Add(() =>
    {
      var geom = new Area<T2, TX2, TY2>(source, x, y, _fill, tooltip, position, scale, inherit)
      {
        OnClick = onclick,
        OnMouseOver = onmouseover,
        OnMouseOut = onmouseout,
        Aesthetic = new()
        {
          Fill = fill,
          Alpha = alpha
        }
      };

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Area<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    PositionAdjustment position = PositionAdjustment.Identity,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return panel.Geom_Area(new Source<T2>(source), x, y, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, position, scale, inherit);
  }

  public static PlotContext<T1, TX1, TY1> Geom_Area<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    PositionAdjustment position = PositionAdjustment.Identity,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_Area(source, x, y, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, position, scale, inherit);

    return context;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Area<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    PositionAdjustment position = PositionAdjustment.Identity,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return context.Geom_Area(new Source<T2>(source), x, y, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, position, scale, inherit);
  }

  public static PanelFactory<T, TX, TY> Geom_Area<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    IAestheticMapping<T, string>? _fill = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    PositionAdjustment position = PositionAdjustment.Identity,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    return Geom_Area(panel, panel.Context.Source!, x, y, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, position, scale, inherit);
  }

  public static PlotContext<T, TX, TY> Geom_Area<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    IAestheticMapping<T, string>? _fill = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    PositionAdjustment position = PositionAdjustment.Identity,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_Area(x, y, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, position, scale, inherit);

    return context;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Ribbon<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? ymin = null,
    Func<T2, TY2>? ymax = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    panel.Add(() =>
    {
      var geom = new Ribbon<T2, TX2, TY2>(source, x, ymin, ymax, _fill, tooltip, scale, inherit)
      {
        OnClick = onclick,
        OnMouseOver = onmouseover,
        OnMouseOut = onmouseout,
        Aesthetic = new()
        {
          Fill = fill,
          Alpha = alpha
        }
      };

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Ribbon<T1, TX1, TY1, T2, TX2, TY2>(
     this PanelFactory<T1, TX1, TY1> panel,
     IEnumerable<T2> source,
     Func<T2, TX2>? x = null,
     Func<T2, TY2>? ymin = null,
     Func<T2, TY2>? ymax = null,
     IAestheticMapping<T2, string>? _fill = null,
     Func<T2, MouseEventArgs, Task>? onclick = null,
     Func<T2, MouseEventArgs, Task>? onmouseover = null,
     Func<T2, MouseEventArgs, Task>? onmouseout = null,
     Func<T2, string>? tooltip = null,
     string fill = "#23d0fc", double alpha = 1.0,
     (bool x, bool y)? scale = null, bool inherit = true)
     where TX1 : struct
     where TX2 : struct
     where TY1 : struct
     where TY2 : struct
  {
    return panel.Geom_Ribbon(new Source<T2>(source), x, ymin, ymax, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, scale, inherit);
  }

  public static PlotContext<T1, TX1, TY1> Geom_Ribbon<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? ymin = null,
    Func<T2, TY2>? ymax = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_Ribbon(source, x, ymin, ymax, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, scale, inherit);

    return context;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Ribbon<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? ymin = null,
    Func<T2, TY2>? ymax = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return context.Geom_Ribbon(new Source<T2>(source), x, ymin, ymax, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, scale, inherit);
  }

  public static PanelFactory<T, TX, TY> Geom_Ribbon<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? ymin = null,
    Func<T, TY>? ymax = null,
    IAestheticMapping<T, string>? _fill = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    return Geom_Ribbon(panel, panel.Context.Source!, x, ymin, ymax, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, scale, inherit);
  }

  public static PlotContext<T, TX, TY> Geom_Ribbon<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? ymin = null,
    Func<T, TY>? ymax = null,
    IAestheticMapping<T, string>? _fill = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_Ribbon(x, ymin, ymax, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, scale, inherit);

    return context;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_ErrorBar<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, TY2>? ymin = null,
    Func<T2, TY2>? ymax = null,
    IAestheticMapping<T2, string>? _color = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    double radius = 5,
    PositionAdjustment position = PositionAdjustment.Identity,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    panel.Add(() =>
    {
      var geom = new ErrorBar<T2, TX2, TY2>(source, x, y, ymin, ymax, _color, tooltip, position, animation, scale, inherit)
      {
        OnClick = onclick,
        OnMouseOver = onmouseover,
        OnMouseOut = onmouseout,
        Line = new()
        {
          Width = width,
          Fill = color,
          Alpha = alpha,
          LineType = lineType
        },
        Circle = new()
        {
          Fill = color,
          Alpha = alpha,
          Radius = radius
        }
      };

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_ErrorBar<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, TY2>? ymin = null,
    Func<T2, TY2>? ymax = null,
    IAestheticMapping<T2, string>? _color = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    double radius = 5,
    PositionAdjustment position = PositionAdjustment.Identity,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return panel.Geom_ErrorBar(new Source<T2>(source), x, y, ymin, ymax, _color, onclick, onmouseover, onmouseout, tooltip, width, color, alpha, lineType, radius, position, animation, scale, inherit);
  }

  public static PlotContext<T1, TX1, TY1> Geom_ErrorBar<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, TY2>? ymin = null,
    Func<T2, TY2>? ymax = null,
    IAestheticMapping<T2, string>? _color = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    double radius = 5,
    PositionAdjustment position = PositionAdjustment.Identity,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_ErrorBar(source, x, y, ymin, ymax, _color, onclick, onmouseover, onmouseout, tooltip, width, color, alpha, lineType, radius, position, animation, scale, inherit);

    return context;
  }

  public static PlotContext<T1, TX1, TY1> Geom_ErrorBar<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, TY2>? ymin = null,
    Func<T2, TY2>? ymax = null,
    IAestheticMapping<T2, string>? _color = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    double radius = 5,
    PositionAdjustment position = PositionAdjustment.Identity,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return context.Geom_ErrorBar(new Source<T2>(source), x, y, ymin, ymax, _color, onclick, onmouseover, onmouseout, tooltip, width, color, alpha, lineType, radius, position, animation, scale, inherit);
  }

  public static PanelFactory<T, TX, TY> Geom_ErrorBar<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    Func<T, TY>? ymin = null,
    Func<T, TY>? ymax = null,
    IAestheticMapping<T, string>? _color = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, string>? tooltip = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    double radius = 5,
    PositionAdjustment position = PositionAdjustment.Identity,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    return Geom_ErrorBar(panel, panel.Context.Source!, x, y, ymin, ymax, _color, onclick, onmouseover, onmouseout, tooltip, width, color, alpha, lineType, radius, position, animation, scale, inherit);
  }

  public static PlotContext<T, TX, TY> Geom_ErrorBar<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    Func<T, TY>? ymin = null,
    Func<T, TY>? ymax = null,
    IAestheticMapping<T, string>? _color = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, string>? tooltip = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    double radius = 5,
    PositionAdjustment position = PositionAdjustment.Identity,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_ErrorBar(x, y, ymin, ymax, _color, onclick, onmouseover, onmouseout, tooltip, width, color, alpha, lineType, radius, position, animation, scale, inherit);

    return context;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Text<T1, TX1, TY1, T2, TX2, TY2, TT>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, double>? _angle = null,
    Func<T2, TT>? text = null,
    IAestheticMapping<T2, string>? _color = null,
    Size? size = null, Anchor anchor = middle, string weight = "normal", string style = "normal", string color = "#23d0fc", double angle = 0.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    panel.Add(() =>
    {
      var geom = new Text<T2, TX2, TY2, TT>(source, x, y, _angle, text, _color, scale, inherit)
      {
        Aesthetic = new()
        {
          Size = size ?? new() { Value = 1 },
          Anchor = anchor,
          Weight = weight,
          Style = style,
          Color = color,
          Angle = angle
        }
      };

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Text<T1, TX1, TY1, T2, TX2, TY2, TT>(
    this PanelFactory<T1, TX1, TY1> panel,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, double>? _angle = null,
    Func<T2, TT>? text = null,
    IAestheticMapping<T2, string>? _color = null,
    Size? size = null, Anchor anchor = middle, string weight = "normal", string style = "normal", string color = "#23d0fc", double angle = 0.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return panel.Geom_Text(new Source<T2>(source), x, y, _angle, text, _color, size, anchor, weight, style, color, angle, scale, inherit);
  }

  public static PlotContext<T1, TX1, TY1> Geom_Text<T1, TX1, TY1, T2, TX2, TY2, TT>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, double>? _angle = null,
    Func<T2, TT>? text = null,
    IAestheticMapping<T2, string>? _color = null,
    Size? size = null, Anchor anchor = middle, string weight = "normal", string style = "normal", string color = "#23d0fc", double angle = 0.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_Text(source, x, y, _angle, text, _color, size, anchor, weight, style, color, angle, scale, inherit);

    return context;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Text<T1, TX1, TY1, T2, TX2, TY2, TT>(
    this PlotContext<T1, TX1, TY1> context,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, double>? _angle = null,
    Func<T2, TT>? text = null,
    IAestheticMapping<T2, string>? _color = null,
    Size? size = null, Anchor anchor = middle, string weight = "normal", string style = "normal", string color = "#23d0fc", double angle = 0.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return context.Geom_Text(new Source<T2>(source), x, y, _angle, text, _color, size, anchor, weight, style, color, angle, scale, inherit);
  }

  public static PanelFactory<T, TX, TY> Geom_Text<T, TX, TY, TT>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    Func<T, double>? _angle = null,
    Func<T, TT>? text = null,
    IAestheticMapping<T, string>? _color = null,
    Size? size = null, Anchor anchor = middle, string weight = "normal", string style = "normal", string color = "#23d0fc", double angle = 0.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    return Geom_Text(panel, panel.Context.Source!, x, y, _angle, text, _color, size, anchor, weight, style, color, angle, scale, inherit);
  }

  public static PlotContext<T, TX, TY> Geom_Text<T, TX, TY, TT>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    Func<T, double>? _angle = null,
    Func<T, TT>? text = null,
    IAestheticMapping<T, string>? _color = null,
    Size? size = null, Anchor anchor = middle, string weight = "normal", string style = "normal", string color = "#23d0fc", double angle = 0.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_Text(x, y, _angle, text, _color, size, anchor, weight, style, color, angle, scale, inherit);

    return context;
  }

  public static PanelFactory<T1, TX1, TY> Geom_VLine<T1, TX1, TY, T2, TX2>(
    this PanelFactory<T1, TX1, TY> panel,
    Source<T2> source,
    Func<T2, TX2> x,
    Func<T2, string> label,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX1 : struct
    where TX2 : struct
    where TY : struct
  {
    panel.Add(() =>
    {
      var geom = new VLine<T2, TX2, TY>(source, x, label)
      {
        Line = new()
        {
          Width = width,
          Fill = color,
          Alpha = alpha,
          LineType = lineType
        },
        Text = new()
        {
          Size = size ?? new() { Value = 0.75 },
          Anchor = (anchor == end ? end : start),
          Weight = weight,
          Style = style,
          Color = color,
          Alpha = alpha
        }
      };

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX1, TY> Geom_VLine<T1, TX1, TY, T2, TX2>(
    this PanelFactory<T1, TX1, TY> panel,
    IEnumerable<T2> source,
    Func<T2, TX2> x,
    Func<T2, string> label,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX1 : struct
    where TX2 : struct
    where TY : struct
  {
    return panel.Geom_VLine(new Source<T2>(source), x, label, width, color, alpha, lineType, size, anchor, weight, style);
  }

  public static PlotContext<T1, TX1, TY> Geom_VLine<T1, TX1, TY, T2, TX2>(
    this PlotContext<T1, TX1, TY> context,
    Source<T2> source,
    Func<T2, TX2> x,
    Func<T2, string> label,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX1 : struct
    where TX2 : struct
    where TY : struct
  {
    context.Default_Panel().Geom_VLine(source, x, label, width, color, alpha, lineType, size, anchor, weight, style);

    return context;
  }

  public static PlotContext<T1, TX1, TY> Geom_VLine<T1, TX1, TY, T2, TX2>(
    this PlotContext<T1, TX1, TY> context,
    IEnumerable<T2> source,
    Func<T2, TX2> x,
    Func<T2, string> label,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX1 : struct
    where TX2 : struct
    where TY : struct
  {
    return context.Geom_VLine(new Source<T2>(source), x, label, width, color, alpha, lineType, size, anchor, weight, style);
  }

  public static PanelFactory<T, TX, TY> Geom_VLine<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX> x,
    Func<T, string> label,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    return Geom_VLine(panel, panel.Context.Source!, x, label, width, color, alpha, lineType, size, anchor, weight, style);
  }

  public static PlotContext<T, TX, TY> Geom_VLine<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX> x,
    Func<T, string> label,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_VLine(x, label, width, color, alpha, lineType, size, anchor, weight, style);

    return context;
  }

  public static PanelFactory<T1, TX, TY1> Geom_HLine<T1, TX, TY1, T2, TY2>(
    this PanelFactory<T1, TX, TY1> panel,
    Source<T2> source,
    Func<T2, TY2> y,
    Func<T2, string> label,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX : struct
    where TY1 : struct
    where TY2 : struct
  {
    panel.Add(() =>
    {
      var geom = new HLine<T2, TX, TY2>(source, y, label)
      {
        Line = new()
        {
          Width = width,
          Fill = color,
          Alpha = alpha,
          LineType = lineType
        },
        Text = new()
        {
          Size = size ?? new() { Value = 0.75 },
          Anchor = anchor == end ? end : start,
          Weight = weight,
          Style = style,
          Color = color,
          Alpha = alpha
        }
      };

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX, TY1> Geom_HLine<T1, TX, TY1, T2, TY2>(
    this PanelFactory<T1, TX, TY1> panel,
    IEnumerable<T2> source,
    Func<T2, TY2> y,
    Func<T2, string> label,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX : struct
    where TY1 : struct
    where TY2 : struct
  {
    return panel.Geom_HLine(new Source<T2>(source), y, label, width, color, alpha, lineType, size, anchor, weight, style);
  }

  public static PlotContext<T1, TX, TY1> Geom_HLine<T1, TX, TY1, T2, TY2>(
    this PlotContext<T1, TX, TY1> context,
    Source<T2> source,
    Func<T2, TY2> y,
    Func<T2, string> label,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_HLine(source, y, label, width, color, alpha, lineType, size, anchor, weight, style);

    return context;
  }

  public static PlotContext<T1, TX, TY1> Geom_HLine<T1, TX, TY1, T2, TY2>(
    this PlotContext<T1, TX, TY1> context,
    IEnumerable<T2> source,
    Func<T2, TY2> y,
    Func<T2, string> label,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX : struct
    where TY1 : struct
    where TY2 : struct
  {
    return context.Geom_HLine(new Source<T2>(source), y, label, width, color, alpha, lineType, size, anchor, weight, style);
  }

  public static PanelFactory<T, TX, TY> Geom_HLine<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TY> y,
    Func<T, string> label,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    return Geom_HLine(panel, panel.Context.Source!, y, label, width, color, alpha, lineType, size, anchor, weight, style);
  }

  public static PlotContext<T, TX, TY> Geom_HLine<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TY> y,
    Func<T, string> label,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_HLine(y, label, width, color, alpha, lineType, size, anchor, weight, style);

    return context;
  }

  public static PanelFactory<T1, TX, TY> Geom_ABLine<T1, TX, TY, T2>(
    this PanelFactory<T1, TX, TY> panel,
    Source<T2> source,
    Func<T2, double> a,
    Func<T2, double> b,
    Func<T2, string>? label = null,
    (bool x, bool y)? transformation = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    panel.Add(() =>
    {
      var geom = new ABLine<T2>(source, a, b, label, transformation)
      {
        Line = new()
        {
          Width = width,
          Fill = color,
          Alpha = alpha,
          LineType = lineType
        },
        Text = new()
        {
          Size = size ?? new() { Value = 0.75 },
          Anchor = (anchor == end ? end : start),
          Weight = weight,
          Style = style,
          Color = color,
          Alpha = alpha
        }
      };

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX, TY> Geom_ABLine<T1, TX, TY, T2>(
    this PanelFactory<T1, TX, TY> panel,
    IEnumerable<T2> source,
    Func<T2, double> a,
    Func<T2, double> b,
    Func<T2, string>? label = null,
    (bool x, bool y)? transformation = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    return panel.Geom_ABLine(new Source<T2>(source), a, b, label, transformation, width, color, alpha, lineType, size, anchor, weight, style);
  }

  public static PlotContext<T1, TX, TY> Geom_ABLine<T1, TX, TY, T2>(
    this PlotContext<T1, TX, TY> context,
    Source<T2> source,
    Func<T2, double> a,
    Func<T2, double> b,
    Func<T2, string>? label = null,
    (bool x, bool y)? transformation = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_ABLine(source, a, b, label, transformation, width, color, alpha, lineType, size, anchor, weight, style);

    return context;
  }

  public static PlotContext<T1, TX, TY> Geom_ABLine<T1, TX, TY, T2>(
    this PlotContext<T1, TX, TY> context,
    IEnumerable<T2> source,
    Func<T2, double> a,
    Func<T2, double> b,
    Func<T2, string>? label = null,
    (bool x, bool y)? transformation = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    return context.Geom_ABLine(new Source<T2>(source), a, b, label, transformation, width, color, alpha, lineType, size, anchor, weight, style);
  }

  public static PanelFactory<T, TX, TY> Geom_ABLine<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, double> a,
    Func<T, double> b,
    Func<T, string>? label = null,
    (bool x, bool y)? transformation = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    return Geom_ABLine(panel, panel.Context.Source!, a, b, label, transformation, width, color, alpha, lineType, size, anchor, weight, style);
  }

  public static PlotContext<T, TX, TY> Geom_ABLine<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, double> a,
    Func<T, double> b,
    Func<T, string>? label = null,
    (bool x, bool y)? transformation = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = end, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_ABLine(a, b, label, transformation, width, color, alpha, lineType, size, anchor, weight, style);

    return context;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_OHLC<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? open = null,
    Func<T2, TY2>? high = null,
    Func<T2, TY2>? low = null,
    Func<T2, TY2>? close = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    if (open is null)
    {
      throw new GGNetUserException($"{nameof(open)} selector should not be null");
    }

    if (high is null)
    {
      throw new GGNetUserException($"{nameof(high)} selector should not be null");
    }

    if (low is null)
    {
      throw new GGNetUserException($"{nameof(low)} selector should not be null");
    }

    if (close is null)
    {
      throw new GGNetUserException($"{nameof(close)} selector should not be null");
    }

    panel.Add(() =>
    {
      var geom = new OHLC<T2, TX2, TY2>(source, x, open, high, low, close)
      {
        OnClick = onclick,
        Aesthetic = new()
        {
          Width = width,
          Fill = color,
          Alpha = alpha,
          LineType = lineType

        }
      };

      return geom;
    });

    return panel;
  }

  public static PlotContext<T1, TX1, TY1> Geom_OHLC<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? open = null,
    Func<T2, TY2>? high = null,
    Func<T2, TY2>? low = null,
    Func<T2, TY2>? close = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_OHLC(source, x, open, high, low, close, onclick, width, color, alpha, lineType);

    return context;
  }

  public static PanelFactory<T, TX, TY> Geom_OHLC<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? open = null,
    Func<T, TY>? high = null,
    Func<T, TY>? low = null,
    Func<T, TY>? close = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid)
    where TX : struct
    where TY : struct
  {
    return Geom_OHLC(panel, panel.Context.Source!, x, open, high, low, close, onclick, width, color, alpha, lineType);
  }

  public static PlotContext<T, TX, TY> Geom_OHLC<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? open = null,
    Func<T, TY>? high = null,
    Func<T, TY>? low = null,
    Func<T, TY>? close = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_OHLC(x, open, high, low, close, onclick, width, color, alpha, lineType);

    return context;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Candlestick<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? open = null,
    Func<T2, TY2>? high = null,
    Func<T2, TY2>? low = null,
    Func<T2, TY2>? close = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    if (open is null)
    {
      throw new GGNetUserException($"{nameof(open)} selector should not be null");
    }

    if (high is null)
    {
      throw new GGNetUserException($"{nameof(high)} selector should not be null");
    }

    if (low is null)
    {
      throw new GGNetUserException($"{nameof(low)} selector should not be null");
    }

    if (close is null)
    {
      throw new GGNetUserException($"{nameof(close)} selector should not be null");
    }

    panel.Add(() =>
    {
      var geom = new Candlestick<T2, TX2, TY2>(source, x, open, high, low, close)
      {
        Line = new()
        {
          Width = width,
          Fill = color,
          Alpha = alpha,
          LineType = lineType
        },
        Rectangle = new()
        {
          Fill = color,
          Alpha = alpha
        }
      };

      return geom;
    });

    return panel;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Candlestick<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? open = null,
    Func<T2, TY2>? high = null,
    Func<T2, TY2>? low = null,
    Func<T2, TY2>? close = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_Candlestick(source, x, open, high, low, close, width, color, alpha, lineType);

    return context;
  }

  public static PanelFactory<T, TX, TY> Geom_Candlestick<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? open = null,
    Func<T, TY>? high = null,
    Func<T, TY>? low = null,
    Func<T, TY>? close = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid)
    where TX : struct
    where TY : struct
  {
    return Geom_Candlestick(panel, panel.Context.Source!, x, open, high, low, close, width, color, alpha, lineType);
  }

  public static PlotContext<T, TX, TY> Geom_Candlestick<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? open = null,
    Func<T, TY>? high = null,
    Func<T, TY>? low = null,
    Func<T, TY>? close = null,
    double width = 1.07, string color = "#23d0fc", double alpha = 1.0, LineType lineType = Solid)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_Candlestick(x, open, high, low, close, width, color, alpha, lineType);

    return context;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Volume<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? volume = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    string fill = "#23d0fc", double alpha = 1.0)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    if (volume is null)
    {
      throw new GGNetUserException($"{nameof(volume)} selector should not be null");
    }

    panel.Add(() =>
    {
      var geom = new Volume<T2, TX2, TY2>(source, x, volume)
      {
        OnClick = onclick,
        Aesthetic = new()
        {
          Fill = fill,
          Alpha = alpha
        }
      };

      return geom;
    });

    return panel;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Volume<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? volume = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    string fill = "#23d0fc", double alpha = 1.0)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_Volume(source, x, volume, onclick, fill, alpha);

    return context;
  }

  public static PanelFactory<T, TX, TY> Geom_Volume<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? volume = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    string fill = "#23d0fc", double alpha = 1.0)
    where TX : struct
    where TY : struct
  {
    return Geom_Volume(panel, panel.Context.Source!, x, volume, onclick, fill, alpha);
  }

  public static PlotContext<T, TX, TY> Geom_Volume<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? volume = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    string fill = "#23d0fc", double alpha = 1.0)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_Volume(x, volume, onclick, fill, alpha);

    return context;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Hex<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, TX2>? dx = null,
    Func<T2, TY2>? dy = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    if (dx is null)
    {
      throw new GGNetUserException($"{nameof(dx)} selector should not be null");
    }

    if (dy is null)
    {
      throw new GGNetUserException($"{nameof(dy)} selector should not be null");
    }

    panel.Add(() =>
    {
      var geom = new Hex<T2, TX2, TY2>(source, x, y, dx, dy, _fill, tooltip, animation, scale, inherit)
      {
        Aesthetic = new()
        {
          Fill = fill,
          Alpha = alpha
        },
        OnClick = onclick,
        OnMouseOver = onmouseover,
        OnMouseOut = onmouseout
      };

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Hex<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, TX2>? dx = null,
    Func<T2, TY2>? dy = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return panel.Geom_Hex(new Source<T2>(source), x, y, dx, dy, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, animation, scale, inherit);
  }

  public static PlotContext<T1, TX1, TY1> Geom_Hex<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, TX2>? dx = null,
    Func<T2, TY2>? dy = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_Hex(source, x, y, dx, dy, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, animation, scale, inherit);

    return context;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Hex<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, TX2>? dx = null,
    Func<T2, TY2>? dy = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return context.Geom_Hex(new Source<T2>(source), x, y, dx, dy, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, animation, scale, inherit);
  }

  public static PanelFactory<T, TX, TY> Geom_Hex<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    Func<T, TX>? dx = null,
    Func<T, TY>? dy = null,
    IAestheticMapping<T, string>? _fill = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    return Geom_Hex(panel, panel.Context.Source!, x, y, dx, dy, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, animation, scale, inherit);
  }

  public static PlotContext<T, TX, TY> Geom_Hex<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    Func<T, TX>? dx = null,
    Func<T, TY>? dy = null,
    IAestheticMapping<T, string>? _fill = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, string>? tooltip = null,
    string fill = "#23d0fc", double alpha = 1.0,
    bool animation = false,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_Hex(x, y, dx, dy, _fill, onclick, onmouseover, onmouseout, tooltip, fill, alpha, animation, scale, inherit);

    return context;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_RidgeLine<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, double>? height = null,
    IAestheticMapping<T2, string>? _fill = null,
    string fill = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    if (height is null)
    {
      throw new GGNetUserException($"{nameof(height)} selector should not be null");
    }

    panel.Add(() =>
    {
      var geom = new RidgeLine<T2, TX2, TY2>(source, x, y, height, _fill, scale, inherit)
      {
        Aesthetic = new Rectangle
        {
          Fill = fill,
          Alpha = alpha
        }
      };

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_RidgeLine<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, double>? height = null,
    IAestheticMapping<T2, string>? _fill = null,
    string fill = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return panel.Geom_RidgeLine(new Source<T2>(source), x, y, height, _fill, fill, alpha, scale, inherit);
  }

  public static PlotContext<T1, TX1, TY1> Geom_RidgeLine<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, double>? height = null,
    IAestheticMapping<T2, string>? _fill = null,
    string fill = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_RidgeLine(source, x, y, height, _fill, fill, alpha, scale, inherit);

    return context;
  }

  public static PlotContext<T1, TX1, TY1> Geom_RidgeLine<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, double>? height = null,
    IAestheticMapping<T2, string>? _fill = null,
    string fill = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return context.Geom_RidgeLine(new Source<T2>(source), x, y, height, _fill, fill, alpha, scale, inherit);
  }

  public static PanelFactory<T, TX, TY> Geom_RidgeLine<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    Func<T, double>? height = null,
    IAestheticMapping<T, string>? _fill = null,
    string fill = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    return Geom_RidgeLine(panel, panel.Context.Source!, x, y, height, _fill, fill, alpha, scale, inherit);
  }

  public static PlotContext<T, TX, TY> Geom_RidgeLine<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    Func<T, double>? height = null,
    IAestheticMapping<T, string>? _fill = null,
    string fill = "#23d0fc", double alpha = 1.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_RidgeLine(x, y, height, _fill, fill, alpha, scale, inherit);

    return context;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Violin<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, double>? width = null,
    IAestheticMapping<T2, string>? _fill = null,
    string fill = "#23d0fc", double alpha = 1.0, string? color = null,
    PositionAdjustment position = PositionAdjustment.Identity,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    if (width is null)
    {
      throw new GGNetUserException($"{nameof(width)} selector should not be null");
    }

    panel.Add(() =>
    {
      var geom = new Violin<T2, TX2, TY2>(source, x, y, width, _fill, position, scale, inherit)
      {
        Aesthetic = new()
        {
          Fill = fill,
          Alpha = alpha,
          Color = string.IsNullOrEmpty(color) ? "inherit" : color,
          Width = string.IsNullOrEmpty(color) ? 0.0 : 0.3
        }
      };

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Violin<T1, TX1, TY1, T2, TX2, TY2>(
    this PanelFactory<T1, TX1, TY1> panel,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, double>? width = null,
    IAestheticMapping<T2, string>? _fill = null,
    string fill = "#23d0fc", double alpha = 1.0, string? color = null,
    PositionAdjustment position = PositionAdjustment.Identity,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return panel.Geom_Violin(new Source<T2>(source), x, y, width, _fill, fill, alpha, color, position, scale, inherit);
  }

  public static PlotContext<T1, TX1, TY1> Geom_Violin<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, double>? width = null,
    IAestheticMapping<T2, string>? _fill = null,
    string fill = "#23d0fc", double alpha = 1.0, string? color = null,
    PositionAdjustment position = PositionAdjustment.Identity,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    context.Default_Panel().Geom_Violin(source, x, y, width, _fill, fill, alpha, color, position, scale, inherit);

    return context;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Violin<T1, TX1, TY1, T2, TX2, TY2>(
    this PlotContext<T1, TX1, TY1> context,
    IEnumerable<T2> source,
    Func<T2, TX2>? x = null,
    Func<T2, TY2>? y = null,
    Func<T2, double>? width = null,
    IAestheticMapping<T2, string>? _fill = null,
    string fill = "#23d0fc", double alpha = 1.0, string? color = null,
    PositionAdjustment position = PositionAdjustment.Identity,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TX2 : struct
    where TY1 : struct
    where TY2 : struct
  {
    return context.Geom_Violin(new Source<T2>(source), x, y, width, _fill, fill, alpha, color, position, scale, inherit);
  }

  public static PanelFactory<T, TX, TY> Geom_Violin<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    Func<T, double>? width = null,
    IAestheticMapping<T, string>? _fill = null,
    string fill = "#23d0fc", double alpha = 1.0, string? color = null,
    PositionAdjustment position = PositionAdjustment.Identity,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    return Geom_Violin(panel, panel.Context.Source!, x, y, width, _fill, fill, alpha, color, position, scale, inherit);
  }

  public static PlotContext<T, TX, TY> Geom_Violin<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    Func<T, double>? width = null,
    IAestheticMapping<T, string>? _fill = null,
    string fill = "#23d0fc", double alpha = 1.0, string? color = null,
    PositionAdjustment position = PositionAdjustment.Identity,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_Violin(x, y, width, _fill, fill, alpha, color, position, scale, inherit);

    return context;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Map<T1, TX1, TY1, T2>(
    this PanelFactory<T1, TX1, TY1> panel,
    Source<T2> source,
    Func<T2, Geospacial.Polygon[]> polygons,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, (Geospacial.Point point, string content)>? tooltip = null,
    bool animation = false,
    string fill = "#23d0fc", double alpha = 1.0, string color = "#000000", double width = 0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TY1 : struct
  {
    panel.Add(() =>
    {
      var geom = new Map<T2>(source, polygons, _fill, tooltip, animation, scale, inherit)
      {
        OnClick = onclick,
        OnMouseOver = onmouseover,
        OnMouseOut = onmouseout,
        Aesthetic = new()
        {
          Fill = fill,
          Alpha = alpha,
          Color = color,
          Width = width
        }
      };

      return geom;
    });

    return panel;
  }

  public static PanelFactory<T1, TX1, TY1> Geom_Map<T1, TX1, TY1, T2>(
    this PanelFactory<T1, TX1, TY1> panel,
    IEnumerable<T2> source,
    Func<T2, Geospacial.Polygon[]> polygons,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, (Geospacial.Point point, string content)>? tooltip = null,
    bool animation = false,
    string fill = "#23d0fc", double alpha = 1.0, string color = "#000000", double width = 0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TY1 : struct
  {
    return Geom_Map(panel, new Source<T2>(source), polygons, _fill, onclick, onmouseover, onmouseout, tooltip, animation, fill, alpha, color, width, scale, inherit);
  }

  public static PanelFactory<T, TX, TY> Geom_Map<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Geospacial.Polygon[] polygons,
    IAestheticMapping<Geospacial.Polygon[], string>? _fill = null,
    Func<Geospacial.Polygon[], MouseEventArgs, Task>? onclick = null,
    Func<Geospacial.Polygon[], MouseEventArgs, Task>? onmouseover = null,
    Func<Geospacial.Polygon[], MouseEventArgs, Task>? onmouseout = null,
    Func<Geospacial.Polygon[], (Geospacial.Point point, string content)>? tooltip = null,
    bool animation = false,
    string fill = "#23d0fc", double alpha = 1.0, string color = "#000000", double width = 0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    return Geom_Map(panel, new Source<Geospacial.Polygon[]>(new[] { polygons }), o => o, _fill, onclick, onmouseover, onmouseout, tooltip, animation, fill, alpha, color, width, scale, inherit);
  }

  public static PlotContext<T1, TX1, TY1> Geom_Map<T1, TX1, TY1, T2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, Geospacial.Polygon[]> polygons,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, (Geospacial.Point point, string content)>? tooltip = null,
    bool animation = false,
    string fill = "#23d0fc", double alpha = 1.0, string color = "#000000", double width = 0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TY1 : struct
  {
    context.Default_Panel().Geom_Map(source, polygons, _fill, onclick, onmouseover, onmouseout, tooltip, animation, fill, alpha, color, width, scale, inherit);

    return context;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Map<T1, TX1, TY1, T2>(
    this PlotContext<T1, TX1, TY1> context,
    IEnumerable<T2> source,
    Func<T2, Geospacial.Polygon[]> polygons,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, (Geospacial.Point point, string content)>? tooltip = null,
    bool animation = false,
    string fill = "#23d0fc", double alpha = 1.0, string color = "#000000", double width = 0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TY1 : struct
  {
    return Geom_Map(context, new Source<T2>(source), polygons, _fill, onclick, onmouseover, onmouseout, tooltip, animation, fill, alpha, color, width, scale, inherit);
  }

  public static PlotContext<T, TX, TY> Geom_Map<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Geospacial.Polygon[] polygons,
    IAestheticMapping<Geospacial.Polygon[], string>? _fill = null,
    Func<Geospacial.Polygon[], MouseEventArgs, Task>? onclick = null,
    Func<Geospacial.Polygon[], MouseEventArgs, Task>? onmouseover = null,
    Func<Geospacial.Polygon[], MouseEventArgs, Task>? onmouseout = null,
    Func<Geospacial.Polygon[], (Geospacial.Point point, string content)>? tooltip = null,
    bool animation = false,
    string fill = "#23d0fc", double alpha = 1.0, string color = "#000000", double width = 0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    return Geom_Map(context, new Source<Geospacial.Polygon[]>(new[] { polygons }), o => o, _fill, onclick, onmouseover, onmouseout, tooltip, animation, fill, alpha, color, width, scale, inherit);
  }

  public static PanelFactory<T, TX, TY> Geom_Map<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, Geospacial.Polygon[]> polygons,
    IAestheticMapping<T, string>? _fill = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, (Geospacial.Point point, string content)>? tooltip = null,
    bool animation = false,
    string fill = "#23d0fc", double alpha = 1.0, string color = "#000000", double width = 0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    return Geom_Map(panel, panel.Context.Source!, polygons, _fill, onclick, onmouseover, onmouseout, tooltip, animation, fill, alpha, color, width, scale, inherit);
  }

  public static PlotContext<T, TX, TY> Geom_Map<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, Geospacial.Polygon[]> polygons,
    IAestheticMapping<T, string>? _fill = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, (Geospacial.Point point, string content)>? tooltip = null,
    bool animation = false,
    string fill = "#23d0fc", double alpha = 1.0, string color = "#000000", double width = 0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_Map(polygons, _fill, onclick, onmouseover, onmouseout, tooltip, animation, fill, alpha, color, width, scale, inherit);

    return context;
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

  public static PlotContext<T, TX, TY> Theme<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Theme.Theme? theme = null,
    bool dark = false,
    Position axisY = Left,
    Position legend = Right)
    where TX : struct
    where TY : struct
  {
    context.Theme = theme ?? GGNet.Theme.Theme.Default(dark, axisY, legend);

    return context;
  }

  #region F# Helpers

  public static (double min, double max)? Range(double min, double max) => (min, max);

  public static (double minMult, double minAdd, double maxMult, double maxAdd)? Expand(double minMult, double minAdd, double maxMult, double maxAdd)
    => (minMult, minAdd, maxMult, maxAdd);

  public static (double? min, double? max)? Limits(double? min = null, double? max = null) => (min, max);

  public static (LocalDate? min, LocalDate? max)? Limits(LocalDate? min = null, LocalDate? max = null) => (min, max);

  public static (LocalDateTime? min, LocalDateTime? max)? Limits(LocalDateTime? min = null, LocalDateTime? max = null) => (min, max);

  public static (T? min, T? max)? Limits<T>(T? min, T? max)
     where T : struct
     => (min, max);

  public static (bool x, bool y)? Scale(bool x, bool y) => (x, y);

  #endregion
}

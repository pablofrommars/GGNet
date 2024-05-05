namespace GGNet;

public partial class PlotContext
{
  private static PlotContext<T, TX, TY> BuildBase<T, TX, TY>(Source<T>? source, Func<T, TX>? x, Func<T, TY>? y)
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

  public static PlotContext<T, LocalDate, double> Build<T>(Source<T> source, Func<T, LocalDate> x, Func<T, double>? y = null)
    => BuildBase(source, x, y);

  public static PlotContext<T, LocalDate, double> Build<T>(IEnumerable<T> items, Func<T, LocalDate> x, Func<T, double>? y = null)
    => BuildBase(new Source<T>(items), x, y);

  public static PlotContext<T, LocalDateTime, double> Build<T>(Source<T> source, Func<T, LocalDateTime> x, Func<T, double>? y = null)
    => BuildBase(source, x, y);

  public static PlotContext<T, LocalDateTime, double> Build<T>(IEnumerable<T> items, Func<T, LocalDateTime> x, Func<T, double>? y = null)
    => BuildBase(new Source<T>(items), x, y);

  public static PlotContext<T, Instant, double> Build<T>(Source<T> source, Func<T, Instant> x, Func<T, double>? y = null)
    => BuildBase(source, x, y);

  public static PlotContext<T, Instant, double> Build<T>(IEnumerable<T> items, Func<T, Instant> x, Func<T, double>? y = null)
    => BuildBase(new Source<T>(items), x, y);

  public static PlotContext<T, TX, double> Build<T, TX>(Source<T> source, Func<T, TX> x, Func<T, double>? y = null)
    where TX : struct, Enum
      => BuildBase(source, x, y);

  public static PlotContext<T, TX, double> Build<T, TX>(IEnumerable<T> items, Func<T, TX> x, Func<T, double>? y = null)
    where TX : struct, Enum
      => BuildBase(new Source<T>(items), x, y);

  public static PlotContext<T, TX, TY> Build<T, TX, TY>(Source<T> source, Func<T, TX> x, Func<T, TY> y)
    where TX : struct, Enum
    where TY : struct, Enum
      => BuildBase(source, x, y);

  public static PlotContext<T, TX, TY> Build<T, TX, TY>(IEnumerable<T> items, Func<T, TX> x, Func<T, TY> y)
    where TX : struct, Enum
    where TY : struct, Enum
      => BuildBase(new Source<T>(items), x, y);

  public static PlotContext<T, double, TY> Build<T, TY>(Source<T> source, Func<T, double> x, Func<T, TY> y)
    where TY : struct, Enum
      => BuildBase(source, x, y);

  public static PlotContext<T, double, TY> Build<T, TY>(IEnumerable<T> items, Func<T, double> x, Func<T, TY> y)
    where TY : struct, Enum
      => BuildBase(new Source<T>(items), x, y);

  public static PlotContext<T, double, double> Build<T>(Source<T> source, Func<T, double>? x = null, Func<T, double>? y = null)
    => BuildBase(source, x, y);

  public static PlotContext<T, double, double> Build<T>(IEnumerable<T> items, Func<T, double>? x = null, Func<T, double>? y = null)
    => BuildBase(new Source<T>(items), x, y);

  public static PlotContext<IWaiver, double, double> Build()
    => BuildBase<IWaiver, double, double>(null, null, null);
}

namespace GGNet;

using Geoms.ABLine;
using Elements;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
  public static PanelFactory<T1, TX, TY> Geom_ABLine<T1, TX, TY, T2>(
    this PanelFactory<T1, TX, TY> panel,
    Source<T2> source,
    Func<T2, double> a,
    Func<T2, double> b,
    Func<T2, string>? label = null,
    (bool x, bool y)? transformation = null,
    double width = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    panel.AddTyped(() =>
    {
      var geom = new ABLine<T2, TX, TY>(source, a, b, label, transformation)
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

  public static PanelFactory<T1, TX, TY> Geom_ABLine<T1, TX, TY, T2>(
    this PanelFactory<T1, TX, TY> panel,
    IEnumerable<T2> source,
    Func<T2, double> a,
    Func<T2, double> b,
    Func<T2, string>? label = null,
    (bool x, bool y)? transformation = null,
    double width = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    return panel.Geom_ABLine(new Source<T2>(source), a, b, label, transformation, width, color, opacity, lineType, size, anchor, weight, style);
  }

  public static PlotContext<T1, TX, TY> Geom_ABLine<T1, TX, TY, T2>(
    this PlotContext<T1, TX, TY> context,
    Source<T2> source,
    Func<T2, double> a,
    Func<T2, double> b,
    Func<T2, string>? label = null,
    (bool x, bool y)? transformation = null,
    double width = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_ABLine(source, a, b, label, transformation, width, color, opacity, lineType, size, anchor, weight, style);

    return context;
  }

  public static PlotContext<T1, TX, TY> Geom_ABLine<T1, TX, TY, T2>(
    this PlotContext<T1, TX, TY> context,
    IEnumerable<T2> source,
    Func<T2, double> a,
    Func<T2, double> b,
    Func<T2, string>? label = null,
    (bool x, bool y)? transformation = null,
    double width = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    return context.Geom_ABLine(new Source<T2>(source), a, b, label, transformation, width, color, opacity, lineType, size, anchor, weight, style);
  }

  public static PanelFactory<T, TX, TY> Geom_ABLine<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, double> a,
    Func<T, double> b,
    Func<T, string>? label = null,
    (bool x, bool y)? transformation = null,
    double width = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    return Geom_ABLine(panel, panel.Context.RequireSource(), a, b, label, transformation, width, color, opacity, lineType, size, anchor, weight, style);
  }

  public static PlotContext<T, TX, TY> Geom_ABLine<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, double> a,
    Func<T, double> b,
    Func<T, string>? label = null,
    (bool x, bool y)? transformation = null,
    double width = 1.07, string color = "#23d0fc", double opacity = 1.0, LineType lineType = Solid,
    Size? size = null, Anchor anchor = End, string weight = "normal", string style = "normal")
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_ABLine(a, b, label, transformation, width, color, opacity, lineType, size, anchor, weight, style);

    return context;
  }
}

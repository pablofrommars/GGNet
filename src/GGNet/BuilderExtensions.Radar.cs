namespace GGNet;

using Geoms.Radar;
using Elements;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
  public static PanelFactory<T1, TX1, TY1> Geom_Radar<T1, TX1, TY1, T2>(
    this PanelFactory<T1, TX1, TY1> panel,
    IReadOnlyList<T2> source,
    Func<T2, TX1>? x = null,
    Func<T2, TY1>? y = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, RenderFragment>? tooltip = null,
    string fill = "#23d0fc", double fillOpacity = 0.25, double width = 2.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TY1 : struct
  {
    panel.Context.CoordSystem = CoordSystem.Polar;

    panel.AddTyped(() =>
    {
      var geom = new Radar<T2, TX1, TY1>(source, x, y, _fill, tooltip, scale)
      {
        Aesthetic = new()
        {
          Fill = fill,
          FillOpacity = fillOpacity,
          Stroke = fill,
          StrokeWidth = width
        },
        OnClick = onclick,
        OnMouseOver = onmouseover,
        OnMouseOut = onmouseout
      };

      return geom;
    });

    return panel;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Radar<T1, TX1, TY1, T2>(
    this PlotContext<T1, TX1, TY1> context,
    IReadOnlyList<T2> source,
    Func<T2, TX1>? x = null,
    Func<T2, TY1>? y = null,
    IAestheticMapping<T2, string>? _fill = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    Func<T2, MouseEventArgs, Task>? onmouseover = null,
    Func<T2, MouseEventArgs, Task>? onmouseout = null,
    Func<T2, RenderFragment>? tooltip = null,
    string fill = "#23d0fc", double fillOpacity = 0.25, double width = 2.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX1 : struct
    where TY1 : struct
  {
    context.Default_Panel().Geom_Radar(source, x, y, _fill, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, width, scale);

    return context;
  }

  public static PanelFactory<T, TX, TY> Geom_Radar<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    IAestheticMapping<T, string>? _fill = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, RenderFragment>? tooltip = null,
    string fill = "#23d0fc", double fillOpacity = 0.25, double width = 2.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    return Geom_Radar(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, y ?? panel.Context.Selectors.Y, _fill ?? (inherit ? panel.Context.Aesthetics.Color : null), onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, width, scale);
  }

  public static PlotContext<T, TX, TY> Geom_Radar<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? y = null,
    IAestheticMapping<T, string>? _fill = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    Func<T, MouseEventArgs, Task>? onmouseover = null,
    Func<T, MouseEventArgs, Task>? onmouseout = null,
    Func<T, RenderFragment>? tooltip = null,
    string fill = "#23d0fc", double fillOpacity = 0.25, double width = 2.0,
    (bool x, bool y)? scale = null, bool inherit = true)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_Radar(x, y, _fill, onclick, onmouseover, onmouseout, tooltip, fill, fillOpacity, width, scale);

    return context;
  }
}

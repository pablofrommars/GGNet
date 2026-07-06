namespace GGNet;

using Geoms.Volume;
using Elements;
using Exceptions;
using Scales;

using static Anchor;
using static LineType;

public static partial class BuilderExtensions
{
  public static PanelFactory<T1, TX1, TY1> Geom_Volume<T1, TX1, TY1, T2>(
    this PanelFactory<T1, TX1, TY1> panel,
    IReadOnlyList<T2> source,
    Func<T2, TX1>? x = null,
    Func<T2, TY1>? volume = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    string fill = "#23d0fc", double opacity = 1.0)
    where TX1 : struct
    where TY1 : struct
  {
    if (volume is null)
    {
      throw new GGNetUserException($"{nameof(volume)} selector should not be null");
    }

    panel.AddTyped(() =>
    {
      var geom = new Volume<T2, TX1, TY1>(source, x, volume)
      {
        OnClick = onclick,
        Aesthetic = new()
        {
          Fill = fill,
          FillOpacity = opacity
        }
      };

      return geom;
    });

    return panel;
  }

  public static PlotContext<T1, TX1, TY1> Geom_Volume<T1, TX1, TY1, T2>(
    this PlotContext<T1, TX1, TY1> context,
    Source<T2> source,
    Func<T2, TX1>? x = null,
    Func<T2, TY1>? volume = null,
    Func<T2, MouseEventArgs, Task>? onclick = null,
    string fill = "#23d0fc", double opacity = 1.0)
    where TX1 : struct
    where TY1 : struct
  {
    context.Default_Panel().Geom_Volume(source, x, volume, onclick, fill, opacity);

    return context;
  }

  public static PanelFactory<T, TX, TY> Geom_Volume<T, TX, TY>(
    this PanelFactory<T, TX, TY> panel,
    Func<T, TX>? x = null,
    Func<T, TY>? volume = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    string fill = "#23d0fc", double opacity = 1.0)
    where TX : struct
    where TY : struct
  {
    return Geom_Volume(panel, panel.Context.RequireSource(), x ?? panel.Context.Selectors.X, volume, onclick, fill, opacity);
  }

  public static PlotContext<T, TX, TY> Geom_Volume<T, TX, TY>(
    this PlotContext<T, TX, TY> context,
    Func<T, TX>? x = null,
    Func<T, TY>? volume = null,
    Func<T, MouseEventArgs, Task>? onclick = null,
    string fill = "#23d0fc", double opacity = 1.0)
    where TX : struct
    where TY : struct
  {
    context.Default_Panel().Geom_Volume(x, volume, onclick, fill, opacity);

    return context;
  }
}

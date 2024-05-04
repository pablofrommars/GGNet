namespace GGNet;

using Scales;
using Facets;
using Geoms;

public sealed class PanelFactory<T, TX, TY>(PlotContext<T, TX, TY> context, double width = 1, double height = 1)
    where TX : struct
    where TY : struct
{
  private readonly List<Func<Data.Panel<T, TX, TY>, Facet<T>?, IGeom>> geoms = [];

  private readonly double width = width;
  private readonly double height = height;

  internal PlotContext<T, TX, TY> Context { get; } = context;

  internal Func<Position<TY>>? Y { get; set; }

  internal string? YLab { get; set; }

  internal void Add(Func<Data.Panel<T, TX, TY>, Facet<T>?, IGeom> geom) => geoms.Add(geom);

  internal Data.Panel<T, TX, TY> Build((int, int) coord, Facet<T>? facet = null, double? width = null, double? height = null)
  {
    var panel = new Data.Panel<T, TX, TY>(coord,
      Context,
      width ?? this.width,
      height ?? this.height
    );

    foreach (var geom in geoms)
    {
      panel.Geoms.Add(geom(panel, facet));
    }

    return panel;
  }
}

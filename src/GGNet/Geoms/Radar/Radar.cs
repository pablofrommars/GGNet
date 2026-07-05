using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;

namespace GGNet.Geoms.Radar;

internal sealed class Radar<T, TX, TY> : Geom<T, TX, TY>
  where TX : struct
  where TY : struct
{
  private readonly Dictionary<string, List<(double x, double y)>> series = [];
  private readonly List<Circle> circles = [];

  public Radar(
    IReadOnlyList<T> source,
    Func<T, TX>? x,
    Func<T, TY>? y,
    IAestheticMapping<T, string>? fill = null,
    Func<T, RenderFragment>? tooltip = null,
    (bool x, bool y)? scale = null,
    bool inherit = true)
    : base(source, scale, inherit)
  {
    Selectors = new()
    {
      X = x,
      Y = y,
      Tooltip = tooltip
    };

    Aesthetics = new()
    {
      Fill = fill
    };
  }

  public override CoordSystem SupportedCoordSystems => CoordSystem.Polar;

  public Selectors<T, TX, TY> Selectors { get; }

  public Aesthetics<T> Aesthetics { get; }

  public Positions<T> Positions { get; } = new();

  public Func<T, MouseEventArgs, Task>? OnClick { get; set; }

  public Func<T, MouseEventArgs, Task>? OnMouseOver { get; set; }

  public Func<T, MouseEventArgs, Task>? OnMouseOut { get; set; }

  private Func<T, double, double, MouseEventArgs, Task>? onMouseOver;

  public Elements.Rectangle Aesthetic { get; set; } = default!;

  public override void Init<T1, TX1, TY1>(Panel<T1, TX1, TY1> panel, Facet<T1>? facet)
  {
    base.Init(panel, facet);

    if (Selectors.X is null)
    {
      Positions.X = XMapping(panel.Data.Selectors.X!, panel.X);
    }
    else
    {
      Positions.X = XMapping(Selectors.X, panel.X);
    }

    if (Selectors.Y is null)
    {
      Positions.Y = YMapping(panel.Data.Selectors.Y!, panel.Y);
    }
    else
    {
      Positions.Y = YMapping(Selectors.Y, panel.Y);
    }

    if (OnMouseOver is null && OnMouseOut is null && Selectors.Tooltip is not null)
    {
      onMouseOver = (item, x, y, _) =>
      {
        panel.Component?.Tooltip?.Show(
          x,
          y,
          0,
          Selectors.Tooltip(item),
          Aesthetics.Fill?.Map(item) ?? Aesthetic.Fill,
          Aesthetic.FillOpacity);

        return Task.CompletedTask;
      };

      OnMouseOut = (_, __) =>
      {
        panel.Component?.Tooltip?.Hide();

        return Task.CompletedTask;
      };
    }
    else if (OnMouseOver is not null)
    {
      onMouseOver = (item, _, __, e) => OnMouseOver(item, e);
    }

    if (!inherit)
    {
      return;
    }

    Aesthetics.Fill ??= panel.Data.Aesthetics.Color as IAestheticMapping<T, string>;
  }

  public override void Train(T item)
  {
    Positions.X.Train(item);
    Positions.Y.Train(item);

    Aesthetics.Fill?.Train(item);
  }

  public override void Legend()
  {
    Legend(Aesthetics.Fill, value => new Elements.Rectangle
    {
      Fill = value,
      FillOpacity = Aesthetic.FillOpacity
    });
  }

  protected override void Shape(T item, bool flip)
  {
    var fill = Aesthetic.Fill;

    if (Aesthetics.Fill is not null)
    {
      fill = Aesthetics.Fill.Map(item);
      if (string.IsNullOrEmpty(fill))
      {
        return;
      }
    }

    var x = Positions.X.Map(item);
    if (double.IsNaN(x))
    {
      return;
    }

    var y = Positions.Y.Map(item);
    if (double.IsNaN(y))
    {
      return;
    }

    if (!series.TryGetValue(fill, out var points))
    {
      points = [];

      series[fill] = points;
    }

    points.Add((x, y));

    if (OnClick is not null || OnMouseOver is not null || OnMouseOut is not null || Selectors.Tooltip is not null)
    {
      circles.Add(new Circle
      {
        X = x,
        Y = y,
        Aesthetic = new()
        {
          Radius = 3.0 * Aesthetic.StrokeWidth,
          Fill = "transparent",
          FillOpacity = 0
        },
        OnClick = OnClick is not null ? e => OnClick(item, e) : null,
        OnMouseOver = onMouseOver is not null ? e => onMouseOver(item, x, y, e) : null,
        OnMouseOut = OnMouseOut is not null ? e => OnMouseOut(item, e) : null
      });
    }

    if (scale.x)
    {
      Positions.X.Position.Shape(x, x);
    }

    if (scale.y)
    {
      // The radial scale is zero-based: the web center is 0, not the data minimum.
      Positions.Y.Position.Shape(0.0, y);
    }
  }

  protected override void Set(bool flip)
  {
    foreach (var (fill, points) in series)
    {
      if (points.Count < 2)
      {
        continue;
      }

      points.Sort((a, b) => a.x.CompareTo(b.x));

      var longitude = new double[points.Count];
      var latitude = new double[points.Count];

      for (var i = 0; i < points.Count; i++)
      {
        (longitude[i], latitude[i]) = points[i];
      }

      Layer.Add(new Shapes.Polygon
      {
        Path = new()
        {
          Longitude = longitude,
          Latitude = latitude
        },
        Aesthetic = new()
        {
          Fill = fill,
          FillOpacity = Aesthetic.FillOpacity,
          Stroke = fill,
          StrokeWidth = Aesthetic.StrokeWidth
        }
      });
    }

    // Hit-target circles go on top of the polygons so they receive mouse events.
    for (var i = 0; i < circles.Count; i++)
    {
      Layer.Add(circles[i]);
    }
  }

  public override void Clear()
  {
    base.Clear();

    series.Clear();
    circles.Clear();
  }
}

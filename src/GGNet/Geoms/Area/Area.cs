using GGNet.Buffers;
using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;

namespace GGNet.Geoms.Area;

internal sealed class Area<T, TX, TY> : Geom<T, TX, TY>
  where TX : struct
  where TY : struct
{
  private sealed class Comparer : Comparer<(double x, double y, T item)>
  {
    public override int Compare((double x, double y, T item) a, (double x, double y, T item) b) => a.x.CompareTo(b.x);
  }

  private static readonly Comparer comparer = new();

  private readonly Buffer<(string fill, SortedBuffer<(double x, double y, T item)> points)> series = new(16, 1);

  private readonly PositionAdjustment position;

  public Area(
    IReadOnlyList<T> source,
    Func<T, TX>? x,
    Func<T, TY>? y,
    IAestheticMapping<T, string>? fill = null,
    Func<T, RenderFragment>? tooltip = null,
    PositionAdjustment position = PositionAdjustment.Identity,
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

    this.position = position;
  }

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

    Aesthetics.Fill ??= panel.Data.Aesthetics.Fill as IAestheticMapping<T, string>;
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

    SortedBuffer<(double x, double y, T item)>? points = null;

    for (var i = 0; i < series.Count; i++)
    {
      var serie = series[i];
      if (serie.fill == fill)
      {
        points = serie.points;
        break;
      }
    }

    if (points is null)
    {
      points = new(comparer: comparer);

      series.Add((fill, points));
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

    points.Add((x, y, item));
  }

  private void Interactivity(Buffer<IShape> circles, T item, double x, double y)
  {
    if (OnClick is not null || onMouseOver is not null || OnMouseOut is not null)
    {
      var circle = new Circle
      {
        X = x,
        Y = y,
        Aesthetic = new()
        {
          Radius = 3.0,
          Fill = "transparent",
          FillOpacity = 0,
        },
        OnClick = OnClick is not null ? e => OnClick(item, e) : null,
        OnMouseOver = onMouseOver is not null ? e => onMouseOver(item, x, y, e) : null,
        OnMouseOut = OnMouseOut is not null ? e => OnMouseOut(item, e) : null
      };

      circles.Add(circle);
    }
  }

  private sealed class SumComparer : Comparer<(double x, double y)>
  {
    public override int Compare((double x, double y) a, (double x, double y) b) => a.x.CompareTo(b.x);
  }

  private static readonly SumComparer sumComparer = new();

  private void Stack()
  {
    var sum = new SortedBuffer<(double x, double y)>(comparer: sumComparer);
    var circles = new Buffer<IShape>();

    for (var i = 0; i < series.Count; i++)
    {
      var (_, points) = series[i];
      for (var j = 0; j < points.Count; j++)
      {
        var (x, _, _) = points[j];

        sum.Add((x, 0));
      }
    }

    for (var i = 0; i < series.Count; i++)
    {
      var (fill, points) = series[i];

      var area = new Shapes.Area
      {
        Aesthetic = new()
        {
          Fill = fill,
          FillOpacity = Aesthetic.FillOpacity
        }
      };

      Layer.Add(area);

      var ylast = 0.0;
      var xlast = 0.0;
      var slast = 0.0;

      var head = 0;

      {
        var (x, y, item) = points[0];

        while (head < sum.Count)
        {
          var (xhead, yhead) = sum[head];

          if (xhead == x)
          {
            ylast = y;
            slast = yhead + y;

            area.Points.Add((x, yhead, slast));

            if (scale.y)
            {
              Positions.Y.Position.Shape(0, slast);
            }

            sum[head++] = (x, slast);
            xlast = x;

            break;
          }

          head++;
        }

        Interactivity(circles, item, x, slast);

        if (scale.x)
        {
          Positions.X.Position.Shape(x, x);
        }
      }

      for (var j = 1; j < points.Count; j++)
      {
        var k = head;
        var (x, y, item) = points[j];

        var (xk, _) = sum[k];

        var xhead = xk;

        while (head < sum.Count)
        {
          (xhead, _) = sum[head];

          if (xhead == x)
          {
            break;
          }

          head++;
        }

        if (head == k)
        {
          var (_, sy) = sum[head];
          slast = sy + y;
          area.Points.Add((x, sy, slast));
          sum[k] = (x, slast);
        }
        else
        {
          var xdelta = xhead - xlast;
          var ydelta = y - ylast;

          if (ydelta >= 0)
          {
            for (; k <= head; k++)
            {
              var (sx, sy) = sum[k];
              var delta = sy + (sx - xlast) / xdelta * ydelta;
              area.Points.Add((sx, sy, delta));
              sum[k] = (sx, delta);
              slast = delta;
            }
          }
          else
          {
            for (; k <= head; k++)
            {
              var (sx, sy) = sum[k];
              var delta = sy + ylast + (sx - xlast) / xdelta * ydelta;
              area.Points.Add((sx, sy, delta));
              sum[k] = (sx, delta);
              slast = delta;
            }
          }
        }

        Interactivity(circles, item, x, slast);

        if (scale.y)
        {
          Positions.Y.Position.Shape(0, slast);
        }

        xlast = x;
        ylast = y;

        if (scale.x)
        {
          Positions.X.Position.Shape(x, x);
        }

        head++;
      }
    }

    Layer.Add(circles);
  }

  private void Identity()
  {
    var circles = new Buffer<IShape>();

    for (var i = 0; i < series.Count; i++)
    {
      var (fill, points) = series[i];

      var area = new Shapes.Area
      {
        Aesthetic = new()
        {
          Fill = fill,
          FillOpacity = Aesthetic.FillOpacity
        }
      };

      Layer.Add(area);

      for (var j = 0; j < points.Count; j++)
      {
        var (x, y, item) = points[j];

        area.Points.Add((x, 0, y));

        Interactivity(circles, item, x, y);

        if (scale.x)
        {
          Positions.X.Position.Shape(x, x);
        }

        if (scale.y)
        {
          Positions.Y.Position.Shape(y, y);
        }
      }
    }

    Layer.Add(circles);
  }

  protected override void Set(bool flip)
  {
    if (position == PositionAdjustment.Stack)
    {
      Stack();
    }
    else if (position == PositionAdjustment.Identity)
    {
      Identity();
    }
    else
    {
      throw new NotImplementedException();
    }
  }

  public override void Clear()
  {
    base.Clear();

    series.Clear();
  }
}

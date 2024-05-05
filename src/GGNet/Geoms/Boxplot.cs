using GGNet.Buffers;
using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;

namespace GGNet.Geoms.Boxplot;

internal sealed class Boxplot<T, TX, TY> : Geom<T, TX, TY>
  where TX : struct
  where TY : struct
{
  private sealed class Comparer : IComparer<(double coord, Dictionary<string, SortedBuffer<double>> data)>
  {
    public int Compare((double coord, Dictionary<string, SortedBuffer<double>> data) x, (double coord, Dictionary<string, SortedBuffer<double>> data) y)
      => x.coord.CompareTo(y.coord);

    public static readonly Comparer Instance = new();
  }

  private readonly SortedBuffer<(double coord, Dictionary<string, SortedBuffer<double>> data)> boxes = new(32, 1, Comparer.Instance);

  private readonly double size;

  public Boxplot(
    Source<T> source,
    Func<T, TX>? x,
    Func<T, TY>? y,
    IAestheticMapping<T, string>? fill = null,
    double size = 0.8,
    (bool x, bool y)? scale = null,
    bool inherit = true)
    : base(source, scale, inherit)
  {
    Selectors = new()
    {
      X = x,
      Y = y
    };

    Aesthetics = new()
    {
      Fill = fill
    };

    this.size = size;
  }

  public Selectors<T, TX, TY> Selectors { get; }

  public Aesthetics<T> Aesthetics { get; }

  public Positions<T> Positions { get; } = new();

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
    var y = Positions.Y.Map(item);

    var exist = false;
    for (var i = 0; i < boxes.Count; i++)
    {
      var (coord, data) = boxes[i];
      if (coord == y)
      {
        if (data.TryGetValue(fill, out var points))
        {
          points.Add(x);
        }
        else
        {
          points = new SortedBuffer<double>(8, 1);
          points.Add(x);
          data[fill] = points;
        }

        exist = true;
        break;
      }
    }

    if (!exist)
    {
      var points = new SortedBuffer<double>(8, 1);
      points.Add(x);

      boxes.Add((y, new() { [fill] = points }));
    }
  }

  protected override void Set(bool flip)
  {
    var delta = size;

    if (boxes.Count > 1)
    {
      var d = double.MaxValue;

      for (var i = 1; i < boxes.Count; i++)
      {
        d = Math.Min(d, boxes[i].coord - boxes[i - 1].coord);
      }

      delta *= d;
    }

    for (var i = 0; i < boxes.Count; i++)
    {
      var (coord, data) = boxes[i];
      var n = data.Count;

      var offset = delta / n;
      var start = coord - delta / 2.0;

      foreach (var (fill, points) in data)
      {
        var p10 = Percentile(points, 0.1);
        var p25 = Percentile(points, 0.25);
        var p50 = Percentile(points, 0.5);
        var p75 = Percentile(points, 0.75);
        var p90 = Percentile(points, 0.9);

        Layer.Add(new Shapes.Line
        {
          X1 = p10,
          X2 = p25,
          Y1 = start + offset / 2,
          Y2 = start + offset / 2,
          Aesthetic = new()
          {
            Stroke = fill,
            StrokeOpacity = 1.0,
            StrokeWidth = Aesthetic.StrokeWidth,
          }
        });

        Layer.Add(new Shapes.Line
        {
          X1 = p75,
          X2 = p90,
          Y1 = start + offset / 2,
          Y2 = start + offset / 2,
          Aesthetic = new()
          {
            Stroke = fill,
            StrokeOpacity = 1.0,
            StrokeWidth = Aesthetic.StrokeWidth,
          }
        });

        Layer.Add(new Rectangle
        {
          X = p25,
          Y = start,
          Width = p75 - p25,
          Height = offset,
          Aesthetic = new()
          {
            Fill = fill,
            FillOpacity = Aesthetic.FillOpacity,
            Stroke = fill,
            StrokeOpacity = 1.0,
            StrokeWidth = Aesthetic.StrokeWidth,
          }
        });

        Layer.Add(new Shapes.Line
        {
          X1 = p50,
          X2 = p50,
          Y1 = start,
          Y2 = start + offset,
          Aesthetic = new()
          {
            Stroke = fill,
            StrokeOpacity = 1.0,
            StrokeWidth = Aesthetic.StrokeWidth,
          }
        });

        start += offset;
      }
    }
  }

  public override void Clear()
  {
    base.Clear();

    boxes.Clear();
  }

  public static double Percentile(SortedBuffer<double> points, double p)
  {
    var n = (points.Count - 1.0) * p + 1.0;
    if (n == 1)
    {
      return points[0];
    }
    else if (n == points.Count)
    {
      return points[^1];
    }
    else
    {
      var index = (int)n;
      return points[index - 1] + (n - index) * (points[index] - points[index - 1]);
    }
  }
}

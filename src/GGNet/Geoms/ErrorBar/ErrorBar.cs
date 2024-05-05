using GGNet.Buffers;
using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;

namespace GGNet.Geoms.ErrorBar;

internal sealed class ErrorBar<T, TX, TY> : Geom<T, TX, TY>
  where TX : struct
  where TY : struct
{
  private sealed class Comparer : IComparer<(double x, Buffer<(string color, double y, double ymin, double ymax, T item)> bars)>
  {
    public static readonly Comparer Instance = new();

    public int Compare((double x, Buffer<(string color, double y, double ymin, double ymax, T item)> bars) x, (double x, Buffer<(string color, double y, double ymin, double ymax, T item)> bars) y)
      => x.x.CompareTo(y.x);
  }

  private readonly SortedBuffer<(double x, Buffer<(string color, double y, double ymin, double ymax, T item)> bars)> bars = new(32, 1, Comparer.Instance);

  private readonly PositionAdjustment position;
  private readonly bool animation;

  public ErrorBar(
    Source<T> source,
    Func<T, TX>? x,
    Func<T, TY>? y,
    Func<T, TY>? ymin,
    Func<T, TY>? ymax,
    IAestheticMapping<T, string>? color = null,
    Func<T, string>? tooltip = null,
    PositionAdjustment position = PositionAdjustment.Identity,
    bool animation = false,
    (bool x, bool y)? scale = null,
    bool inherit = true)
    : base(source, scale, inherit)
  {
    Selectors = new()
    {
      X = x,
      Y = y,
      YMin = ymin,
      YMax = ymax,
      Tooltip = tooltip
    };

    Aesthetics = new()
    {
      Color = color
    };

    this.animation = animation;
    this.position = position;
  }

  public Selectors<T, TX, TY> Selectors { get; }

  public Aesthetics<T> Aesthetics { get; }

  public Positions<T> Positions { get; } = new();

  public Func<T, MouseEventArgs, Task>? OnClick { get; set; }

  public Func<T, MouseEventArgs, Task>? OnMouseOver { get; set; }

  public Func<T, MouseEventArgs, Task>? OnMouseOut { get; set; }

  private Func<T, double, double, MouseEventArgs, Task>? onMouseOver;

  public Elements.Line Line { get; set; } = default!;

  public Elements.Circle Circle { get; set; } = default!;

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

    if (Selectors.YMin is null)
    {
      Positions.YMin = YMapping(panel.Data.Selectors.Y!, panel.Y);
    }
    else
    {
      Positions.YMin = YMapping(Selectors.YMin, panel.Y);
    }

    if (Selectors.YMax is null)
    {
      Positions.YMax = YMapping(panel.Data.Selectors.Y!, panel.Y);
    }
    else
    {
      Positions.YMax = YMapping(Selectors.YMax, panel.Y);
    }

    if (OnMouseOver is null && OnMouseOut is null && Selectors.Tooltip is not null)
    {
      onMouseOver = (item, x, y, _) =>
      {
        var radius = Circle.Radius;
        if (animation)
        {
          radius *= 1.5;
        }

        panel.Component?.Tooltip?.Show(
          x,
          y,
          radius,
          Selectors.Tooltip(item),
          Aesthetics.Color?.Map(item) ?? Circle.Fill,
          Circle.FillOpacity);

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

    Aesthetics.Color ??= panel.Data.Aesthetics.Color as IAestheticMapping<T, string>;
  }

  public override void Train(T item)
  {
    Positions.X.Train(item);
    Positions.Y.Train(item);
    Positions.YMin.Train(item);
    Positions.YMax.Train(item);

    Aesthetics.Color?.Train(item);
  }

  public override void Legend()
  {
    Legend(Aesthetics.Color, value =>
    [
        new Elements.VLine
        {
          Stroke = value,
          StrokeOpacity = Line.StrokeOpacity,
          StrokeWidth = Line.StrokeWidth,
          LineType = Line.LineType
        },
        new Elements.Circle
        {
          Fill = value,
          FillOpacity = Circle.FillOpacity,
                    //Radius = Circle.Radius
                    Radius = 2
        }
    ]);
  }

  protected override void Shape(T item, bool flip)
  {
    var color = Line.Stroke;

    if (Aesthetics.Color is not null)
    {
      color = Aesthetics.Color.Map(item);
      if (string.IsNullOrEmpty(color))
      {
        return;
      }
    }

    var x = Positions.X.Map(item);
    var y = Positions.Y.Map(item);
    var ymin = Positions.YMin.Map(item);
    var ymax = Positions.YMax.Map(item);

    var exist = false;

    for (var i = 0; i < bars.Count; i++)
    {
      var bar = bars[i];
      if (bar.x == x)
      {
        bar.bars.Add((color, y, ymin, ymax, item));
        exist = true;
        break;
      }
    }

    if (!exist)
    {
      var bar = new Buffer<(string color, double y, double ymin, double ymax, T item)>(8, 1);
      bar.Add((color, y, ymin, ymax, item));
      bars.Add((x, bar));
    }
  }

  private void Identity()
  {
    for (var i = 0; i < bars.Count; i++)
    {
      var bar = bars[i];

      for (var j = 0; j < bar.bars.Count; j++)
      {
        var (color, y, ymin, ymax, item) = bar.bars[j];

        Layer.Add(new Shapes.Line
        {
          X1 = bar.x,
          X2 = bar.x,
          Y1 = ymin,
          Y2 = ymax,
          Aesthetic = new()
          {
            Stroke = color,
            StrokeOpacity = Line.StrokeOpacity,
            StrokeWidth = Line.StrokeWidth,
            LineType = Line.LineType
          }
        });

        var circle = new Circle
        {
          Classes = animation ? "animate-point" : string.Empty,
          X = bar.x,
          Y = y,
          Aesthetic = new()
          {
            Radius = Circle.Radius,
            Fill = color,
            FillOpacity = Circle.FillOpacity
          },
          OnClick = OnClick is not null ? e => OnClick(item, e) : null,
          OnMouseOver = onMouseOver is not null ? e => onMouseOver(item, bar.x, y, e) : null,
          OnMouseOut = OnMouseOut is not null ? e => OnMouseOut(item, e) : null
        };

        Layer.Add(circle);

        if (scale.x)
        {
          Positions.X.Position.Shape(bar.x, bar.x);
        }

        if (scale.y)
        {
          Positions.YMin.Position.Shape(ymin, ymax);
          Positions.YMax.Position.Shape(ymin, ymax);
        }
      }
    }
  }

  private void Dodge()
  {
    var delta = 0.6;

    if (bars.Count > 1)
    {
      var d = double.MaxValue;

      for (var i = 1; i < bars.Count; i++)
      {
        d = Math.Min(d, bars[i].x - bars[i - 1].x);
      }

      delta *= d;
    }

    for (var i = 0; i < bars.Count; i++)
    {
      var bar = bars[i];
      var n = bar.bars.Count;

      var w = delta / n;
      var x = bar.x - delta / 2.0 + w / 2.0;

      for (var j = 0; j < n; j++)
      {
        var (color, y, ymin, ymax, item) = bar.bars[j];

        Layer.Add(new Shapes.Line
        {
          X1 = x,
          X2 = x,
          Y1 = ymin,
          Y2 = ymax,
          Aesthetic = new()
          {
            Stroke = color,
            StrokeOpacity = Line.StrokeOpacity,
            StrokeWidth = Line.StrokeWidth,
            LineType = Line.LineType
          }
        });

        var _x = x;

        var circle = new Circle
        {
          Classes = animation ? "animate-point" : string.Empty,
          X = x,
          Y = y,
          Aesthetic = new()
          {
            Radius = Circle.Radius,
            Fill = color,
            FillOpacity = Circle.FillOpacity
          },
          OnClick = OnClick is not null ? e => OnClick(item, e) : null,
          OnMouseOver = onMouseOver is not null ? e => onMouseOver(item, _x, y, e) : null,
          OnMouseOut = OnMouseOut is not null ? e => OnMouseOut(item, e) : null
        };

        Layer.Add(circle);

        if (scale.x)
        {
          Positions.X.Position.Shape(x, x);
        }

        if (scale.y)
        {
          Positions.YMin.Position.Shape(ymin, ymax);
          Positions.YMax.Position.Shape(ymin, ymax);
        }

        x += w;
      }
    }
  }

  protected override void Set(bool flip)
  {
    if (position == PositionAdjustment.Identity)
    {
      Identity();
    }
    else if (position == PositionAdjustment.Dodge)
    {
      Dodge();
    }
    else
    {
      throw new NotImplementedException();
    }
  }

  public override void Clear()
  {
    base.Clear();

    bars.Clear();
  }
}

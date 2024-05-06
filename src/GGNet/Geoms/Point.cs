using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;

namespace GGNet.Geoms.Point;

internal sealed class Point<T, TX, TY> : Geom<T, TX, TY>
  where TX : struct
  where TY : struct
{
  private readonly bool animation;

  public Point(
    Source<T> source,
    Func<T, TX>? x,
    Func<T, TY>? y,
    IAestheticMapping<T, double>? size,
    IAestheticMapping<T, string>? color,
    Func<T, RenderFragment>? tooltip,
    bool animation,
    (bool x, bool y)? scale,
    bool inherit)
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
      Size = size,
      Color = color,
    };

    this.animation = animation;
  }

  public Selectors<T, TX, TY> Selectors { get; }

  public Aesthetics<T> Aesthetics { get; }

  public Positions<T> Positions { get; } = new();

  public Func<T, MouseEventArgs, Task>? OnClick { get; set; }

  public Func<T, MouseEventArgs, Task>? OnMouseOver { get; set; }

  public Func<T, MouseEventArgs, Task>? OnMouseOut { get; set; }

  private Func<T, double, double, MouseEventArgs, Task>? onMouseOver;

  public Elements.Circle Aesthetic { get; init; } = default!;

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
        var radius = Aesthetic.Radius;
        if (Aesthetics.Size is not null)
        {
          radius = Aesthetics.Size.Map(item);
        }

        if (animation)
        {
          radius *= 1.5;
        }

        panel.Component?.Tooltip?.Show(
          x,
          y,
          radius,
          Selectors.Tooltip(item),
          Aesthetics.Color?.Map(item) ?? Aesthetic.Fill,
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

    Aesthetics.Size ??= panel.Data.Aesthetics.Size as IAestheticMapping<T, double>;
    Aesthetics.Color ??= panel.Data.Aesthetics.Color as IAestheticMapping<T, string>;
  }

  public override void Train(T item)
  {
    Positions.X.Train(item);
    Positions.Y.Train(item);

    Aesthetics.Size?.Train(item);
    Aesthetics.Color?.Train(item);
  }

  public override void Legend()
  {
    Legend(Aesthetics.Color, value => new Elements.Circle
    {
      Fill = value,
      FillOpacity = Aesthetic.FillOpacity,
      Radius = Aesthetic.Radius
    });

    Legend(Aesthetics.Size, value => new Elements.Circle
    {
      Fill = Aesthetic.Fill,
      FillOpacity = Aesthetic.FillOpacity,
      Radius = value
    });
  }

  protected override void Shape(T item, bool flip)
  {
    var color = Aesthetic.Fill;

    if (Aesthetics.Color is not null)
    {
      color = Aesthetics.Color.Map(item);
      if (string.IsNullOrEmpty(color))
      {
        return;
      }
    }

    var radius = Aesthetic.Radius;

    if (Aesthetics.Size is not null)
    {
      radius = Aesthetics.Size.Map(item);
      if (radius <= 0)
      {
        return;
      }
    }

    var x = Positions.X.Map(item);
    var y = Positions.Y.Map(item);

    var circle = new Circle
    {
      Classes = animation ? "animate-point" : string.Empty,
      X = x,
      Y = y,
      Aesthetic = new()
      {
        Radius = radius,
        Fill = color,
        FillOpacity = Aesthetic.FillOpacity
      },
      OnClick = OnClick is not null ? e => OnClick(item, e) : null,
      OnMouseOver = onMouseOver is not null ? e => onMouseOver(item, x, y, e) : null,
      OnMouseOut = OnMouseOut is not null ? e => OnMouseOut(item, e) : null
    };

    Layer.Add(circle);

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

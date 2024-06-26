﻿using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;

namespace GGNet.Geoms.Hex;

internal sealed class Hex<T, TX, TY> : Geom<T, TX, TY>
  where TX : struct
  where TY : struct
{
  private readonly bool animation;

  public Hex(
    Source<T> source,
    Func<T, TX>? x,
    Func<T, TY>? y,
    Func<T, TX> Dx,
    Func<T, TY> Dy,
    IAestheticMapping<T, string>? fill = null,
    Func<T, RenderFragment>? tooltip = null,
    bool animation = false,
    (bool x, bool y)? scale = null,
    bool inherit = true)
    : base(source, scale, inherit)
  {
    Selectors = new()
    {
      X = x,
      Y = y,
      Dx = Dx,
      Dy = Dy,
      Tooltip = tooltip
    };

    Aesthetics = new()
    {
      Fill = fill
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

    Positions.Dx = XMapping(Selectors.Dx, panel.X);
    Positions.Dy = YMapping(Selectors.Dy, panel.Y);

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
    Positions.Dx.Train(item);
    Positions.Dy.Train(item);

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
    var y = Positions.Y.Map(item);
    var dx = Positions.Dx.Map(item) / 2.0;
    var dy = Positions.Dy.Map(item) / Math.Sqrt(3.0) / 2.0 * 1.15;

    var hex = new Shapes.Polygon
    {
      Classes = animation ? "animate-hex" : string.Empty,
      Path = new()
      {
        Longitude = [x + dx, x + dx, x, x - dx, x - dx, x],
        Latitude = [y + dy, y - dy, y - 2.0 * dy, y - dy, y + dy, y + 2.0 * dy]
      },
      Aesthetic = new()
      {
        Fill = fill,
        FillOpacity = Aesthetic.FillOpacity
      },
      OnClick = OnClick is not null ? e => OnClick(item, e) : null,
      OnMouseOver = onMouseOver is not null ? e => onMouseOver(item, x, y, e) : null,
      OnMouseOut = OnMouseOut is not null ? e => OnMouseOut(item, e) : null
    };

    Layer.Add(hex);

    if (scale.x)
    {
      Positions.X.Position.Shape(x - dx, x + dx);
    }

    if (scale.y)
    {
      Positions.Y.Position.Shape(y - 2.0 * dy, y + 2.0 * dy);
    }
  }
}

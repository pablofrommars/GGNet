using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;

namespace GGNet.Geoms.Tile;

internal sealed class Tile<T, TX, TY> : Geom<T, TX, TY>
  where TX : struct
  where TY : struct
{
  public Tile(
    Source<T> source,
    Func<T, TX> x,
    Func<T, TY> y,
    Func<T, double> width,
    Func<T, double> height,
    IAestheticMapping<T, string>? fill = null,
    (bool x, bool y)? scale = null)
    : base(source, scale, false)
  {
    Selectors = new()
    {
      X = x,
      Y = y,
      Width = width,
      Height = height
    };

    Aesthetics = new()
    {
      Fill = fill
    };
  }

  public Selectors<T, TX, TY> Selectors { get; }

  public Aesthetics<T> Aesthetics { get; }

  public Positions<T> Positions { get; } = new();

  public Elements.Rectangle Aesthetic { get; set; } = default!;

  public override void Init<T1, TX1, TY1>(Panel<T1, TX1, TY1> panel, Facet<T1>? facet)
  {
    base.Init(panel, facet);

    Positions.X = XMapping(Selectors.X!, panel.X);
    Positions.Y = YMapping(Selectors.Y!, panel.Y);
  }

  public override void Train(T item)
  {
    Positions.X.Train(item);
    Positions.Y.Train(item);
  }

  public override void Legend()
  {
    Legend(Aesthetics.Fill, value => new Elements.Rectangle
    {
      Fill = value,
      Alpha = Aesthetic.Alpha
    });
  }

  protected override void Shape(T item, bool flip)
  {
    var x = Positions.X.Map(item);
    var y = Positions.Y.Map(item);
    var width = Selectors.Width(item);
    var height = Selectors.Height(item);

    var fill = Aesthetic.Fill;
    if (Aesthetics.Fill is not null)
    {
      fill = Aesthetics.Fill.Map(item);
      if (string.IsNullOrEmpty(fill))
      {
        return;
      }
    }

    var rectangle = new Rectangle
    {
      X = x,
      Y = y,
      Width = width,
      Height = height,
      Aesthetic = new()
      {
        Fill = fill,
        Alpha = Aesthetic.Alpha,
        Color = Aesthetic.Color,
        Width = Aesthetic.Width
      }
    };

    Layer.Add(rectangle);

    if (scale.x)
    {
      Positions.X.Position.Shape(x, x + width);
    }

    if (scale.y)
    {
      Positions.Y.Position.Shape(y, y + height);
    }
  }
}

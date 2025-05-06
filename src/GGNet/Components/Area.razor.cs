namespace GGNet.Components;

using Rendering;
using Shapes;

public partial class Area<T, TX, TY> : ComponentBase
   where TX : struct
   where TY : struct
{
  [Parameter]
  public required Data.Panel<T, TX, TY> Panel { get; init; }

  [Parameter]
  public required IChildRenderModeHandler RenderModeHandler { get; init; }

  [Parameter]
  public required ICoord Coord { get; init; }

  [Parameter]
  public Zone Zone { get; set; }

  [Parameter]
  public required string Clip { get; init; }

  private readonly RenderFragment _renderShapes;

  public Area()
  {
    _renderShapes = RenderShapes;
  }

  protected override bool ShouldRender() => RenderModeHandler.ShouldRender();

  private double X(double x) => Coord.ToX(x);

  private double Y(double y) => Coord.ToY(y);

  private string Path(Path path)
  {
    var sb = StringBuilderCache.Acquire();
    try
    {
      Path(sb, path);
      return sb.ToString();
    }
    finally
    {
      StringBuilderCache.Release(sb);
    }
  }

  private void Path(StringBuilder sb, Path path)
  {
    var (x, y) = path.Points[0];

    var M = true;

    for (var j = 0; j < path.Points.Count; j++)
    {
      (x, y) = path.Points[j];

      if (double.IsNaN(y))
      {
        M = true;
      }
      else
      {
        sb.Append(M ? " M " : " L ");
        sb.Append(X(x));
        sb.Append(' ');
        sb.Append(Y(y));

        M = false;
      }
    }
  }

  private string Path(Area area)
  {
    var sb = StringBuilderCache.Acquire();
    try
    {
      Path(sb, area);
      return sb.ToString();
    }
    finally
    {
      StringBuilderCache.Release(sb);
    }
  }

  private void Path(StringBuilder sb, Area area)
  {
    var (x, _, ymax) = area.Points[0];

    sb.Append("M ");
    sb.Append(X(x));
    sb.Append(' ');
    sb.Append(Y(ymax));

    for (var j = 1; j < area.Points.Count; j++)
    {
      (x, _, ymax) = area.Points[j];

      sb.Append(" L ");
      sb.Append(X(x));
      sb.Append(' ');
      sb.Append(Y(ymax));
    }

    for (var j = 0; j < area.Points.Count; j++)
    {
      double ymin;
      (x, ymin, _) = area.Points[area.Points.Count - j - 1];

      sb.Append(" L ");
      sb.Append(X(x));
      sb.Append(' ');
      sb.Append(Y(ymin));
    }

    sb.Append(" Z");
  }

  private string Path(Geospacial.Polygon poly)
  {
    var sb = StringBuilderCache.Acquire();
    try
    {
      AppendPolygon(sb, poly);
      return sb.ToString();
    }
    finally
    {
      StringBuilderCache.Release(sb);
    }
  }

  private string Path(Geospacial.Polygon[] polygons)
  {
    var sb = StringBuilderCache.Acquire();
    try
    {
      Path(sb, polygons);
      return sb.ToString();
    }
    finally
    {
      StringBuilderCache.Release(sb);
    }
  }

  private void Path(StringBuilder sb, Geospacial.Polygon[] polygons)
  {
    AppendPolygon(sb, polygons[0]);

    for (var i = 1; i < polygons.Length; i++)
    {
      sb.Append(' ');

      AppendPolygon(sb, polygons[i]);
    }
  }

  private void AppendPolygon(StringBuilder sb, Geospacial.Polygon poly)
  {
    sb.Append("M ");
    sb.Append(X(poly.Longitude[0]));
    sb.Append(' ');
    sb.Append(Y(poly.Latitude[0]));

    for (var i = 1; i < poly.Longitude.Length; i++)
    {
      sb.Append(" L ");
      sb.Append(X(poly.Longitude[i]));
      sb.Append(' ');
      sb.Append(Y(poly.Latitude[i]));
    }

    sb.Append(" Z");
  }
}

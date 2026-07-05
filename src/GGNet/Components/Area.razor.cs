namespace GGNet.Components;

using Rendering;
using Shapes;

public partial class Area<T, TX, TY> : ComponentBase
   where TX : struct
   where TY : struct
{
  private static readonly ObjectPool<StringBuilder> pool = new DefaultObjectPoolProvider().CreateStringBuilderPool();

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

  private readonly RenderFragment renderShapes;

  public Area()
  {
    renderShapes = RenderShapes;
  }

  protected override bool ShouldRender() => RenderModeHandler.ShouldRender();

  private double X(double x) => Coord.ToX(x);

  private double Y(double y) => Coord.ToY(y);

  private (double x, double y) P(double x, double y) => Coord.Project(x, y);

  private string Path(Path path)
  {
    var sb = pool.Get();
    try
    {
      Path(sb, path);
      return sb.ToString();
    }
    finally
    {
      sb.Clear();
      pool.Return(sb);
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
        var (px, py) = P(x, y);

        sb.Append(CultureInfo.InvariantCulture, $"{(M ? " M " : " L ")}{px} {py}");

        M = false;
      }
    }
  }

  private string Path(Area area)
  {
    var sb = pool.Get();
    try
    {
      Path(sb, area);
      return sb.ToString();
    }
    finally
    {
      sb.Clear();
      pool.Return(sb);
    }
  }

  private void Path(StringBuilder sb, Area area)
  {
    var (x, _, ymax) = area.Points[0];

    var (px, py) = P(x, ymax);

    sb.Append(CultureInfo.InvariantCulture, $"M {px} {py}");

    for (var j = 1; j < area.Points.Count; j++)
    {
      (x, _, ymax) = area.Points[j];

      (px, py) = P(x, ymax);

      sb.Append(CultureInfo.InvariantCulture, $" L {px} {py}");
    }

    for (var j = 0; j < area.Points.Count; j++)
    {
      double ymin;
      (x, ymin, _) = area.Points[area.Points.Count - j - 1];

      (px, py) = P(x, ymin);

      sb.Append(CultureInfo.InvariantCulture, $" L {px} {py}");
    }

    sb.Append(" Z");
  }

  private string Path(Geospacial.Polygon poly)
  {
    var sb = pool.Get();
    try
    {
      AppendPolygon(sb, poly);
      return sb.ToString();
    }
    finally
    {
      sb.Clear();
      pool.Return(sb);
    }
  }

  private string Path(Geospacial.Polygon[] polygons)
  {
    var sb = pool.Get();
    try
    {
      Path(sb, polygons);
      return sb.ToString();
    }
    finally
    {
      sb.Clear();
      pool.Return(sb);
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
    var (px, py) = P(poly.Longitude[0], poly.Latitude[0]);

    sb.Append(CultureInfo.InvariantCulture, $"M {px} {py}");

    for (var i = 1; i < poly.Longitude.Length; i++)
    {
      (px, py) = P(poly.Longitude[i], poly.Latitude[i]);

      sb.Append(CultureInfo.InvariantCulture, $" L {px} {py}");
    }

    sb.Append(" Z");
  }
}

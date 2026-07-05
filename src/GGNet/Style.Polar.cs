namespace GGNet;

public sealed partial class Style
{
  public sealed class StylePolar
  {
    public PolarRingType Rings { get; set; } = PolarRingType.Polygon;

    public double LabelMargin { get; set; } = 8.0;
  }
}

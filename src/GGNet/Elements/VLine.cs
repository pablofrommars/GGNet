namespace GGNet.Elements;

public readonly record struct VLine : IElement
{
  public required string Stroke { get; init; }

  public double StrokeOpacity { get; init; }

  public double StrokeWidth { get; init; }

  public LineType LineType { get; init; }
}

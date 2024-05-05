namespace GGNet.Elements;

public readonly record struct Line : IElement
{
  public Line() { }

  public string Stroke { get; init; } = "inherit";

  public double StrokeOpacity { get; init; } = 1.0;

  public double StrokeWidth { get; init; } = 1.0;

  public LineType LineType { get; init; } = LineType.Solid;
}

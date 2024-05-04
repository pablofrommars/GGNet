namespace GGNet.Elements;

public readonly record struct Circle : IElement
{
  public Circle() { }

  public double Radius { get; init; }

  public string Fill { get; init; } = "inhenit";

  public double FillOpacity { get; init; } = 1.0;

  public string Stroke { get; init; } = "inhenit";

  public double StrokeOpacity { get; init; } = 1.0;

  public double StrokeWidth { get; init; }

  public string StopColor => Fill;

  public double StopOpacity => FillOpacity;
}

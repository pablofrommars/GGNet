namespace GGNet.Elements;

public readonly record struct Rectangle : IElement
{
  public Rectangle() { }

  public string Fill { get; init; } = "inhenit";

  public double FillOpacity { get; init; } = 1.0;

  public string Stroke { get; init; } = "inhenit";

  public double StrokeOpacity { get; init; } = 1.0;

  public double StrokeWidth { get; init; }

  public Margin Margin { get; init; } = new();

  public string StopColor => Fill;

  public double StopOpacity => FillOpacity;
}

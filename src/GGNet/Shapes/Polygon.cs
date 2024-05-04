namespace GGNet.Shapes;

public readonly record struct Polygon : IShape
{
  public string? Classes { get; init; }

  public Func<MouseEventArgs, Task>? OnClick { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOver { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOut { get; init; }

  public required Geospacial.Polygon Path { get; init; }

  public required Elements.Rectangle Aesthetic { get; init; }
}

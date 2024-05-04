namespace GGNet.Shapes;

public readonly record struct Circle : IShape
{
  public string? Classes { get; init; }

  public Func<MouseEventArgs, Task>? OnClick { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOver { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOut { get; init; }

  public double X { get; init; }

  public double Y { get; init; }

  public required Elements.Circle Aesthetic { get; init; }
}

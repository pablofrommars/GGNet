namespace GGNet.Shapes;

public readonly record struct Rectangle : IShape
{
  public string? Classes { get; init; }

  public Func<MouseEventArgs, Task>? OnClick { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOver { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOut { get; init; }

  public double X { get; init; }

	public double Y { get; init; }

	public double Width { get; init; }

	public double Height { get; init; }

	public required Elements.Rectangle Aesthetic { get; init; }
}

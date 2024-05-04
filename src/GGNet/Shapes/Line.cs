namespace GGNet.Shapes;

public readonly record struct Line : IShape
{
  public string? Classes { get; init; }

  public Func<MouseEventArgs, Task>? OnClick { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOver { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOut { get; init; }

  public double X1 { get; init; }

	public double X2 { get; init; }

	public double Y1 { get; init; }

	public double Y2 { get; init; }

	public required Elements.Line Aesthetic { get; init; }
}

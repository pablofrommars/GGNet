namespace GGNet.Shapes;

public readonly record struct VLine : IShape
{
  public string? Classes { get; init; }

  public Func<MouseEventArgs, Task>? OnClick { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOver { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOut { get; init; }

  public double X { get; init; }

	public required string Label { get; init; }

	public required Elements.Line Line { get; init; }

	public required Elements.Text Text { get; init; }
}

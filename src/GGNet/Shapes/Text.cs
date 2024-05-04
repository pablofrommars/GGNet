namespace GGNet.Shapes;

public readonly record struct Text : IShape
{
  public string? Classes { get; init; }

  public Func<MouseEventArgs, Task>? OnClick { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOver { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOut { get; init; }

  public double X { get; init; }

	public double Y { get; init; }

	public double Height { get; init; }

	public double Width { get; init; }

	public string? Value { get; init; }

	public required Elements.Text Aesthetic { get; init; }
}

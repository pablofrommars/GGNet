namespace GGNet.Shapes;

public record class Shape
{
	public string? Classes { get; internal set; }

	public Func<MouseEventArgs, Task>? OnClick { get; internal set; }

	public Func<MouseEventArgs, Task>? OnMouseOver { get; internal set; }

	public Func<MouseEventArgs, Task>? OnMouseOut { get; internal set; }
}
namespace GGNet.Shapes;

public record Text : Shape
{
	public double X { get; init; }

	public double Y { get; init; }

	public double Height { get; init; }

	public double Width { get; init; }

	public string? Value { get; init; }

	public required Elements.Text Aesthetic { get; init; }
}
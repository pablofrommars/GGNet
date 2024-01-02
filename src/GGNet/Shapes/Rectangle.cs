namespace GGNet.Shapes;

public record Rectangle : Shape
{
	public double X { get; init; }

	public double Y { get; init; }

	public double Width { get; init; }

	public double Height { get; init; }

	public required Elements.Rectangle Aesthetic { get; init; }
}
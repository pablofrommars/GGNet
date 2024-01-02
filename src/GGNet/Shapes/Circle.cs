namespace GGNet.Shapes;

public record Circle : Shape
{
	public double X { get; init; }

	public double Y { get; init; }

	public required Elements.Circle Aesthetic { get; init; }
}
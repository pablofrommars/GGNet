namespace GGNet.Shapes;

public record Line : Shape
{
	public double X1 { get; init; }

	public double X2 { get; init; }

	public double Y1 { get; init; }

	public double Y2 { get; init; }

	public required Elements.Line Aesthetic { get; init; }
}
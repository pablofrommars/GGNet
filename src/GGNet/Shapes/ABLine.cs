namespace GGNet.Shapes;

public record ABLine : Shape
{
	public double A { get; init; }

	public double B { get; init; }

	public (bool x, bool y) Transformation { get; init; }

	public required string Label { get; init; }

	public required Elements.Line Line { get; init; }

	public required Elements.Text Text { get; init; }
}
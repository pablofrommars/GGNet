namespace GGNet.Shapes;

public record VLine : Shape
{
	public double X { get; init; }

	public required string Label { get; init; }

	public required Elements.Line Line { get; init; }

	public required Elements.Text Text { get; init; }
}

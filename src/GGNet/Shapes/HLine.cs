namespace GGNet.Shapes;

public record HLine : Shape
{
	public double Y { get; init; }

	public required string Label { get; init; }

	public required Elements.Line Line { get; init; }

	public required Elements.Text Text { get; init; }
}

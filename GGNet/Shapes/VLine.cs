namespace GGNet.Shapes;

public record VLine : Shape
{
	public double X { get; set; }

	public string Label { get; set; } = default!;

	public Elements.Line Line { get; set; } = default!;

	public Elements.Text Text { get; set; } = default!;
}

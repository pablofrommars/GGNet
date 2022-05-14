namespace GGNet.Shapes;

public record ABLine : Shape
{
	public double A { get; set; }

	public double B { get; set; }

	public (bool x, bool y) Transformation { get; set; }

	public string Label { get; set; } = default!;

	public Elements.Line Line { get; set; } = default!;

	public Elements.Text Text { get; set; } = default!;
}
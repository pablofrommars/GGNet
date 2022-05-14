namespace GGNet.Shapes;

public record HLine : Shape
{
	public double Y { get; set; }

	public string Label { get; set; } = default!;

	public Elements.Line Line { get; set; } = default!;

	public Elements.Text Text { get; set; } = default!;
}

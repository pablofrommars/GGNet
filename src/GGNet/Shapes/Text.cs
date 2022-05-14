namespace GGNet.Shapes;

public record Text : Shape
{
	public double X { get; set; }

	public double Y { get; set; }

	public double Height { get; set; }

	public double Width { get; set; }

	public string? Value { get; set; }

	public Elements.Text Aesthetic { get; set; } = default!;
}
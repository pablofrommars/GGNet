namespace GGNet.Shapes;

public record Circle : Shape
{
	public double X { get; set; }

	public double Y { get; set; }

	public Elements.Circle Aesthetic { get; set; } = default!;
}
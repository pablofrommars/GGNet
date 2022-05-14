namespace GGNet.Shapes;

public record Line : Shape
{
	public double X1 { get; set; }

	public double X2 { get; set; }

	public double Y1 { get; set; }

	public double Y2 { get; set; }

	public Elements.Line Aesthetic { get; set; } = default!;
}
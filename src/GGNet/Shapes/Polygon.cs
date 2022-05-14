namespace GGNet.Shapes;

public record Polygon : Shape
{
	public Geospacial.Polygon Path { get; set; } = default!;

	public Elements.Rectangle Aesthetic { get; set; } = default!;
}
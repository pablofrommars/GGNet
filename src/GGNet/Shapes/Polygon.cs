namespace GGNet.Shapes;

public record Polygon : Shape
{
	public required Geospacial.Polygon Path { get; init; }

	public required Elements.Rectangle Aesthetic { get; init; }
}
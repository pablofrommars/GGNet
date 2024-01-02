namespace GGNet.Shapes;

public record MultiPolygon : Shape
{
	public required Geospacial.Polygon[] Polygons { get; init; }

	public required Elements.Rectangle Aesthetic { get; init; }
}
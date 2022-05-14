namespace GGNet.Shapes;

public record MultiPolygon : Shape
{
	public Geospacial.Polygon[] Polygons { get; set; } = default!;

	public Elements.Rectangle Aesthetic { get; set; } = default!;
}
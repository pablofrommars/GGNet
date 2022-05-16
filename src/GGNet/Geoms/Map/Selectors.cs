namespace GGNet.Geoms.Map;

internal sealed class Selectors<T>
{
	public Func<T, Geospacial.Polygon[]> Polygons { get; set; } = default!;

	public Func<T, (Geospacial.Point point, string content)>? Tooltip { get; set; }
}
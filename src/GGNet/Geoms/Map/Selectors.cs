namespace GGNet.Geoms.Map;

internal sealed class Selectors<T>
{
	public required Func<T, Geospacial.Polygon[]> Polygons { get; set; }

	public Func<T, (Geospacial.Point point, RenderFragment content)>? Tooltip { get; set; }
}

namespace GGNet.Geospacial;

public readonly record struct Polygon
{
	public bool Hole { get; init; }

	public required double[] Longitude { get; init; }

	public required double[] Latitude { get; init; }
}

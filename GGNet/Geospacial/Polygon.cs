namespace GGNet.Geospacial;

public sealed record Polygon
{
	public bool Hole { get; init; }

	public double[] Longitude { get; init; } = default!;

	public double[] Latitude { get; init; } = default!;
}
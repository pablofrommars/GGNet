namespace GGNet.Geospacial;

public readonly record struct Point
{
	public double Longitude { get; init; }

	public double Latitude { get; init; }
}

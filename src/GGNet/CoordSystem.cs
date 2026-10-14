namespace GGNet;

/// <summary>
/// Coordinate systems as flags: a plot holds a single value; each geom declares the set it supports.
/// </summary>
[Flags]
public enum CoordSystem
{
	Cartesian = 1 << 0,
	Polar = 1 << 1
}

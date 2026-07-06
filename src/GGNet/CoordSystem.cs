namespace GGNet;

// Flags: PlotContext.CoordSystem holds a single value; geoms declare the set they
// support via IGeom.SupportedCoordSystems.
[Flags]
public enum CoordSystem
{
	Cartesian = 1 << 0,
	Polar = 1 << 1
}

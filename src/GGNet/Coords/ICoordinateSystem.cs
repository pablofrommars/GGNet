using GGNet.Components;

namespace GGNet.Coords;

// The coordinate-system strategy: fractions in, pixels out. Scales own
// value→fraction (Position.Coord); the system owns fraction→pixel, so
// strategies stay scale-agnostic and pure-math testable. Instances are per
// panel: Measure captures the panel's plotting area — and whatever interior
// state (center, radius) Project and grid composition need.
internal interface ICoordinateSystem
{
	void Measure(Zone area);

	(double x, double y) Project(double cx, double cy);
}

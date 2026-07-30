using GGNet.Components;

namespace GGNet.Coords;

// The coordinate-system strategy: fractions in, pixels out. Scales own
// value→fraction (Position.Coord); the system owns fraction→pixel, so
// strategies stay scale-agnostic and pure-math testable. Instances are per
// panel: Measure captures the panel's plotting area — and whatever interior
// state (center, radius) Project and grid composition need.
internal interface ICoordinateSystem
{
	// Cartesian reserves axis bands around the plotting area; polar draws
	// breaks and labels inside it and reserves none.
	bool CarvesAxisBands { get; }

	// Expansion hints for defaulted scales; null means the scale's own default.
	(double minMult, double minAdd, double maxMult, double maxAdd)? XExpansion(bool discrete);

	(double minMult, double minAdd, double maxMult, double maxAdd)? YExpansion(bool discrete);

	void Measure(Zone area);

	(double x, double y) Project(double cx, double cy);

	// Inverse of Project: pixels in, fractions out. Systems without a
	// meaningful inverse (polar, until a need arrives) throw.
	(double cx, double cy) Unproject(double px, double py);

	GridComposition ComposeGrid(GridInputs inputs);
}

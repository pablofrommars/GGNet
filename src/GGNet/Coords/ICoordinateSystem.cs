using GGNet.Components;

namespace GGNet.Coords;

internal interface ICoordinateSystem
{
	bool CarvesAxisBands { get; }

	(double minMult, double minAdd, double maxMult, double maxAdd)? XExpansion(bool discrete);

	(double minMult, double minAdd, double maxMult, double maxAdd)? YExpansion(bool discrete);

	void Measure(Zone area, bool hasXTitles);

	(double x, double y) Project(double cx, double cy);

	(double cx, double cy) Unproject(double px, double py);

	GridComposition ComposeGrid(GridInputs inputs);
}

using GGNet.Components;

namespace GGNet.Coords;

internal closed class CoordinateSystem
{
	public abstract bool CarvesAxisBands { get; }

	public abstract (double minMult, double minAdd, double maxMult, double maxAdd)? XExpansion(bool discrete);

	public abstract (double minMult, double minAdd, double maxMult, double maxAdd)? YExpansion(bool discrete);

	public abstract void Measure(Zone area, bool hasXTitles);

	public abstract (double x, double y) Project(double cx, double cy);

	public abstract (double cx, double cy) Unproject(double px, double py);

	public abstract GridComposition ComposeGrid(GridInputs inputs);
}

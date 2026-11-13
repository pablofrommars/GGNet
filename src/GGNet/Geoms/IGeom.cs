using GGNet.Shapes;

namespace GGNet.Geoms;

internal interface IGeom
{
	List<Shape> Layer { get; }

	IStatSource? StatSource => null;

	CoordSystem SupportedCoordSystems => CoordSystem.Cartesian | CoordSystem.Polar;

	void Train();

	void Legend();

	void Shape();

	void Clear();

	bool Inspected => false;

	IReadOnlyList<Readings.ISeriesIndex> InspectSeries() => [];

	IReadOnlyList<Readings.ISeriesIndex> StepSeries() => [];
}

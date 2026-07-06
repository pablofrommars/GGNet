using GGNet.Shapes;

namespace GGNet.Geoms;

internal interface IGeom
{
	List<Shape> Layer { get; }

	CoordSystem SupportedCoordSystems => CoordSystem.Cartesian | CoordSystem.Polar;

	void Train();

	void Legend();

	void Shape();

	void Clear();
}

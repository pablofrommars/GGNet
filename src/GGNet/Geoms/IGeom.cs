using GGNet.Shapes;

namespace GGNet.Geoms;

internal interface IGeom
{
	List<IShape> Layer { get; }

	CoordSystem SupportedCoordSystems => CoordSystem.Cartesian | CoordSystem.Polar;

	void Train();

	void Legend();

	void Shape(bool flip);

	void Clear();
}

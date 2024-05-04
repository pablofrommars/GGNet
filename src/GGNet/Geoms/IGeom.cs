using GGNet.Common;
using GGNet.Shapes;

namespace GGNet.Geoms;

public interface IGeom
{
	Buffer<IShape> Layer { get; }

	void Train();

	void Legend();

	void Shape(bool flip);

	void Clear();
}

using GGNet.Shapes;

namespace GGNet.Geoms;

public interface IGeom
{
	Buffer<Shape> Layer { get; }

	void Train();

	void Legend();

	void Shape(bool flip);

	void Clear();
}
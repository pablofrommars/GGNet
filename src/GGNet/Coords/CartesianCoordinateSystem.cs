using GGNet.Components;

namespace GGNet.Coords;

internal sealed class CartesianCoordinateSystem : ICoordinateSystem
{
	private Zone area;

	public void Measure(Zone area) => this.area = area;

	public (double x, double y) Project(double cx, double cy)
	  => (area.X + cx * area.Width, area.Y + (1.0 - cy) * area.Height);
}

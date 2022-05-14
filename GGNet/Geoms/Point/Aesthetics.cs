using GGNet.Scales;

namespace GGNet.Geoms.Point;

internal sealed class Aesthetics<T>
{
	public IAestheticMapping<T, double>? Size { get; set; }

	public IAestheticMapping<T, string>? Color { get; set; }
}
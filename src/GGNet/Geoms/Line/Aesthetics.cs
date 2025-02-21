using GGNet.Scales;

namespace GGNet.Geoms.Line;

internal sealed class Aesthetics<T>
{
	public IAestheticMapping<T, string>? Color { get; set; }

	public IAestheticMapping<T, LineType>? LineType { get; set; }
}
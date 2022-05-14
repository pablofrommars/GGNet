using GGNet.Common;
using GGNet.Scales;

namespace GGNet.Geoms.Line;

internal class Aesthetics<T>
{
	public IAestheticMapping<T, string>? Color { get; set; }

	public IAestheticMapping<T, LineType>? LineType { get; set; }
}
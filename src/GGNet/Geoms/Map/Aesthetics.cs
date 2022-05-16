using GGNet.Scales;

namespace GGNet.Geoms.Map;

internal sealed class Aesthetics<T>
{
	public IAestheticMapping<T, string>? Fill { get; set; }
}